using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
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

        #region Content

        /// <summary>
        /// Inserts the specified content before this node.
        /// </summary>
        /// <param name="xmlReader">The XML reader to get content from.</param>
        public virtual void InsertBefore(XmlReader xmlReader)
        {
            if (xmlReader == null) throw new ArgumentNullException("xmlReader");
            Check(true);
            NodeCache nodeCache = Helper.GetNodeCache(xmlReader);
            if (nodeCache != null)
            {
                using (new UpdateContext())
                {
                    Update(new InsertBefore(DbNode.pre, Database.Data, null, nodeCache));
                }
            }
        }

        /// <summary>
        /// Inserts the specified content after this node.
        /// </summary>
        /// <param name="xmlReader">The XML reader to get content from.</param>
        public virtual void InsertAfter(XmlReader xmlReader)
        {
            if (xmlReader == null) throw new ArgumentNullException("xmlReader");
            Check(true);
            NodeCache nodeCache = Helper.GetNodeCache(xmlReader);
            if (nodeCache != null)
            {
                using (new UpdateContext())
                {
                    Update(new InsertAfter(DbNode.pre, Database.Data, null, nodeCache));
                }
            }
        }

        #endregion
    }
}
