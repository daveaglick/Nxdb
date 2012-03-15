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
using System.Linq;
using System.Xml;
using Nxdb.Dom;
using org.basex.data;
using org.basex.query.item;
using org.basex.query.up.expr;

namespace Nxdb.Node
{
    /// <summary>
    /// Representation of an XML element node.
    /// </summary>
    public class Element : ContainerNode
    {
        //Should only be called from Node.Get()
        internal Element(ANode aNode, Database database) : base(aNode, Data.ELEM, database) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="Element"/> class.
        /// Manually constructed nodes are immutable and are not added to the database.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        public Element(string name) : base(new FElem(new QNm(name.Token())), Data.ELEM, null) { }

        public override System.Xml.XmlNodeType NodeType
        {
            get { return System.Xml.XmlNodeType.Element; }
        }
        
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
            return node == null ? null : (Attribute) Get(node);
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
            return node == null ? String.Empty : node.@string().Token();
        }

        /// <summary>
        /// Removes all the attributes from this element. This can only be used on database nodes.
        /// If the node is invalid this does nothing.
        /// </summary>
        /// <exception cref="NotSupportedException">The node is not a database node.</exception>
        public void RemoveAllAttributes()
        {
            Check(true);
            ANode[] nodes = EnumerateANodes(ANode.attributes()).ToArray();
            Updates.Do(new Delete(null, Seq.get(nodes, nodes.Length)));
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
                Updates.Do(new Delete(null, node));
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
            if (DbNode != null)
            {
                Updates.Do(new Insert(null, attr, false, false, false, false, DbNode));
            }
            else if (FNode != null)
            {
                ((FElem) FNode).add(attr);
            }
        }

        public override void RemoveAll()
        {
            using (new Updates())
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

        protected override XmlNode CreateXmlNode()
        {
            return new DomElement(this);
        }
    }
}
