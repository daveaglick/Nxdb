using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using org.basex.query.item;
using org.basex.query.iter;
using org.basex.query.up.primitives;

namespace Nxdb
{
    /// <summary>
    /// Base class for nodes that are part of a tree (all except attributes).
    /// </summary>
    public abstract class TreeNode : Node
    {
        protected TreeNode(ANode aNode, Database database, int kind) : base(aNode, database, kind) { }
    }

    /// <summary>
    /// Base class for nodes that can contain other nodes (document and element).
    /// </summary>
    public abstract class ContainerNode : TreeNode
    {
        protected ContainerNode(ANode aNode, Database database, int kind) : base(aNode, database, kind) { }
    }

    // TODO: Implement IEquatable<Node>
    public abstract class Node //: IEquatable<Node>
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
            if (ANode.kind(aNode.ndType()) != kind) throw new ArgumentException("incorrect node type");
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

        internal static Node GetNode(ANode aNode, Database database = null)
        {
            if (aNode == null) throw new ArgumentNullException("aNode");

            // Is this a database node?
            DBNode dbNode = aNode as DBNode;
            if (dbNode != null)
            {
                return GetNode(dbNode, database);
            }

            // If not, create the appropriate non-database node class
            NodeType nodeType = aNode.ndType();
            if (nodeType == NodeType.ELM)
            {
                return new Element(aNode, null);
            }
            if (nodeType == NodeType.TXT)
            {
                return new Text(aNode, null);
            }
            if (nodeType == NodeType.ATT)
            {
                return new Attribute(aNode, null);
            }
            if (nodeType == NodeType.DOC)
            {
                return new Document(aNode, null);
            }
            if (nodeType == NodeType.COM)
            {
                return new Comment(aNode, null);
            }
            if (nodeType == NodeType.PI)
            {
                return new ProcessingInstruction(aNode, null);
            }
            throw new ArgumentException("invalid node type");
        }

        internal static Node GetNode(int pre, Database database)
        {
            if (database == null) throw new ArgumentNullException("database");
            if (pre < 0) throw new ArgumentOutOfRangeException("pre");
            return GetNode(new DBNode(database.Data, pre), database, false);
        }

        internal static Node GetNode(DBNode dbNode, Database database, bool copy = true)
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
            if (nodeType == NodeType.ELM)
            {
                return new Element(dbNode, database);
            }
            if (nodeType == NodeType.TXT)
            {
                return new Text(dbNode, database);
            }
            if (nodeType == NodeType.ATT)
            {
                return new Attribute(dbNode, database);
            }
            if (nodeType == NodeType.DOC)
            {
                return new Document(dbNode, database);
            }
            if (nodeType == NodeType.COM)
            {
                return new Comment(dbNode, database);
            }
            if (nodeType == NodeType.PI)
            {
                return new ProcessingInstruction(dbNode, database);
            }
            throw new ArgumentException("invalid node type");
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

        protected ANode ANode
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
            get { return _id; }
        }

        /// <summary>
        /// Gets the database index for the node or -1 if this is not a database node or if the
        /// node has become invalid. The database index potentially changes with every database
        /// update, so this value may be different between calls.
        /// </summary>
        public int Index
        {
            get { return Valid && DbNode != null ? DbNode.pre : -1; }
        }

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
                            _time = long.MinValue;
                            return false;
                        }
                        DbNode.set(pre, _kind);    // Assume that the kind is the same since we found the same ID
                    }
                }

                return true;
            }
        }

        // Checks that this is a database node, and if not throws an exception
        protected void RequireDatabase()
        {
            if(DbNode == null) throw new NotSupportedException("this operation is only supported for database nodes");
        }

        #endregion

        #region Enumeration

        // Provides typed enumeration for BaseX NodeIter, which are limited to ANode results
        // and thus the enumeration results are guaranteed to produce NxNode objects
        protected IEnumerable<T> EnumerateNodes<T>(NodeIter iter)
        {
            return Valid ? new IterEnum(iter, Database).Cast<T>() : Enumerable.Empty<T>();
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
        /// Gets the children of this node. An empty sequence will be returned if the node is
        /// invalid or if the node does not support children (such as attribute nodes).
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
        /// <returns>The child node at the specified index or null if the node is invalid, no
        /// child node could be found, or if the node does not support children (such as
        /// attribute nodes).</returns>
        public TreeNode Child(int index)
        {
            return Children.ElementAtOrDefault(index);
        }

        /// <summary>
        /// Gets the attributes. Per the XML standard, the ordering of attributes is
        /// undefined and should not be considered relevant or consistent. An empty
        /// sequence will be returned if the node is invalid or is not an Element.
        /// </summary>
        public IEnumerable<Attribute> Attributes
        {
            get { return EnumerateNodes<Attribute>(ANode.attributes()); }
        }

        /// <summary>
        /// Gets the following sibling nodes. An empty sequence will be returned if the
        /// node is invalid..
        /// </summary>
        public IEnumerable<TreeNode> FollowingSiblings
        {
            get { return EnumerateNodes<TreeNode>(ANode.follSibl()); }
        }

        /// <summary>
        /// Gets the preceding sibling nodes. An empty sequence will be returned if the
        /// node is invalid.
        /// </summary>
        public IEnumerable<TreeNode> PrecedingSiblings
        {
            get { return EnumerateNodes<TreeNode>(ANode.precSibl()); }
        }
        
        /// <summary>
        /// Gets the following nodes. An empty sequence will be returned if the
        /// node is invalid.
        /// </summary>
        public IEnumerable<TreeNode> Following
        {
            get { return EnumerateNodes<TreeNode>(ANode.foll()); }
        }

        /// <summary>
        /// Gets the preceding nodes. An empty sequence will be returned if the
        /// node is invalid.
        /// </summary>
        public IEnumerable<TreeNode> Preceding
        {
            get { return EnumerateNodes<TreeNode>(ANode.prec()); }
        }

        /// <summary>
        /// Gets the parent node. The return value will be null if the node is
        /// invalid or does not have a parent node (such as a stand-alone result node).
        /// Unlike XML DOM implementations, the parent of an Attribute is the container
        /// Element. Also, the parent of a document root Element is the Document.
        /// </summary>
        public ContainerNode Parent
        {
            get
            {
                if (Valid)
                {
                    ANode parent = ANode.parent();
                    if (parent != null)
                    {
                        return (ContainerNode)GetNode(parent, Database);
                    }
                }
                return null;
            }
        }

        /// <summary>
        /// Gets the ancestor nodes. An empty sequence will be returned if the
        /// node is invalid.
        /// </summary>
        public IEnumerable<ContainerNode> Ancestors
        {
            get { return EnumerateNodes<ContainerNode>(ANode.anc()); }
        }

        /// <summary>
        /// Gets the ancestor nodes and current node. An empty sequence will
        /// be returned if the node is invalid.
        /// </summary>
        public IEnumerable<TreeNode> AncestorsOrSelf
        {
            get { return EnumerateNodes<TreeNode>(ANode.ancOrSelf()); }
        }

        /// <summary>
        /// Gets the descendant nodes. An empty sequence will be returned if the
        /// node is invalid or is not a ContainerNode.
        /// </summary>
        public IEnumerable<TreeNode> Descendants
        {
            get { return EnumerateNodes<TreeNode>(ANode.descendant()); }
        }

        /// <summary>
        /// Gets the descendant nodes and current node. An empty sequence will
        /// be returned if the node is invalid.
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

        #endregion

        // TODO: Add a property for BaseUri
    }
}
