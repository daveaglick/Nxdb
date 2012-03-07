/*
 * Copyright 2012 WildCard, LLC
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 * 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using org.basex.data;
using org.basex.query.item;
using org.basex.query.iter;
using org.basex.query.up.expr;

namespace Nxdb.Node
{
    /// <summary>
    /// The base class for XML nodes in Nxdb. There are two types of nodes, database nodes and query nodes (or
    /// non-database nodes). Database nodes contain a database ID (accessible via the Id property) and can be
    /// manipulated (which will change the content in the database on disk). Query nodes are immutable and cannot
    /// be modified. Attempting to modify a query node will result in a NotSupportedException. Database nodes are
    /// obtained whenever you directly access the database or execute a query that returns results from the database.
    /// Query nodes are obtained whenever you execute a query that returns temporary or non-database results or when
    /// you create a node directly or from XmlNodes or XNodes. The check which kind of node this is look at the
    /// Id property. If it is >= 0 the node is a database node; if it is -1 the node is a query node.
    /// </summary>
    public abstract class Node : IEquatable<Node>, IQuery
    {
        private ANode _aNode;  // This should be updated before every use by calling Valid.get, if null then invalid
        private DBNode _dbNode; // This should be set if the node is a database node
        private FNode _fNode; // This should be set if the node is a result node
        private XmlNode _xmlNode = null; // Cached XmlNode inastance for DOM interoperability
        private readonly int _id = -1; // The unique immutable ID for the node, -1 if not a database node
        private readonly int _kind; // The database kind supported by the subclass
        private readonly Database _database = null; // Cache the database that this node belongs to

        #region Construction

        protected Node(ANode aNode, int kind, Database database)
        {
            _dbNode = aNode as DBNode;
            if (aNode == null) throw new ArgumentNullException("aNode");
            if (aNode.kind() != kind) throw new ArgumentException("Incorrect node type");
            _aNode = aNode;
            _fNode = aNode as FNode;
            _kind = kind;
            _database = database;
            if (_dbNode != null)
            {
                _id = _dbNode.data().id(_dbNode.pre);
            }
        }

        internal static Node Get(ANode aNode)
        {
            if (aNode == null) throw new ArgumentNullException("aNode");

            // Is this a database node?
            DBNode dbNode = aNode as DBNode;
            if (dbNode != null)
            {
                Database database = Database.Get(dbNode.data());
                return Get(dbNode, database);
            }

            // If not, create the appropriate non-database node class
            NodeType nodeType = aNode.nodeType();
            if (nodeType == org.basex.query.item.NodeType.ELM)
            {
                return new Element(aNode, null);
            }
            if (nodeType == org.basex.query.item.NodeType.TXT)
            {
                return new Text(aNode, null);
            }
            if (nodeType == org.basex.query.item.NodeType.ATT)
            {
                return new Attribute(aNode, null);
            }
            if (nodeType == org.basex.query.item.NodeType.DOC)
            {
                return new Document(aNode, null);
            }
            if (nodeType == org.basex.query.item.NodeType.COM)
            {
                return new Comment(aNode, null);
            }
            if (nodeType == org.basex.query.item.NodeType.PI)
            {
                return new ProcessingInstruction(aNode, null);
            }
            throw new ArgumentException("Invalid node type");
        }

        internal static Node Get(int pre, Data data)
        {
            if (data == null) throw new ArgumentNullException("data");
            if (pre < 0) throw new ArgumentOutOfRangeException("pre");
            
            //Is it already created?
            Database database = Database.Get(data);
            Node node = database.GetNode(pre);
            return node ?? Get(new DBNode(data, pre), database, false);
        }
        
        internal static Node Get(DBNode dbNode, Database database, bool check = true)
        {
            if (dbNode == null) throw new ArgumentNullException("dbNode");
            if (database == null) throw new ArgumentNullException("database");

            // Copy the DBNode if it wasn't created from scratch
            if (check)
            {
                // Is it already created?
                Node node = database.GetNode(dbNode.pre);
                if (node != null)
                {
                    return node;
                }

                // Otherwise, copy the DBNode so we can make a new Node
                dbNode = dbNode.copy();
            }

            // Create the appropriate database node class
            Node newNode;
            NodeType nodeType = dbNode.nodeType();
            if (nodeType == org.basex.query.item.NodeType.ELM)
            {
                newNode = new Element(dbNode, database);
            }
            else if (nodeType == org.basex.query.item.NodeType.TXT)
            {
                newNode = new Text(dbNode, database);
            }
            else if (nodeType == org.basex.query.item.NodeType.ATT)
            {
                newNode = new Attribute(dbNode, database);
            }
            else if (nodeType == org.basex.query.item.NodeType.DOC)
            {
                newNode = new Document(dbNode, database);
            }
            else if (nodeType == org.basex.query.item.NodeType.COM)
            {
                newNode = new Comment(dbNode, database);
            }
            else if (nodeType == org.basex.query.item.NodeType.PI)
            {
                newNode = new ProcessingInstruction(dbNode, database);
            }
            else
            {
                throw new ArgumentException("Invalid node type");
            }

            // Cache and return the node
            database.SetNode(dbNode.pre, newNode);
            return newNode;
        }

        /// <summary>
        /// Gets a new node by copying from an XmlNode.
        /// </summary>
        /// <param name="node">The node to copy from.</param>
        /// <returns></returns>
        public static Node Get(XmlNode node)
        {
            IList<ANode> nodes = Helper.GetNodes(new XmlNodeReader(node));
            if (nodes.Count != 1) throw new Exception("Unexpected behavior: the XmlNode resulted in more than one Node");
            return Get(nodes[0]);
        }

        /// <summary>
        /// Gets the a new node by copying from an XNode.
        /// </summary>
        /// <param name="node">The XNode to copy from.</param>
        /// <returns></returns>
        public static Node Get(XNode node)
        {
            IList<ANode> nodes = Helper.GetNodes(node.CreateReader());
            if (nodes.Count != 1) throw new Exception("Unexpected behavior: the XNode resulted in more than one Node");
            return Get(nodes[0]);
        }

        #endregion
        
        #region Properties

        /// <summary>
        /// Gets the database that this node belongs to or null if not a database node.
        /// </summary>
        public Database Database
        {
            get { return _database; }
        }

        protected internal ANode ANode
        {
            get { return _aNode; }
        }

        protected DBNode DbNode
        {
            get { return _dbNode; }
        }

        protected FNode FNode
        {
            get { return _fNode; }
        }

        /// <summary>
        /// Gets the unique immutable database ID of this node or -1 if this is not a database node.
        /// </summary>
        public int Id
        {
            get
            {
                Check();
                return _id;
            }
        }

        /// <summary>
        /// Gets the database index for the node or -1 if this is not a database node.
        /// The database index potentially changes with every database
        /// update, so this value may be different between calls.
        /// </summary>
        public int Index
        {
            get
            {
                Check();
                return DbNode != null ? DbNode.pre : -1;
            }
        }

        public abstract XmlNodeType NodeType { get; }

        #endregion

        #region Validity

        // Updates.Apply() -> Database.Update() -> Node.Validate()
        internal bool Validate()
        {
            // If no ANode, then we're invalid
            if(_aNode == null)
            {
                return false;
            }

            // If we're not a database node, then always valid
            if (DbNode == null)
            {
                return true;
            }

            // First check if the pre value is too large (the database shrunk),
            // then check if the current pre still refers to the same id
            // (do second since it requires disk access and is thus a little slower)
            if (DbNode.pre >= DbNode.data().meta.size || _id != DbNode.data().id(DbNode.pre))
            {
                int pre = DbNode.data().pre(_id);
                if (pre == -1)
                {
                    Invalidate();
                    return false;
                }
                DbNode.set(pre, _kind);    // Assume that the kind is the same since we found the same ID
            }
            return true;
        }

        /// <summary>
        /// Occurs when this node is invalidated.
        /// </summary>
        public event EventHandler<EventArgs> Invalidated;

        private void Invalidate()
        {
            _aNode = null;
            _dbNode = null;
            _fNode = null;
            _xmlNode = null;
            EventHandler<EventArgs> handler = Invalidated;
            if (handler != null) handler(this, EventArgs.Empty);
        }

        /// <summary>
        /// Gets a value indicating whether this node is valid. Non-database nodes
        /// are always valid.
        /// </summary>
        /// <value>
        ///   <c>true</c> if valid; otherwise, <c>false</c>.
        /// </value>
        public bool Valid
        {
            get { return _aNode != null; }
        }

        // Checks node validity and optionally checks if this is a database node
        protected void Check(bool requireDatabase = false)
        {
            if (_aNode == null) throw new InvalidOperationException("the node is no longer valid");
            if (requireDatabase && DbNode == null)
                throw new NotSupportedException("this operation is only supported for database nodes");
        }

        #endregion

        #region Enumeration

        // Provides typed enumeration for BaseX NodeIter, which are limited to ANode results
        // and thus the enumeration results are guaranteed to produce Node objects
        private IEnumerable<T> EnumerateNodes<T>(NodeIter iter)
        {
            Check();
            return new IterEnum(iter).Cast<T>();
        }

        // Enumerate the BaseX ANodes in a NodeIter
        // Also, does not do a validity check
        protected IEnumerable<ANode> EnumerateANodes(NodeIter iter)
        {
            while (true)
            {
                ANode node = iter.next();
                if (node == null) yield break;
                yield return node.copy();
            }
        }

        /// <summary>
        /// Gets the children of this node. An empty sequence will be returned if the node
        /// does not support children (such as attribute nodes).
        /// Attributes are not included as part of this sequence and must be enumerated with
        /// the Attributes property.
        /// </summary>
        public IEnumerable<TreeNode> Children
        {
            get
            {
                return EnumerateNodes<TreeNode>(ANode.children());
            }
        }

        /// <summary>
        /// Gets the child node at a specified index.
        /// </summary>
        /// <param name="index">The index of the child to return.</param>
        /// <returns>The child node at the specified index or null if no
        /// child node could be found or if the node does not support children (such as
        /// attribute nodes).</returns>
        public TreeNode Child(int index)
        {
            return Children.ElementAtOrDefault(index);
        }

        /// <summary>
        /// Gets the attributes. Per the XML standard, the ordering of attributes is
        /// undefined and should not be considered relevant or consistent. An empty
        /// sequence will be returned if the node is not an element.
        /// </summary>
        public IEnumerable<Attribute> Attributes
        {
            get
            {
                return EnumerateNodes<Attribute>(ANode.attributes());
            }
        }

        /// <summary>
        /// Gets the following sibling nodes.
        /// </summary>
        public IEnumerable<TreeNode> FollowingSiblings
        {
            get
            {
                return EnumerateNodes<TreeNode>(ANode.followingSibling());
            }
        }

        /// <summary>
        /// Gets the preceding sibling nodes.
        /// </summary>
        public IEnumerable<TreeNode> PrecedingSiblings
        {
            get
            {
                return EnumerateNodes<TreeNode>(ANode.precedingSibling());
            }
        }
        
        /// <summary>
        /// Gets the following nodes.
        /// </summary>
        public IEnumerable<TreeNode> Following
        {
            get
            {
                return EnumerateNodes<TreeNode>(ANode.following());
            }
        }

        /// <summary>
        /// Gets the preceding nodes.
        /// </summary>
        public IEnumerable<TreeNode> Preceding
        {
            get
            {
                return EnumerateNodes<TreeNode>(ANode.preceding());
            }
        }

        /// <summary>
        /// Gets the parent node. The return value will be null if the node
        /// does not have a parent node (such as a stand-alone result node).
        /// Unlike XML DOM implementations, the parent of an attribute is the container
        /// element. Also, the parent of a document root element is the document.
        /// </summary>
        public ContainerNode Parent
        {
            get
            {
                Check();
                ANode parent = ANode.parent();
                if (parent != null)
                {
                    return (ContainerNode) Get(parent);
                }
                return null;
            }
        }

        /// <summary>
        /// Gets the ancestor nodes.
        /// </summary>
        public IEnumerable<ContainerNode> Ancestors
        {
            get
            {
                return EnumerateNodes<ContainerNode>(ANode.ancestor());
            }
        }

        /// <summary>
        /// Gets the ancestor nodes and current node.
        /// </summary>
        public IEnumerable<TreeNode> AncestorsOrSelf
        {
            get
            {
                return EnumerateNodes<TreeNode>(ANode.ancestorOrSelf());
            }
        }

        public Document Document
        {
            get
            {
                Check();
                foreach (ANode ancestor in EnumerateANodes(ANode.ancestorOrSelf()))
                {
                    if (ancestor.nodeType() == org.basex.query.item.NodeType.DOC)
                    {
                        return Get(ancestor) as Document;
                    }
                }
                return null;
            }
        }

        /// <summary>
        /// Gets the descendant nodes.
        /// </summary>
        public IEnumerable<TreeNode> Descendants
        {
            get
            {
                return EnumerateNodes<TreeNode>(ANode.descendant());
            }
        }

        /// <summary>
        /// Gets the descendant nodes and current node.
        /// </summary>
        public IEnumerable<TreeNode> DescendantsOrSelf
        {
            get
            {
                return EnumerateNodes<TreeNode>(ANode.descendantOrSelf());
            }
        }

        #endregion

        #region Content
        
        /// <summary>
        /// Removes this node from the database.
        /// </summary>
        public void Remove()
        {
            Check(true);
            Updates.Add(new Delete(null, DbNode));
        }

        /// <summary>
        /// Gets or sets the value. Returns an empty string if no value.
        /// Document/Element: same as InnerText
        /// Text/Comment: text content
        /// Attribute: value
        /// Processing Instruction: text content (without the target)
        /// </summary>
        public virtual string Value
        {
            get
            {
                Check();
                return ANode.@string().Token();
            }
            set
            {
                if (value == null) throw new ArgumentNullException("value");
                Check(true);
                Updates.Add(new Replace(null, DbNode, new Atm(value.Token()), true));
            }
        }

        /// <summary>
        /// Gets or sets the fully-qualified name of this node for elements, attributes,
        /// and processing instructions. For documents, this returns the document name.
        /// Returns an empty string for all others.
        /// </summary>
        public virtual string Name
        {
            get
            {
                Check();
                return String.Empty;
            }
            set
            {
                if (value == null) throw new ArgumentNullException("value");
                Check(true);
            }
        }

        // Contains the implementation - should be called only by Element, Attribute, and ProcessingInstruction
        protected string NameImpl
        {
            get
            {
                Check();
                return ANode.name().Token();
            }
            set
            {
                if (value == null) throw new ArgumentNullException("value");
                Check(true);
                Updates.Add(new Rename(null, DbNode, new QNm(value.Token())));
            }
        }

        /// <summary>
        /// Gets the local name of this node for elements, attributes,
        /// and processing instructions. Returns an empty string for all others.
        /// </summary>
        public virtual string LocalName
        {
            get
            {
                Check();
                return String.Empty;
            }
        }

        // Contains the implementation - should be called only by Element, Attribute, and ProcessingInstruction
        protected string LocalNameImpl
        {
            get
            {
                Check();
                return ANode.qname().local().Token();
            }
        }

        /// <summary>
        /// Gets the prefix of this node for elements, attributes,
        /// and processing instructions. Returns an empty string for all others.
        /// </summary>
        public virtual string Prefix
        {
            get
            {
                Check();
                return String.Empty;
            }
        }

        // Contains the implementation - should be called only by Element, Attribute, and ProcessingInstruction
        protected string PrefixImpl
        {
            get
            {
                Check();
                return ANode.qname().prefix().Token();
            }
        }

        /// <summary>
        /// Gets the namespace of this node for elements, attributes,
        /// and processing instructions. Returns an empty string for all others.
        /// </summary>
        public virtual string NamespaceUri
        {
            get
            {
                Check();
                return String.Empty;
            }
        }

        // Contains the implementation - should be called only by Element, Attribute, and ProcessingInstruction
        protected string NamespaceUriImpl
        {
            get
            {
                Check();
                return ANode.qname().uri().Token();
            }
        }

        /// <summary>
        /// Gets the base URI of this node or an empty string if none is available.
        /// </summary>
        public string BaseUri
        {
            get
            {
                Check();
                return ANode.baseURI().Token();
            }
        }

        #endregion

        #region IQuery

        /// <inheritdoc />
        public IEnumerable<object> Eval(string expression)
        {
            return new Query(this).Eval(expression);
        }

        /// <inheritdoc />
        public IEnumerable<T> Eval<T>(string expression)
        {
            return Eval(expression).OfType<T>();
        }

        /// <inheritdoc />
        public IList<object> EvalList(string expression)
        {
            return new List<object>(Eval(expression));
        }

        /// <inheritdoc />
        public IList<T> EvalList<T>(string expression)
        {
            return new List<T>(Eval(expression).OfType<T>());
        }

        /// <inheritdoc />
        public object EvalSingle(string expression)
        {
            return Eval(expression).FirstOrDefault();
        }

        /// <inheritdoc />
        public T EvalSingle<T>(string expression) where T : class
        {
            return Eval<T>(expression).FirstOrDefault();
        }

        #endregion

        #region Dom
        
        public XmlNode XmlNode
        {
            get
            {
                Check();
                return _xmlNode ?? (_xmlNode = CreateXmlNode());
            }
        }

        protected abstract XmlNode CreateXmlNode();

        #endregion

        #region Equality

        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type. For database nodes,
        /// two objects are equal if they represent the same node in the database. For non-database nodes,
        /// two objects are equal if they hold a reference to the same underlying node. Invalid nodes always
        /// compare as unequal.
        /// </summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns>
        /// true if the current object is equal to the <paramref name="other"/> parameter; otherwise, false.
        /// </returns>
        public bool Equals(Node other)
        {
            if (other == null)
            {
                return false;
            }
            if (DbNode != null)
            {
                return this == other;
            }
            return ANode.id == other.ANode.id;
        }

        /// <summary>
        /// Determines whether the specified <see cref="System.Object"/> is equal to this instance.
        /// </summary>
        /// <param name="other">The <see cref="System.Object"/> to compare with this instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="System.Object"/> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object other)
        {
            Node node = other as Node;
            return node != null && Equals(node);
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode()
        {
            int result = 17;
            if (DbNode != null)
            {
                result = 37*result + _id.GetHashCode();
                result = 37*result + DbNode.data().GetHashCode();
            }
            else if (ANode != null)
            {
                result = 37*ANode.id;
            }
            return result;
        }

        #endregion

    }
}
