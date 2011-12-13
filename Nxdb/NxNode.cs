using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using Nxdb.Dom;
using Nxdb.Io;
using org.basex.build;
using org.basex.data;
using org.basex.query.item;
using org.basex.query.iter;
using org.basex.query.up.primitives;
using org.basex.util;
using ItemNodeType = org.basex.query.item.NodeType;

namespace Nxdb
{
    //TODO: Reimplement IEquatable<NxNode>
    public class NxNode //: IEquatable<NxNode>
    {
        private readonly NxDatabase _database;
        public NxDatabase Database
        {
            get { return _database; }
        }

        private ANode _aNode;  // This should be updated before every use by calling Valid.get or CheckValid(), null = invalidated
        private DBNode _dbNode; // This should be set if the node is a DBNode, null = not a DBNode

        // The unique immutable ID for the node, -1 if not database node
        private readonly int _id;
        public int Id
        {
            get { return _id; }
        }

        // Exposing "pre" as "Index" for API consumers, -1 if not database node
        public int Index
        {
            get
            {
                CheckValid();
                return _dbNode == null ? -1 : _dbNode.pre;
            }
        }

        // Cache the last modified time to avoid checking pre against id on every operation
        private long _time;

        internal NxNode(NxDatabase database, int pre)
            : this(database, pre, database.GetId(pre))
        {}

        internal NxNode(NxDatabase database, int pre, int id)
        {
            if (database == null) throw new ArgumentNullException("database");
            if (pre < 0) throw new ArgumentOutOfRangeException("pre");
            if (id < 0) throw new ArgumentOutOfRangeException("id");
            _database = database;
            _dbNode = new DBNode(database.Data, pre);
            _aNode = _dbNode;
            _id = id;
            _time = database.GetTime();
            if (!CheckType(ItemNodeType.ELM, ItemNodeType.TXT,
                ItemNodeType.ATT, ItemNodeType.DOC,
                ItemNodeType.COM, ItemNodeType.PI))
                throw new ArgumentException("Invalid node type");
        }

        internal NxNode(NxDatabase database, DBNode node)
        {
            if (database == null) throw new ArgumentNullException("database");
            if (node == null) throw new ArgumentNullException("node");
            _database = database;
            _dbNode = node.copy();
            _aNode = _dbNode;
            _id = database.GetId(node.pre);
            _time = database.GetTime();
            if (!CheckType(ItemNodeType.ELM, ItemNodeType.TXT,
                ItemNodeType.ATT, ItemNodeType.DOC,
                ItemNodeType.COM, ItemNodeType.PI))
                throw new ArgumentException("Invalid node type");
        }

        internal NxNode(NxDatabase database, ANode node)
        {
            if (database == null) throw new ArgumentNullException("database");
            if (node == null) throw new ArgumentNullException("node");
            _database = database;
            _dbNode = node as DBNode;
            if(_dbNode != null)
            {
                // If this is a DBNode, we need a copy
                _dbNode = _dbNode.copy();
                _aNode = _dbNode;
                _id = database.GetId(_dbNode.pre);
            }
            else
            {
                // Otherwise, just use the original ANode
                _aNode = node;
                _id = -1;
            }
            _time = database.GetTime();
            if (!CheckType(ItemNodeType.ELM, ItemNodeType.TXT,
                ItemNodeType.ATT, ItemNodeType.DOC,
                ItemNodeType.COM, ItemNodeType.PI))
                throw new ArgumentException("Invalid node type");
        }
        
        #region Dom

        public XmlNodeType NodeType
        {
            get
            {
                CheckValid();
                NodeType nodeType = _aNode.ndType();
                if (nodeType == ItemNodeType.ELM)
                {
                    return XmlNodeType.Element;
                }
                if (nodeType == ItemNodeType.TXT)
                {
                    return XmlNodeType.Text;
                }
                if (nodeType == ItemNodeType.ATT)
                {
                    return XmlNodeType.Attribute;
                }
                if(nodeType == ItemNodeType.DOC)
                {
                    return XmlNodeType.Document;
                }
                if (nodeType == ItemNodeType.COM)
                {
                    return XmlNodeType.Comment;
                }
                if (nodeType == ItemNodeType.PI)
                {
                    return XmlNodeType.ProcessingInstruction;
                }
                throw new InvalidOperationException("Unexpected node type");
            }
        }

        // Get the System.XmlNode representation and cache it for future reference
        // TODO: Uncomment DOM objects in the switch statement
        public XmlNode XmlNode
        {
            get
            {
                CheckValid();

                // Has it already been cached?
                WeakReference weakNode;
                XmlNode xmlNode = null;
                if(_database.DomCache.TryGetValue(_id, out weakNode))
                {
                    xmlNode = (XmlNode)weakNode.Target;
                }

                // If not found in the cache
                if (xmlNode == null)
                {
                    // Create the appropriate node type
                    NodeType nodeType = _aNode.ndType();
                    if (nodeType == ItemNodeType.ELM)
                    {
                        //xmlNode = new NxElement(this);
                    }
                    else if (nodeType == ItemNodeType.TXT)
                    {
                        //xmlNode = new NxText(this);
                    }
                    else if (nodeType == ItemNodeType.ATT)
                    {
                        //xmlNode = new NxAttribute(this);
                    }
                    else if (nodeType == ItemNodeType.DOC)
                    {
                        //xmlNode = new NxDocument(this);
                    }
                    else if (nodeType == ItemNodeType.COM)
                    {
                        //xmlNode = new NxComment(this);
                    }
                    else if (nodeType == ItemNodeType.PI)
                    {
                        //xmlNode = new NxProcessingInstruction(this);
                    }
                    else
                    {
                        throw new InvalidOperationException("Unexpected node type");
                    }

                    // Cache for later
                    _database.DomCache[_id] = new WeakReference(xmlNode);
                }

                return xmlNode;
            }
        }

        // Checks if the input NxNode is null and if so, returns null - used from Dom implementations
        internal static XmlNode GetXmlNode(NxNode node)
        {
            return node == null ? null : node.XmlNode;
        }

        #endregion
        
        #region Validity

        // Verify the id and pre values still match, and if they don't update the pre
        // Should be checked before every operation (and if invalid, don't perform the operation)
        // Need to check because nodes may have been modified explicilty (through methods) or implicitly (XQuery Update)
        // Returns true if this NxNode is valid (it exists in the database), false otherwise
        public bool Valid
        {
            get
            {
                // If we no longer have a node, then we've been invalidated
                if( _aNode == null )
                {
                    return false;
                }

                // If we're not a database node, then always valid
                if( _dbNode == null )
                {
                    return true;
                }

                // Check if the database has been modified since the last validity check
                long time = _database.GetTime();
                if (_time != time)
                {
                    _time = time;

                    // First check if the pre value is too large (the database shrunk),
                    // then check if the current pre still refers to the same id (do second since it requires disk access)
                    if (_dbNode.pre >= _database.GetSize() || _id != _database.GetId(_dbNode.pre))
                    {
                        int pre = _database.GetPre(_id);
                        if(pre == -1)
                        {
                            _aNode = null;
                            _dbNode = null;
                            return false;
                        }
                        _dbNode.set(pre, ANode.kind(_aNode.ndType()));
                    }
                }

                return true;
            }
        }

        private void CheckValid(bool requireDatabase = false, params NodeType[] types)
        {
            if( !Valid )
            {
                throw new InvalidOperationException("Node no longer valid");
            }

            if(requireDatabase && _dbNode == null)
            {
                throw new InvalidOperationException("This operation requires a database node");
            }

            if(types.Length > 0 && !CheckType(types))
            {
                throw new InvalidOperationException("Node type is not valid for this operation");
            }
        }

        private void CheckValid(params NodeType[] types)
        {
            CheckValid(false, types);
        }

        private bool CheckType(params NodeType[] types)
        {
            NodeType nodeType = _aNode.ndType();
            return types.Contains(nodeType);
        }

        #endregion

        /*
        #region Queries
        
        public NxQuery GetQuery(string expression)
        {
            NxQuery query = new NxQuery(database.Manager, expression);
            query.SetContext(this);
            return query;
        }

        //If you are executing an update operation, you may need to enumerate the result for the
        //update to take effect (I.e., use .Count() after the query)
        public IEnumerable<object> Query(string expression)
        {
            return GetQuery(expression).Evaluate();
        }

        #endregion
        
        */

        #region Axis Traversal

        // Provides typed enumeration for BaseX NodeIter, which are limited to ANode results
        // and thus the enumeration results are guaranteed to produce NxNode objects
        private IEnumerable<NxNode> EnumerateNodes(NodeIter iter)
        {
            IterEnum iterEnum = new IterEnum(_database, iter);
            return iterEnum.Cast<NxNode>();
        }

        private IEnumerable<ANode> EnumerateANodes(NodeIter iter)
        {
            while (true)
            {
                ANode node = iter.next();
                if (node == null) yield break;
                yield return node;
            }
        }

        public IEnumerable<NxNode> Children
        {
            get
            {
                CheckValid();
                return EnumerateNodes(_aNode.children());
            }
        }

        public NxNode Child(int index)
        {
            return Children.ElementAtOrDefault(index);
        }

        public IEnumerable<NxNode> ChildElements
        {
            get { return Children.Where(c => c._aNode.ndType() == ItemNodeType.ELM); }
        }

        public NxNode ChildElement(int index)
        {
            return ChildElements.ElementAtOrDefault(index);
        }

        public IEnumerable<NxNode> FollowingSiblings
        {
            get
            {
                CheckValid();
                return EnumerateNodes(_aNode.follSibl());
            }
        }

        public IEnumerable<NxNode> PrecedingSiblings
        {
            get
            {
                CheckValid();
                return EnumerateNodes(_aNode.precSibl());
            }
        }

        public IEnumerable<NxNode> Following
        {
            get
            {
                CheckValid();
                return EnumerateNodes(_aNode.foll());
            }
        }

        public IEnumerable<NxNode> Preceding
        {
            get
            {
                CheckValid();
                return EnumerateNodes(_aNode.prec());
            }
        }

        public NxNode Parent
        {
            get
            {
                CheckValid();
                ANode node = _aNode.parent();
                return node == null ? null : new NxNode(_database, node);
            }
        }

        public IEnumerable<NxNode> Ancestors
        {
            get
            {
                CheckValid();
                return EnumerateNodes(_aNode.anc());
            }
        }

        public IEnumerable<NxNode> AncestorsOrSelf
        {
            get
            {
                CheckValid();
                return EnumerateNodes(_aNode.ancOrSelf());
            }
        }

        public IEnumerable<NxNode> Descendants
        {
            get
            {
                CheckValid();
                return EnumerateNodes(_aNode.descendant());
            }
        }

        public IEnumerable<NxNode> DescendantsOrSelf
        {
            get
            {
                CheckValid();
                return EnumerateNodes(_aNode.descOrSelf());
            }
        }

        #endregion

        #region Attributes

        /// <summary>
        /// Gets the attributes. Per the XML standard, the ordering of attributes is
        /// undefined and should not be considered relevant or consistent.
        /// </summary>
        public IEnumerable<NxNode> Attributes
        {
            get
            {
                CheckValid(true, ItemNodeType.ELM);
                return EnumerateNodes(_aNode.attributes());
            }
        }

        private ANode AttributeANode(string name)
        {
            QNm qnm = new QNm(name.Token());
            return EnumerateANodes(_aNode.attributes()).FirstOrDefault(n => n.qname().eq(qnm));
        }

        public NxNode Attribute(string name)
        {
            CheckValid(ItemNodeType.ELM);
            if (name == null) throw new ArgumentNullException("name");
            if (name == String.Empty) throw new ArgumentException("name");

            ANode node = AttributeANode(name);
            return node == null ? null : new NxNode(_database, node);
        }

        // Returns String.Empty if the attribute doesn't exist
        public string AttributeValue(string name)
        {
            CheckValid(ItemNodeType.ELM);
            if (name == null) throw new ArgumentNullException("name");
            if (name == String.Empty) throw new ArgumentException("name");

            ANode node = AttributeANode(name);
            return node == null ? String.Empty : node.atom().Token();
        }
        
        public void RemoveAllAttributes()
        {
            CheckValid(true, ItemNodeType.ELM);

            using (new UpdateContext())
            {
                foreach(DBNode node in EnumerateANodes(_aNode.attributes()).OfType<DBNode>())
                {
                    Update(new DeleteNode(node.pre, _database.Data, null));
                }
            }
        }

        public void RemoveAttribute(string name)
        {
            CheckValid(true, ItemNodeType.ELM);
            if (name == null) throw new ArgumentNullException("name");
            if (name == String.Empty) throw new ArgumentException("name");

            DBNode node = AttributeANode(name) as DBNode;
            if (node != null)
            {
                using (new UpdateContext())
                {
                    Update(new DeleteNode(node.pre, _database.Data, null));
                }
            }
        }

        public void InsertAttribute(string name, string value)
        {
            CheckValid(true, ItemNodeType.ELM);
            if (name == null) throw new ArgumentNullException("name");
            if (value == null) throw new ArgumentNullException("value");
            if (name == String.Empty) throw new ArgumentException("name");

            using (new UpdateContext())
            {
                Update(new InsertAttribute(_dbNode.pre, _database.Data, null,
                    Helper.GetNodeCache(new FAttr(new QNm(name.Token()), value.Token()))));
            }
        }

        #endregion

        #region Reading/Writing

        // Helper to add an update primitive to the open update context with the current database context
        private void Update(UpdatePrimitive update)
        {
            UpdateContext.AddUpdate(update, _database.Context);
        }

        /// <summary>
        /// Removes all child nodes AND attributes.
        /// </summary>
        public void RemoveAll()
        {
            using (new UpdateContext())
            {
                RemoveAllAttributes();  // Contains the call to CheckValid()
                foreach (DBNode node in EnumerateANodes(_aNode.children()).OfType<DBNode>())
                {
                    Update(new DeleteNode(node.pre, _database.Data, null));
                }
            }
        }
        
        /// <summary>
        /// Removes this node from the database and immediatly invalidates it.
        /// </summary>
        public void Remove()
        {
            CheckValid(true);

            using (new UpdateContext())
            {
                Update(new DeleteNode(_dbNode.pre, _database.Data, null));
            }

            _aNode = null;
            _dbNode = null;
        }

        public void Append(XmlReader xmlReader)
        {
            CheckValid(true, ItemNodeType.ELM, ItemNodeType.DOC);
            if (xmlReader == null) throw new ArgumentNullException("xmlReader");

            NodeCache nodeCache = Helper.GetNodeCache(xmlReader);
            if (nodeCache != null)
            {
                using (new UpdateContext())
                {
                    Update(new InsertInto(_dbNode.pre, _database.Data, null, nodeCache, true));
                }
            }
        }

        public void Prepend(XmlReader xmlReader)
        {
            CheckValid(true, ItemNodeType.ELM, ItemNodeType.DOC);
            if (xmlReader == null) throw new ArgumentNullException("xmlReader");

            NodeCache nodeCache = Helper.GetNodeCache(xmlReader);
            if (nodeCache != null)
            {
                using (new UpdateContext())
                {
                    Update(new InsertIntoFirst(_dbNode.pre, _database.Data, null, nodeCache));
                }
            }
        }

        public void InsertBefore(XmlReader xmlReader)
        {
            CheckValid(true, ItemNodeType.ELM, ItemNodeType.TXT,
                ItemNodeType.COM, ItemNodeType.PI);
            if (xmlReader == null) throw new ArgumentNullException("xmlReader");

            NodeCache nodeCache = Helper.GetNodeCache(xmlReader);
            if (nodeCache != null)
            {
                using (new UpdateContext())
                {
                    Update(new InsertBefore(_dbNode.pre, _database.Data, null, nodeCache));
                }
            }
        }

        public void InsertAfter(XmlReader xmlReader)
        {
            CheckValid(true, ItemNodeType.ELM, ItemNodeType.TXT,
                ItemNodeType.COM, ItemNodeType.PI);
            if (xmlReader == null) throw new ArgumentNullException("xmlReader");

            NodeCache nodeCache = Helper.GetNodeCache(xmlReader);
            if (nodeCache != null)
            {
                using (new UpdateContext())
                {
                    Update(new InsertAfter(_dbNode.pre, _database.Data, null, nodeCache));
                }
            }
        }
        
        // Validity check is performed in streaming methods
        public string InnerXml
        {
            get
            {
                StringBuilder builder = new StringBuilder();
                using (XmlWriter xmlWriter = XmlWriter.Create(builder, Helper.WriterSettings))
                {
                    WriteInnerXml(xmlWriter);
                }
                return builder.ToString();
            }
            set
            {
                if (value == null) throw new ArgumentNullException("value");
                using (StringReader stringReader = new StringReader(value))
                {
                    using (XmlReader xmlReader = XmlReader.Create(stringReader, Helper.ReaderSettings))
                    {
                        ReadInnerXml(xmlReader);
                    }
                }
            }
        }
        
        public void WriteInnerXml(XmlWriter xmlWriter)
        {
            if (xmlWriter == null) throw new ArgumentNullException("xmlWriter");
            CheckValid(ItemNodeType.ELM, ItemNodeType.DOC);
            using(XmlWriterSerializer serializer = new XmlWriterSerializer(xmlWriter, false))
            {
                foreach(ANode node in EnumerateANodes(_aNode.children()))
                {
                    node.serialize(serializer);
                }
            }
        }

        public void ReadInnerXml(XmlReader xmlReader)
        {
            if (xmlReader == null) throw new ArgumentNullException("xmlReader");
            CheckValid(true, ItemNodeType.ELM, ItemNodeType.DOC);
            ReplaceChildren(Helper.GetNodeCache(xmlReader));
        }

        // Validity check is performed in streaming methods
        public string OuterXml
        {
            get
            {
                StringBuilder builder = new StringBuilder();
                using (XmlWriter xmlWriter = XmlWriter.Create(builder, Helper.WriterSettings))
                {
                    WriteOuterXml(xmlWriter);
                }
                return builder.ToString();
            }
        }

        public void WriteOuterXml(XmlWriter xmlWriter)
        {
            if (xmlWriter == null) throw new ArgumentNullException("xmlWriter");
            CheckValid(ItemNodeType.ELM, ItemNodeType.DOC);
            using (XmlWriterSerializer serializer = new XmlWriterSerializer(xmlWriter, false))
            {
                _aNode.serialize(serializer);
            }
        }

        /// <summary>
        /// Gets or sets the inner text for document or element nodes.
        /// Only valid for document and element nodes.
        /// </summary>
        /// <value>
        /// The new inner text.
        /// </value>
        /// <exception cref="ArgumentNullException">
        /// Setting the value with null.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Node is not a document or element node. Node is no longer valid. Setting the value of a non-database node.
        /// </exception>
        // Validity check is performed in streaming methods
        public string InnerText
        {
            get
            {
                using (StringWriter stringWriter = new StringWriter())
                {
                    WriteInnerText(stringWriter);
                    stringWriter.Close();
                    return stringWriter.ToString();
                }
            }
            set
            {
                if (value == null) throw new ArgumentNullException("value");
                using (StringReader stringReader = new StringReader(value))
                {
                    ReadInnerText(stringReader);
                }
            }
        }

        /// <summary>
        /// Gets a TextReader that can be used to stream inner text content for this node.
        /// </summary>
        public TextReader InnerTextReader
        {
            get
            {
                CheckValid(ItemNodeType.ELM, ItemNodeType.DOC);
                return new InnerTextReader(EnumerateANodes(_aNode.descendant())
                    .Where(n => n.ndType() == ItemNodeType.TXT).GetEnumerator());
            }
        }

        public void WriteInnerText(TextWriter textWriter)
        {
            if (textWriter == null) throw new ArgumentNullException("textWriter");
            CheckValid(ItemNodeType.ELM, ItemNodeType.DOC);
            using (TextWriterSerializer serializer = new TextWriterSerializer(textWriter))
            {
                _aNode.serialize(serializer);
            }
        }

        public void ReadInnerText(TextReader textReader)
        {
            if (textReader == null) throw new ArgumentNullException("textReader");
            CheckValid(true, ItemNodeType.ELM, ItemNodeType.DOC);
            ReplaceChildren(Helper.GetNodeCache(new ANode[]{new FTxt(textReader.ReadToEnd().Token())}));
        }

        // Used by ReadInnerXml(), ReadInnerText(), and Value.set
        // Deletes all the child nodes and adds the nodeCache nodes, does nothing if nodeCache is null
        private void ReplaceChildren(NodeCache nodeCache)
        {
            if (nodeCache != null)
            {
                using (new UpdateContext())
                {
                    // Remove all child elements
                    foreach (DBNode node in EnumerateANodes(_aNode.children()).OfType<DBNode>())
                    {
                        Update(new DeleteNode(node.pre, _database.Data, null));
                    }

                    // Append the new content
                    Update(new InsertInto(_dbNode.pre, _database.Data, null, nodeCache, true));
                }
            }
        }

        /// <summary>
        /// Gets or sets the value. Returns an empty string if no value.
        /// Document/Element: same as InnerText
        /// Text/Comment: text content
        /// Attribute: value
        /// Processing Instruction: text content (without the target)
        /// </summary>
        public string Value
        {
            get
            {
                return CheckType(ItemNodeType.ELM, ItemNodeType.DOC)
                    ? InnerText : _aNode.atom().Token();
            }
            set
            {
                if (value == null) throw new ArgumentNullException("value");
                CheckValid(true);
                if (CheckType(ItemNodeType.ELM, ItemNodeType.DOC))
                {
                    // If an element or document, set the inner text
                    ReplaceChildren(Helper.GetNodeCache(new ANode[]{new FTxt(value.Token())}));
                }
                else
                {
                    // Otherwise, use the XQuery Update set value primitive
                    using (new UpdateContext())
                    {
                        Update(new ReplaceValue(_dbNode.pre, _database.Data, null, value.Token()));
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets the fully-qualified name of this node.
        /// Get name returns an empty string if not an element or attribute.
        /// Set name changes the name for elements, attributes, or processing instructions.
        /// </summary>
        public string Name
        {
            get
            {
                CheckValid();
                return CheckType(ItemNodeType.ELM, ItemNodeType.ATT)
                    ? _aNode.nname().Token() : String.Empty;
            }
            set
            {
                if (value == null) throw new ArgumentNullException("value");
                CheckValid(true);
                if (CheckType(ItemNodeType.ELM, ItemNodeType.ATT, ItemNodeType.PI))
                {
                    using (new UpdateContext())
                    {
                        Update(new RenameNode(_dbNode.pre, _database.Data, null, new QNm(value.Token())));
                    }
                }
            }
        }

        public string LocalName
        {
            get
            { 
                CheckValid();
                return CheckType(ItemNodeType.ELM, ItemNodeType.ATT, ItemNodeType.PI)
                    ? _aNode.qname().ln().Token() : String.Empty;
            }
        }

        public string Prefix
        {
            get
            {
                CheckValid();
                return CheckType(ItemNodeType.ELM, ItemNodeType.ATT, ItemNodeType.PI)
                    ? _aNode.qname().pref().Token() : String.Empty;
            }
        }

        public string NamespaceUri
        {
            get
            {
                CheckValid();
                return CheckType(ItemNodeType.ELM, ItemNodeType.ATT, ItemNodeType.PI)
                    ? _aNode.qname().uri().atom().Token() : String.Empty;
            }
        }
        
        #endregion

        #region Equality/Hashing

        //public bool Equals(NxNode other)
        //{
        //    if( other == null )
        //    {
        //        return false;
        //    }
        //    return database.Equals(other.Database) && id == other.Id;
        //}

        //public override bool Equals(object other)
        //{
        //    NxNode node = other as NxNode;
        //    if( node != null )
        //    {
        //        return Equals(node);
        //    }
        //    return false;
        //}

        //public override int GetHashCode()
        //{
        //    int result = 17;
        //    result = 37 * result + id.GetHashCode();
        //    result = 37 * result + database.GetHashCode();
        //    return result;
        //}

        #endregion
    }
}
