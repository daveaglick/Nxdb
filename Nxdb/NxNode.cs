using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using Nxdb.Dom;
using org.basex.build;
using org.basex.data;
using org.basex.query.item;
using org.basex.query.iter;
using org.basex.query.up.primitives;
using org.basex.util;

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

        private ANode _aNode;  //This should be updated before every use by calling Valid.get or CheckValid(), null = invalidated
        private DBNode _dbNode; //This should be set if the node is a DBNode, null = not a DBNode

        //The unique immutable ID for the node, -1 if not database node
        private readonly int _id;
        public int Id
        {
            get { return _id; }
        }

        //Exposing "pre" as "Index" for API consumers, -1 if not database node
        public int Index
        {
            get
            {
                CheckValid();
                return _dbNode == null ? -1 : _dbNode.pre;
            }
        }

        //Cache the last modified time to avoid checking pre against id on every operation
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
        }

        internal NxNode(NxDatabase database, ANode node)
        {
            if (database == null) throw new ArgumentNullException("database");
            if (node == null) throw new ArgumentNullException("node");
            _database = database;
            _dbNode = node as DBNode;
            if(_dbNode != null)
            {
                //If this is a DBNode, we need a copy
                _dbNode = _dbNode.copy();
                _aNode = _dbNode;
                _id = database.GetId(_dbNode.pre);
            }
            else
            {
                //Otherwise, just use the original ANode
                _aNode = node;
                _id = -1;
            }
            _time = database.GetTime();
        }
        
        #region Dom

        public XmlNodeType NodeType
        {
            get
            {
                CheckValid();
                NodeType nodeType = _aNode.ndType();
                if (nodeType == org.basex.query.item.NodeType.ELM)
                {
                    return XmlNodeType.Element;
                }
                if (nodeType == org.basex.query.item.NodeType.TXT)
                {
                    return XmlNodeType.Text;
                }
                if (nodeType == org.basex.query.item.NodeType.ATT)
                {
                    return XmlNodeType.Attribute;
                }
                if(nodeType == org.basex.query.item.NodeType.DOC)
                {
                    return XmlNodeType.Document;
                }
                if (nodeType == org.basex.query.item.NodeType.COM)
                {
                    return XmlNodeType.Comment;
                }
                if (nodeType == org.basex.query.item.NodeType.PI)
                {
                    return XmlNodeType.ProcessingInstruction;
                }
                throw new InvalidOperationException("Unexpected node type");
            }
        }

        //Get the System.XmlNode representation and cache it for future reference
        //TODO: Uncomment DOM objects in the switch statement
        public XmlNode XmlNode
        {
            get
            {
                CheckValid();

                //Has it already been cached?
                WeakReference weakNode;
                XmlNode xmlNode = null;
                if(_database.DomCache.TryGetValue(_id, out weakNode))
                {
                    xmlNode = (XmlNode)weakNode.Target;
                }

                //If not found in the cache
                if (xmlNode == null)
                {
                    //Create the appropriate node type
                    NodeType nodeType = _aNode.ndType();
                    if (nodeType == org.basex.query.item.NodeType.ELM)
                    {
                        //xmlNode = new NxElement(this);
                    }
                    else if (nodeType == org.basex.query.item.NodeType.TXT)
                    {
                        //xmlNode = new NxText(this);
                    }
                    else if (nodeType == org.basex.query.item.NodeType.ATT)
                    {
                        //xmlNode = new NxAttribute(this);
                    }
                    else if (nodeType == org.basex.query.item.NodeType.DOC)
                    {
                        //xmlNode = new NxDocument(this);
                    }
                    else if (nodeType == org.basex.query.item.NodeType.COM)
                    {
                        //xmlNode = new NxComment(this);
                    }
                    else if (nodeType == org.basex.query.item.NodeType.PI)
                    {
                        //xmlNode = new NxProcessingInstruction(this);
                    }
                    else
                    {
                        throw new InvalidOperationException("Unexpected node type");
                    }

                    //Cache for later
                    _database.DomCache[_id] = new WeakReference(xmlNode);
                }

                return xmlNode;
            }
        }

        //Checks if the input NxNode is null and if so, returns null - used from Dom implementations
        internal static XmlNode GetXmlNode(NxNode node)
        {
            return node == null ? null : node.XmlNode;
        }

        #endregion
        
        #region Validity

        //Verify the id and pre values still match, and if they don't update the pre
        //Should be checked before every operation (and if invalid, don't perform the operation)
        //Need to check because nodes may have been modified explicilty (through methods) or implicitly (XQuery Update)
        //Returns true if this NxNode is valid (it exists in the database), false otherwise
        public bool Valid
        {
            get
            {
                //If we no longer have a node, then we've been invalidated
                if( _aNode == null )
                {
                    return false;
                }

                //If we're not a database node, then always valid
                if( _dbNode == null )
                {
                    return true;
                }

                //Check if the database has been modified since the last validity check
                long time = _database.GetTime();
                if (_time != time)
                {
                    _time = time;

                    //First check if the pre value is too large (the database shrunk),
                    //then check if the current pre still refers to the same id (do second since it requires disk access)
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

        private void CheckValid(bool requireDatabase = false)
        {
            if( !Valid )
            {
                throw new InvalidOperationException("Node no longer valid");
            }

            if(requireDatabase && _dbNode == null)
            {
                throw new InvalidOperationException("This operation requires a database node");
            }
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

        //Provides typed enumeration for BaseX NodeIter, which are limited to ANode results
        //and thus the enumeration results are guaranteed to produce NxNode objects
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

        public IEnumerable<NxNode> ChildNodes
        {
            get
            {
                CheckValid();
                return EnumerateNodes(_aNode.children());
            }
        }

        public bool HasChildren
        {
            get
            { 
                CheckValid();
                return _aNode.hasChildren();
            }
        }

        public IEnumerable<NxNode> FollowingSiblingNodes
        {
            get
            {
                CheckValid();
                return EnumerateNodes(_aNode.follSibl());
            }
        }

        public IEnumerable<NxNode> PrecedingSiblingNodes
        {
            get
            {
                CheckValid();
                return EnumerateNodes(_aNode.precSibl());
            }
        }

        public IEnumerable<NxNode> FollowingNodes
        {
            get
            {
                CheckValid();
                return EnumerateNodes(_aNode.foll());
            }
        }

        public IEnumerable<NxNode> PrecedingNodes
        {
            get
            {
                CheckValid();
                return EnumerateNodes(_aNode.prec());
            }
        }

        public NxNode ParentNode
        {
            get
            {
                CheckValid();
                ANode node = _aNode.parent();
                return node == null ? null : new NxNode(_database, node);
            }
        }

        public IEnumerable<NxNode> AncestorNodes
        {
            get
            {
                CheckValid();
                return EnumerateNodes(_aNode.anc());
            }
        }

        public IEnumerable<NxNode> AncestorOrSelfNodes
        {
            get
            {
                CheckValid();
                return EnumerateNodes(_aNode.ancOrSelf());
            }
        }

        public IEnumerable<NxNode> DescendantNodes
        {
            get
            {
                CheckValid();
                return EnumerateNodes(_aNode.descendant());
            }
        }

        public IEnumerable<NxNode> DescendantOrSelfNodes
        {
            get
            {
                CheckValid();
                return EnumerateNodes(_aNode.descOrSelf());
            }
        }

        #endregion

        #region Attributes

        public IEnumerable<NxNode> Attributes
        {
            get
            {
                CheckValid(true);
                return EnumerateNodes(_aNode.attributes());
            }
        }

        private ANode GetAttributeANode(string name)
        {
            QNm qnm = new QNm(name.Token());
            return EnumerateANodes(_aNode.attributes()).FirstOrDefault(n => n.qname().eq(qnm));
        }

        public NxNode GetAttributeNode(string name)
        {
            CheckValid();
            if (name == null) throw new ArgumentNullException("name");
            if (name == String.Empty) throw new ArgumentException("name");

            ANode node = GetAttributeANode(name);
            return node == null ? null : new NxNode(_database, node);
        }

        //Returns String.Empty if the attribute doesn't exist
        public string GetAttribute(string name)
        {
            CheckValid();
            if (name == null) throw new ArgumentNullException("name");
            if (name == String.Empty) throw new ArgumentException("name");

            ANode node = GetAttributeANode(name);
            return node == null ? String.Empty : node.atom().Token();
        }
        
        public void RemoveAllAttributes()
        {
            CheckValid(true);

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
            CheckValid(true);
            if (name == null) throw new ArgumentNullException("name");
            if (name == String.Empty) throw new ArgumentException("name");

            DBNode node = GetAttributeANode(name) as DBNode;
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
            CheckValid(true);
            if (name == null) throw new ArgumentNullException("name");
            if (value == null) throw new ArgumentNullException("value");
            if (name == String.Empty) throw new ArgumentException("name");

            using (new UpdateContext())
            {
                Update(new InsertAttribute(_dbNode.pre, _database.Data, null,
                    GetNodeCache(new FAttr(new QNm(name.Token()), value.Token()))));
            }
        }

        #endregion

        #region Reading/Writing

        //Helper to add an update primitive to the open update context with the current database context
        private void Update(UpdatePrimitive update)
        {
            UpdateContext.AddUpdate(update, _database.Context);
        }

        //Helper to generate a NodeCache from a series of ANodes
        private NodeCache GetNodeCache(params ANode[] nodes)
        {
            return new NodeCache(nodes, nodes.Length);
        }

        /// <summary>
        /// Removes all child nodes AND attributes.
        /// </summary>
        public void RemoveAll()
        {
            using (new UpdateContext())
            {
                RemoveAllAttributes();  //Contains the call to CheckValid()
                foreach (DBNode node in EnumerateANodes(_aNode.children()).OfType<DBNode>())
                {
                    Update(new DeleteNode(node.pre, _database.Data, null));
                }
            }
        }

        public void RemoveChild(NxNode node)
        {
            CheckValid(true);
            if (node == null) throw new ArgumentNullException("node");
            if (node._dbNode == null || !node.Database.Equals(_database)) throw new ArgumentException("node must be from the same database");
            if ( !node._aNode.parent().@is(_aNode) ) throw new ArgumentException("node is not a child of this node");

            using (new UpdateContext())
            {
                Update(new DeleteNode(node._dbNode.pre, _database.Data, null));
            }
        }

        ////Removes a specific child (including attributes)
        //public void RemoveChild(NxNode refNode)
        //{
        //    if (refNode == null)
        //    {
        //        throw new ArgumentNullException("refNode");
        //    }
        //    CheckValid(true, Data.ELEM, Data.DOC);
        //    refNode.CheckValid();
        //    if (database.Data.parent(refNode.pre, refNode.kind) != pre)
        //    {
        //        throw new ArgumentException("The specified node is not a direct descendant of this node");
        //    }
        //    RemoveChild(database.Data, refNode.pre);
        //    FinishUpdate();
        //}

        //internal static void RemoveChild(Data data, int refPre)
        //{
        //    data.delete(refPre);
        //}

        //public void ReplaceChild(XmlReader xmlReader, NxNode refNode)
        //{
        //    if (refNode == null)
        //    {
        //        throw new ArgumentNullException("refNode");
        //    }
        //    CheckValid(true, Data.ELEM, Data.DOC);
        //    refNode.CheckValid(true, Data.ELEM, Data.TEXT, Data.COMM, Data.PI);
        //    if (database.Data.parent(refNode.pre, refNode.kind) != pre)
        //    {
        //        throw new ArgumentException("The specified node is not a direct descendant of this node");
        //    }
        //    int ipre = refNode.pre + database.Data.size(refNode.pre, refNode.kind);
        //    DataInserter.Insert(xmlReader, database.Data, ipre, pre);
        //    database.Data.delete(refNode.pre);
        //    FinishUpdate();
        //}

        //public void ReplaceChild(string xmlContent, NxNode refNode)
        //{
        //    StringBasedOperation(xmlContent, refNode, ReplaceChild);
        //}

        //public void Match(XmlReader xmlReader)
        //{
        //    CheckValid(true, Data.ELEM);
        //    DataMatcher.Match(xmlReader, database.Data, pre);
        //    FinishUpdate();
        //}

        //public void Match(string xmlContent)
        //{
        //    StringBasedOperation(xmlContent, Match);
        //}

        ////Flushes the data and optionaly rebuilds indexes
        //private void FinishUpdate()
        //{
        //    database.Data.flush();
        //    if (database.Manager.OptimizeCollectionOnUpdate)
        //    {
        //        database.Optimize();
        //    }
        //}

        //public string InnerXml
        //{
        //    get
        //    {
        //        CheckValid();
        //        StringBuilder stringBuilder = new StringBuilder();
        //        using (XmlWriter xmlWriter = XmlWriter.Create(stringBuilder, NxDatabase.WriterSettings))
        //        {
        //            WriteContentTo(xmlWriter);
        //        }
        //        return stringBuilder.ToString();
        //    }
        //    set
        //    {
        //        StringBasedOperation(value, ReadContentFrom);
        //    }
        //}

        ////Same as InnerXml.set
        //public void ReadContentFrom(XmlReader xmlReader)
        //{
        //    if (xmlReader == null)
        //    {
        //        throw new ArgumentNullException("xmlReader");
        //    }
        //    CheckValid(true, Data.ELEM, Data.DOC);
        //    int ipre = DeleteAllChildren(false);
        //    DataInserter.Insert(xmlReader, database.Data, ipre, pre);
        //    FinishUpdate();
        //}

        //public string OuterXml
        //{
        //    get
        //    {
        //        CheckValid();
        //        StringBuilder stringBuilder = new StringBuilder();
        //        using (XmlWriter xmlWriter = XmlWriter.Create(stringBuilder, NxDatabase.WriterSettings))
        //        {
        //            WriteTo(xmlWriter);
        //        }
        //        return stringBuilder.ToString();
        //    }
        //}

        //public string InnerText
        //{
        //    get
        //    {
        //        using (StringWriter stringWriter = new StringWriter())
        //        {
        //            WriteTextTo(stringWriter);
        //            stringWriter.Close();
        //            return stringWriter.ToString();
        //        }
        //    }
        //    set
        //    {
        //        if(value == null)
        //        {
        //            throw new ArgumentNullException("value");
        //        }
        //        if (CheckValid(Data.ELEM, Data.DOC))
        //        {
        //            int ipre = DeleteAllChildren(false);
        //            DataInserter.Insert(XmlNodeType.Text, database.Data, ipre, pre, null, value);
        //            FinishUpdate();
        //        }
        //    }
        //}

        //public TextReader InnerTextReader
        //{
        //    get
        //    {
        //        CheckValid();
        //        return new NxTextReader(database, pre, kind);
        //    }
        //}

        ////Same as InnerText.get
        //public void WriteTextTo(TextWriter textWriter)
        //{
        //    if (textWriter == null)
        //    {
        //        throw new ArgumentNullException("textWriter");
        //    }
        //    CheckValid();
        //    using (TextWriterSerializer serializer = new TextWriterSerializer(textWriter))
        //    {
        //        serializer.node(database.Data, pre);
        //    }
        //}

        ////Same as OuterXml
        //public void WriteTo(XmlWriter xmlWriter)
        //{
        //    if (xmlWriter == null)
        //    {
        //        throw new ArgumentNullException("xmlWriter");
        //    }
        //    if (CheckValid(Data.ELEM, Data.DOC))
        //    {
        //        using (XmlWriterSerializer serializer = new XmlWriterSerializer(xmlWriter, kind == Data.DOC))
        //        {
        //            serializer.node(database.Data, pre);
        //        }
        //    }
        //}

        ////Same as InnerXml
        //public void WriteContentTo(XmlWriter xmlWriter)
        //{
        //    if (xmlWriter == null)
        //    {
        //        throw new ArgumentNullException("xmlWriter");
        //    }
        //    if (CheckValid(Data.ELEM, Data.DOC))
        //    {
        //        using (XmlWriterSerializer serializer = new XmlWriterSerializer(xmlWriter, false))
        //        {
        //            foreach (int childPre in GetChildPres(database.Data, pre, kind, false))
        //            {
        //                serializer.node(database.Data, childPre);
        //            }
        //        }
        //    }
        //}

        //public void InsertAfter(XmlReader xmlReader, NxNode refNode)
        //{
        //    if (xmlReader == null)
        //    {
        //        throw new ArgumentNullException("xmlReader");
        //    }
        //    if (refNode == null)
        //    {
        //        throw new ArgumentNullException("refNode");
        //    }
        //    CheckValid(true, Data.ELEM, Data.DOC);
        //    refNode.CheckValid(true, Data.ELEM, Data.TEXT, Data.COMM, Data.PI);
        //    if (database.Data.parent(refNode.pre, refNode.kind) != pre)
        //    {
        //        throw new ArgumentException("The specified node is not a direct descendant of this node");
        //    }
        //    int ipre = refNode.pre + database.Data.size(refNode.pre, refNode.kind);
        //    DataInserter.Insert(xmlReader, database.Data, ipre, pre);
        //    FinishUpdate();
        //}

        //public void InsertAfter(XmlNodeType nodeType, string name, string value, NxNode refNode)
        //{
        //    if (refNode == null)
        //    {
        //        throw new ArgumentNullException("refNode");
        //    }
        //    CheckValid(true, Data.ELEM, Data.DOC);
        //    refNode.CheckValid(true, Data.ELEM, Data.TEXT, Data.COMM, Data.PI);
        //    if (database.Data.parent(refNode.pre, refNode.kind) != pre)
        //    {
        //        throw new ArgumentException("The specified node is not a direct descendant of this node");
        //    }
        //    int ipre = refNode.pre + database.Data.size(refNode.pre, refNode.kind);
        //    DataInserter.Insert(nodeType, database.Data, ipre, pre, name, value);
        //    FinishUpdate();
        //}

        //public void InsertBefore(XmlReader xmlReader, NxNode refNode)
        //{
        //    if (xmlReader == null)
        //    {
        //        throw new ArgumentNullException("xmlReader");
        //    }
        //    if (refNode == null)
        //    {
        //        throw new ArgumentNullException("refNode");
        //    }
        //    CheckValid(true, Data.ELEM, Data.DOC);
        //    refNode.CheckValid(true, Data.ELEM, Data.TEXT, Data.COMM, Data.PI);
        //    if (database.Data.parent(refNode.pre, refNode.kind) != pre)
        //    {
        //        throw new ArgumentException("The specified node is not a direct descendant of this node");
        //    }
        //    InsertBefore(database.Data, pre, xmlReader, refNode.pre);
        //    FinishUpdate();
        //}

        //internal static void InsertBefore(Data data, int pre, XmlReader xmlReader, int refPre)
        //{
        //    DataInserter.Insert(xmlReader, data, refPre, pre);
        //}

        //public void InsertBefore(XmlNodeType nodeType, string name, string value, NxNode refNode)
        //{
        //    if (refNode == null)
        //    {
        //        throw new ArgumentNullException("refNode");
        //    }
        //    CheckValid(true, Data.ELEM, Data.DOC);
        //    refNode.CheckValid(true, Data.ELEM, Data.TEXT, Data.COMM, Data.PI);
        //    if (database.Data.parent(refNode.pre, refNode.kind) != pre)
        //    {
        //        throw new ArgumentException("The specified node is not a direct descendant of this node");
        //    }
        //    InsertBefore(database.Data, pre, nodeType, name, value, refNode.pre);
        //    FinishUpdate();
        //}

        //internal static void InsertBefore(Data data, int pre, XmlNodeType nodeType, string name, string value, int refPre)
        //{
        //    DataInserter.Insert(nodeType, data, refPre, pre, name, value);
        //}

        //public void AppendChild(XmlReader xmlReader)
        //{
        //    if (xmlReader == null)
        //    {
        //        throw new ArgumentNullException("xmlReader");
        //    }
        //    CheckValid(true, Data.ELEM, Data.DOC);
        //    AppendChild(database.Data, pre, kind, xmlReader);
        //    FinishUpdate();
        //}

        //internal static void AppendChild(Data data, int pre, int kind, XmlReader xmlReader)
        //{
        //    int ipre = pre + data.size(pre, kind);
        //    DataInserter.Insert(xmlReader, data, ipre, pre);
        //}

        //public void AppendChild(XmlNodeType nodeType, string name, string value)
        //{
        //    CheckValid(true, Data.ELEM, Data.DOC);
        //    AppendChild(database.Data, pre, kind, nodeType, name, value);
        //    FinishUpdate();
        //}

        //internal static void AppendChild(Data data, int pre, int kind, XmlNodeType nodeType, string name, string value)
        //{
        //    int ipre = pre + data.size(pre, kind);
        //    DataInserter.Insert(nodeType, data, ipre, pre, name, value);
        //}

        //public void PrependChild(XmlReader xmlReader)
        //{
        //    if (xmlReader == null)
        //    {
        //        throw new ArgumentNullException("xmlReader");
        //    }
        //    CheckValid(true, Data.ELEM);
        //    int ipre = pre + database.Data.attSize(pre, kind);
        //    DataInserter.Insert(xmlReader, database.Data, ipre, pre);
        //    FinishUpdate();
        //}

        //public void PrependChild(XmlNodeType nodeType, string name, string value)
        //{
        //    CheckValid(true, Data.ELEM);
        //    int ipre = pre + database.Data.attSize(pre, kind);
        //    DataInserter.Insert(nodeType, database.Data, ipre, pre, name, value);
        //    FinishUpdate();
        //}

        //private static void StringBasedOperation(string xmlContent, Action<XmlReader> action)
        //{
        //    if (xmlContent == null)
        //    {
        //        throw new ArgumentNullException("xmlContent");
        //    }
        //    using (StringReader stringReader = new StringReader(xmlContent))
        //    {
        //        using (XmlReader xmlReader = XmlReader.Create(stringReader, NxDatabase.ReaderSettings))
        //        {
        //            action(xmlReader);
        //        }
        //    }
        //}

        //private static void StringBasedOperation(string xmlContent, NxNode refNode, Action<XmlReader, NxNode> action)
        //{
        //    if (xmlContent == null)
        //    {
        //        throw new ArgumentNullException("xmlContent");
        //    }
        //    using (StringReader stringReader = new StringReader(xmlContent))
        //    {
        //        using (XmlReader xmlReader = XmlReader.Create(stringReader, NxDatabase.ReaderSettings))
        //        {
        //            action(xmlReader, refNode);
        //        }
        //    }
        //}

        //public void InsertAfter(string xmlContent, NxNode refNode)
        //{
        //    StringBasedOperation(xmlContent, refNode, InsertAfter);
        //}

        //public void InsertBefore(string xmlContent, NxNode refNode)
        //{
        //    StringBasedOperation(xmlContent, refNode, InsertBefore);
        //}

        //public void AppendChild(string xmlContent)
        //{
        //    StringBasedOperation(xmlContent, AppendChild);
        //}

        //public void PrependChild(string xmlContent)
        //{
        //    StringBasedOperation(xmlContent, PrependChild);
        //}

        /// <summary>
        /// Gets or sets the value. Returns an empty string if no value.
        /// </summary>
        /// <value>
        /// Document/Element: inner text
        /// Text/Comment: text content
        /// Attribute: value
        /// Processing Instruction: text content (without the target)
        /// </value>
        public string Value
        {
            get
            {
                CheckValid();
                return _aNode.atom().Token();
            }
            set
            {
                throw new NotImplementedException();
                //if (value == null)
                //{
                //    throw new ArgumentNullException("value");
                //}
                //CheckValid(true, Data.TEXT, Data.ATTR, Data.COMM, Data.PI);
                //SetValue(database.Data, pre, kind, value);
                //FinishUpdate();
            }
        }

        //internal static string GetValue(Data data, int pre, int kind)
        //{
        //    return Token.@string(data.text(pre, kind != Data.ATTR));
        //}

        //internal static void SetValue(Data data, int pre, int kind, string value)
        //{
        //    data.replace(pre, kind, Token.token(value));
        //}

        //public string Name
        //{
        //    get
        //    {
        //        if( CheckValid(Data.ELEM, Data.ATTR, Data.PI) )
        //        {
        //            return GetName(database.Data, pre, kind);
        //        }
        //        return String.Empty;
        //    }
        //    set
        //    {
        //        if (value == null)
        //        {
        //            throw new ArgumentNullException("value");
        //        }
        //        CheckValid(true, Data.ELEM, Data.ATTR, Data.PI);
        //        if (!XmlReader.IsName(value))
        //        {
        //            throw new XmlException("Invalid XML name");
        //        }
        //        byte[] name = Token.token(value);
        //        byte[] uri = value.IndexOf(':') > 0
        //            ? database.Data._field_ns.uri(database.Data._field_ns.uri(name, pre))
        //            : new byte[]{};
        //        database.Data.rename(pre, kind, Token.token(value), uri);
        //        FinishUpdate();
        //    }
        //}

        //internal static string GetName(Data data, int pre, int kind)
        //{
        //    return Token.@string(data.name(pre, kind));
        //}

        /// <summary>
        /// Gets the name of this node. Returns an empty string if not an element or attribute.
        /// </summary>
        public string Name
        {
            get
            {
                CheckValid();
                if(CheckType(org.basex.query.item.NodeType.ELM,
                    org.basex.query.item.NodeType.ATT))
                {
                    return _aNode.nname().Token();
                }
                return String.Empty;
            }
        }

        //public string LocalName
        //{
        //    get
        //    {
        //        if (CheckValid(Data.ELEM, Data.ATTR, Data.PI))
        //        {
        //            byte[] name = database.Data.name(pre, kind);
        //            byte[] localName = Token.ln(name);
        //            if (localName.Length > 0)
        //            {
        //                return Token.@string(localName);
        //            }
        //            return Token.@string(name);
        //        }
        //        return String.Empty;
        //    }
        //}

        //public string Prefix
        //{
        //    get
        //    {
        //        if (CheckValid(Data.ELEM, Data.ATTR, Data.PI))
        //        {
        //            return Token.@string(Token.pref(database.Data.name(pre, kind)));
        //        }
        //        return String.Empty;
        //    }
        //}

        //public string NamespaceURI
        //{
        //    get
        //    {
        //        if (CheckValid(Data.ELEM, Data.ATTR, Data.PI))
        //        {
        //            byte[] name = database.Data.name(pre, kind);
        //            byte[] pref = Token.pref(name);
        //            if(pref.Length > 0)
        //            {
        //                return Token.@string(database.Data._field_ns.uri(database.Data._field_ns.uri(name, pre)));
        //            }
        //        }
        //        return String.Empty;
        //    }
        //}

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
