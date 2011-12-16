using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using org.basex.query.item;
using org.basex.query.iter;
using org.basex.query.up.primitives;

namespace Nxdb
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
    public abstract class Node : IEquatable<Node>
    {
        private readonly Database _database = null; // The database this node belongs to or null if not a database node
        private readonly ANode _aNode;  // This should be updated before every use by calling Valid.get
        private readonly DBNode _dbNode; // This should be set if the node is a database node
        private readonly FNode _fNode; // This should be set if the node is a result node
        private readonly int _id = -1; // The unique immutable ID for the node, -1 if not a database node
        private readonly int _kind; // The database kind supported by the subclass
        private long _time = -1; // Cache the last modified time to avoid checking pre against id on every operation, also invalid if MinValue

        #region Construction

        protected Node(ANode aNode, Database database, int kind)
        {
            _dbNode = aNode as DBNode;
            if (aNode == null) throw new ArgumentNullException("aNode");
            if (_dbNode != null && database == null) throw new ArgumentNullException("database");
            if (ANode.kind(aNode.ndType()) != kind) throw new ArgumentException("Incorrect node type");
            _aNode = aNode;
            _fNode = aNode as FNode;
            _kind = kind;
            if (_dbNode != null)
            {
                _database = database;
                _id = database.GetId(_dbNode.pre);
                _time = database.GetTime();
            }
        }

        internal static Node Get(ANode aNode, Database database = null)
        {
            if (aNode == null) throw new ArgumentNullException("aNode");

            // Is this a database node?
            DBNode dbNode = aNode as DBNode;
            if (dbNode != null)
            {
                return Get(dbNode, database);
            }

            // If not, create the appropriate non-database node class
            NodeType nodeType = aNode.ndType();
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

        internal static Node Get(int pre, Database database)
        {
            if (database == null) throw new ArgumentNullException("database");
            if (pre < 0) throw new ArgumentOutOfRangeException("pre");
            return Get(new DBNode(database.Data, pre), database, false);
        }

        internal static Node Get(DBNode dbNode, Database database, bool copy = true)
        {
            if (database == null) throw new ArgumentNullException("database");
            if (dbNode == null) throw new ArgumentNullException("dbNode");

            // Copy the DBNode if it wasn't created from scratch
            if (copy)
            {
                dbNode = dbNode.copy();
            }

            // Create the appropriate database node class
            NodeType nodeType = dbNode.ndType();
            if (nodeType == org.basex.query.item.NodeType.ELM)
            {
                return new Element(dbNode, database);
            }
            if (nodeType == org.basex.query.item.NodeType.TXT)
            {
                return new Text(dbNode, database);
            }
            if (nodeType == org.basex.query.item.NodeType.ATT)
            {
                return new Attribute(dbNode, database);
            }
            if (nodeType == org.basex.query.item.NodeType.DOC)
            {
                return new Document(dbNode, database);
            }
            if (nodeType == org.basex.query.item.NodeType.COM)
            {
                return new Comment(dbNode, database);
            }
            if (nodeType == org.basex.query.item.NodeType.PI)
            {
                return new ProcessingInstruction(dbNode, database);
            }
            throw new ArgumentException("Invalid node type");
        }

        public static Node Get(XmlNode node)
        {
            IList<ANode> nodes = Helper.GetNodes(new XmlNodeReader(node));
            if (nodes.Count != 1) throw new Exception("Unexpected behavior: the XmlNode resulted in more than one Node");
            return Get(nodes[0]);
        }

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

        /// <summary>
        /// Gets a value indicating whether this node is valid. Non-database nodes
        /// are always valid.
        /// </summary>
        /// <value>
        ///   <c>true</c> if valid; otherwise, <c>false</c>.
        /// </value>
        public bool Valid
        {
            get
            {
                // If time == minimum value we've been invalidated
                if (_time == long.MinValue)
                {
                    return false;
                }

                // If we're not a database node, then always valid
                if (DbNode == null)
                {
                    return true;
                }

                // Check if the database has been modified since the last validity check
                long time = _database.GetTime();
                if (_time != time)
                {
                    _time = time;

                    // First check if the pre value is too large (the database shrunk),
                    // then check if the current pre still refers to the same id
                    // (do second since it requires disk access and is thus a little slower)
                    if (DbNode.pre >= _database.GetSize() || _id != _database.GetId(DbNode.pre))
                    {
                        int pre = _database.GetPre(_id);
                        if (pre == -1)
                        {
                            Invalidate();
                            return false;
                        }
                        DbNode.set(pre, _kind);    // Assume that the kind is the same since we found the same ID
                    }
                }

                return true;
            }
        }

        protected void Invalidate()
        {
            _time = long.MinValue;
        }

        // Checks node validity and optionally checks if this is a database node
        protected void Check(bool requireDatabase = false)
        {
            if (!Valid) throw new InvalidOperationException("the node is no longer valid");
            if (requireDatabase && DbNode == null)
                throw new NotSupportedException("this operation is only supported for database nodes");
        }

        #endregion

        #region Enumeration

        // Provides typed enumeration for BaseX NodeIter, which are limited to ANode results
        // and thus the enumeration results are guaranteed to produce NxNode objects
        protected IEnumerable<T> EnumerateNodes<T>(NodeIter iter)
        {
            Check();
            return new IterEnum(iter, Database).Cast<T>();
        }

        // Enumerate the BaseX ANodes in a NodeIter
        protected IEnumerable<ANode> EnumerateANodes(NodeIter iter)
        {
            while (true)
            {
                ANode node = iter.next();
                if (node == null) yield break;
                yield return node;
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
            get { return EnumerateNodes<TreeNode>(ANode.children()); }
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
            get { return EnumerateNodes<Attribute>(ANode.attributes()); }
        }

        /// <summary>
        /// Gets the following sibling nodes.
        /// </summary>
        public IEnumerable<TreeNode> FollowingSiblings
        {
            get { return EnumerateNodes<TreeNode>(ANode.follSibl()); }
        }

        /// <summary>
        /// Gets the preceding sibling nodes.
        /// </summary>
        public IEnumerable<TreeNode> PrecedingSiblings
        {
            get { return EnumerateNodes<TreeNode>(ANode.precSibl()); }
        }
        
        /// <summary>
        /// Gets the following nodes.
        /// </summary>
        public IEnumerable<TreeNode> Following
        {
            get { return EnumerateNodes<TreeNode>(ANode.foll()); }
        }

        /// <summary>
        /// Gets the preceding nodes.
        /// </summary>
        public IEnumerable<TreeNode> Preceding
        {
            get { return EnumerateNodes<TreeNode>(ANode.prec()); }
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
                    return (ContainerNode)Get(parent, Database);
                }
                return null;
            }
        }

        /// <summary>
        /// Gets the ancestor nodes.
        /// </summary>
        public IEnumerable<ContainerNode> Ancestors
        {
            get { return EnumerateNodes<ContainerNode>(ANode.anc()); }
        }

        /// <summary>
        /// Gets the ancestor nodes and current node.
        /// </summary>
        public IEnumerable<TreeNode> AncestorsOrSelf
        {
            get { return EnumerateNodes<TreeNode>(ANode.ancOrSelf()); }
        }

        /// <summary>
        /// Gets the descendant nodes.
        /// </summary>
        public IEnumerable<TreeNode> Descendants
        {
            get { return EnumerateNodes<TreeNode>(ANode.descendant()); }
        }

        /// <summary>
        /// Gets the descendant nodes and current node.
        /// </summary>
        public IEnumerable<TreeNode> DescendantsOrSelf
        {
            get { return EnumerateNodes<TreeNode>(ANode.descOrSelf()); }
        }

        #endregion

        #region Content

        // Helper to add an update primitive to the open update context with the current database context
        protected void Update(UpdatePrimitive update)
        {
            UpdateContext.AddUpdate(update, _database.Context);
        }

        /// <summary>
        /// Removes this node from the database and invalidates it.
        /// </summary>
        public void Remove()
        {
            Check(true);
            using (new UpdateContext())
            {
                Update(new DeleteNode(DbNode.pre, Database.Data, null));
            }
            Invalidate();
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
                return ANode.atom().Token();
            }
            set
            {
                if (value == null) throw new ArgumentNullException("value");
                Check(true);
                using (new UpdateContext())
                {
                    Update(new ReplaceValue(DbNode.pre, Database.Data, null, value.Token()));
                }
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
                return ANode.nname().Token();
            }
            set
            {
                if (value == null) throw new ArgumentNullException("value");
                Check(true);
                using (new UpdateContext())
                {
                    Update(new RenameNode(_dbNode.pre, _database.Data, null, new QNm(value.Token())));
                }
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
                return ANode.qname().ln().Token();
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
                return ANode.qname().pref().Token();
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
                return ANode.qname().uri().atom().Token();
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
            //Do a validity check without throwing exceptions (Equals should never throw exceptions)
            if (other == null || !Valid || !other.Valid)
            {
                return false;
            }
            if(DbNode != null)
            {
                return _database.Equals(other._database) && _id == other._id;
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
            if(DbNode != null)
            {
                result = 37 * result + _id.GetHashCode();   
                result = 37 * result + _database.GetHashCode(); 
            }
            else
            {
                result = 37 * ANode.id;
            }
            return result;
        }

        #endregion

    }
}
