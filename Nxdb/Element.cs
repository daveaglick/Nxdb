using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using org.basex.data;
using org.basex.query.item;
using org.basex.query.up.primitives;

namespace Nxdb
{
    public class Element : ContainerNode
    {
        internal Element(ANode aNode, Database database) : base(aNode, database, Data.ELEM) { }

        public override System.Xml.XmlNodeType NodeType
        {
            get { return System.Xml.XmlNodeType.Element; }
        }

        #region Attributes
        
        // Gets a specific attribute ANode for a given attribute name
        private ANode AttributeANode(string name)
        {
            QNm qnm = new QNm(name.Token());
            return EnumerateANodes(ANode.attributes()).FirstOrDefault(n => n.qname().eq(qnm));
        }

        /// <summary>
        /// Gets the attribute with the specified name.
        /// </summary>
        /// <param name="name">The name of the attribute to get.</param>
        /// <returns>An attribute with the specified name or null if the node is invalid
        /// or an attribute with the specified name is not found.</returns>
        public Attribute Attribute(string name)
        {
            if (name == null) throw new ArgumentNullException("name");
            if (name == String.Empty) throw new ArgumentException("name");
            Check();
            ANode node = AttributeANode(name);
            return node == null ? null : new Attribute(node, Database);
        }

        /// <summary>
        /// Gets the value of the attribute with the specified name.
        /// </summary>
        /// <param name="name">The name of the attribute to get a value for.</param>
        /// <returns>
        /// The value of the attribute or an empty string if the node is invalid
        /// or an attribute with the specified name is not found.
        /// </returns>
        public string AttributeValue(string name)
        {
            if (name == null) throw new ArgumentNullException("name");
            if (name == String.Empty) throw new ArgumentException("name");
            Check();
            ANode node = AttributeANode(name);
            return node == null ? String.Empty : node.atom().Token();
        }

        /// <summary>
        /// Removes all the attributes from this element. This can only be used on database nodes.
        /// If the node is invalid this does nothing.
        /// </summary>
        /// <exception cref="NotSupportedException">The node is not a database node.</exception>
        public void RemoveAllAttributes()
        {
            Check(true);
            using (new UpdateContext())
            {
                foreach (DBNode node in EnumerateANodes(ANode.attributes()).Cast<DBNode>())
                {
                    Update(new DeleteNode(node.pre, Database.Data, null));
                }
            }
        }

        /// <summary>
        /// Removes an attribute with the specified name. This can only be used on database nodes.
        /// If the node is invalid this does nothing.
        /// </summary>
        /// <param name="name">The name of the attribute to remove.</param>
        /// <exception cref="NotSupportedException">The node is not a database node.</exception>
        public void RemoveAttribute(string name)
        {
            if (name == null) throw new ArgumentNullException("name");
            if (name == String.Empty) throw new ArgumentException("name");
            Check(true);
            DBNode node = AttributeANode(name) as DBNode;
            if (node != null)
            {
                using (new UpdateContext())
                {
                    Update(new DeleteNode(node.pre, Database.Data, null));
                }
            }
        }

        /// <summary>
        /// Inserts a new attribute with the specified name and value.
        /// </summary>
        /// <param name="name">The name of the new attribute.</param>
        /// <param name="value">The value of the new attribute.</param>
        public void InsertAttribute(string name, string value)
        {
            if (name == null) throw new ArgumentNullException("name");
            if (value == null) throw new ArgumentNullException("value");
            if (name == String.Empty) throw new ArgumentException("name");
            Check();
            FAttr attr = new FAttr(new QNm(name.Token()), value.Token());
            if(DbNode != null)
            {
                using (new UpdateContext())
                {
                    Update(new InsertAttribute(DbNode.pre, Database.Data, null, Helper.GetNodeCache(attr)));
                }
            }
            else if(FNode != null)
            {
                FNode.add(attr);
            }
        }

        #endregion

        #region Content

        public override void RemoveAll()
        {
            using(new UpdateContext())
            {
                RemoveAllAttributes();
                base.RemoveAll();
            }
        }

        public override string Name
        {
            get { return NameImpl; }
            set { NameImpl = value; }
        }

        public override string LocalName
        {
            get { return LocalNameImpl; }
        }

        public override string Prefix
        {
            get { return PrefixImpl; }
        }

        public override string NamespaceUri
        {
            get { return NamespaceUriImpl; }
        }

        #endregion
    }
}
