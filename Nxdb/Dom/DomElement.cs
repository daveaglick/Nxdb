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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.XPath;
using org.basex.query.item;
using org.basex.query.iter;
using org.basex.util;
using Type=System.Type;

namespace Nxdb.Dom
{
    public class DomElement : XmlElement, IDomNode
    {
        private readonly Element _node;
        public Node Node
        {
            get { return _node; }
        }

        internal DomElement(Element node)
            : base(node.Prefix, node.LocalName, node.NamespaceUri, (XmlDocument)node.Document.XmlNode)
        {
            _node = node;
        }

        public override void WriteTo(XmlWriter w)
        {
            _node.WriteOuterXml(w);
        }

        public override void WriteContentTo(XmlWriter w)
        {
            _node.WriteInnerXml(w);
        }

        public override string Name
        {
            get { return _node.Name; }
        }

        public override string LocalName
        {
            get { return _node.LocalName; }
        }

        public override string Prefix
        {
            get { return _node.Prefix; }
        }

        public override XmlNode ParentNode
        {
            get { return _node.Parent.XmlNode; }
        }

        public override string InnerXml
        {
            get { return _node.InnerXml; }
        }

        public override string InnerText
        {
            get { return _node.InnerText; }
        }

        public override XmlNode PreviousSibling
        {
            get
            {
                TreeNode node = Node.PrecedingSiblings.FirstOrDefault();
                return node != null ? node.XmlNode : null;
            }
        }

        public override XmlNode NextSibling
        {
            get
            {
                TreeNode node = Node.FollowingSiblings.FirstOrDefault();
                return node != null ? node.XmlNode : null;
            }
        }

        public override XmlNodeList ChildNodes
        {
            get { return new DomNodeList(_node.Children.Cast<Node>()); }
        }


        public override XmlNode FirstChild
        {
            get
            {
                Node node = _node.Children.FirstOrDefault();
                return node != null ? node.XmlNode : null;
            }
        }

        public override XmlNode LastChild
        {
            get
            {
                Node node = _node.Children.LastOrDefault();
                return node != null ? node.XmlNode : null;
            }
        }

        public override bool HasChildNodes
        {
            get { return _node.Children.Count() > 0; }
        }

        public override string OuterXml
        {
            get { return _node.OuterXml; }
        }

        private bool _addingAttributes = false;
        public override bool IsReadOnly
        {
            get { return !_addingAttributes; }
        }

        public override XmlAttributeCollection Attributes
        {
            get
            {
                DomDocument ownerDocument = (DomDocument)OwnerDocument;
                ownerDocument.IgnoreAttributeChanges = true;
                _addingAttributes = true;
                XmlAttributeCollection attributes = base.Attributes;
                base.Attributes.RemoveAll();
                foreach (Attribute attributeNode in _node.Attributes)
                {
                    DomAttribute attribute = (DomAttribute)attributeNode.XmlNode;
                    attribute.EnableOwnerElement = false;
                    attributes.Append(attribute);
                    attribute.EnableOwnerElement = true;
                }
                _addingAttributes = false;
                ownerDocument.IgnoreAttributeChanges = false;
                return attributes;
            }
        }

        public override string GetAttribute(string name)
        {
            XmlAttribute attribute = Attributes[name];
            return attribute != null ? attribute.Value : null;
        }

        public override XmlAttribute GetAttributeNode(string name)
        {
            return Attributes[name];
        }

        public override bool HasAttribute(string name)
        {
            return Attributes[name] != null;
        }

        public override bool HasAttributes
        {
            get { return Attributes.Count > 0; }
        }

        public override string NamespaceURI
        {
            get { return _node.NamespaceUri; }
        }

        public override string BaseURI
        {
            get { return String.Empty; }
        }

        //** Not implemented

        public override void SetAttribute(string name, string value)
        {
            throw new NotImplementedException();
        }

        public override void RemoveAttribute(string name)
        {
            throw new NotImplementedException();
        }

        public override XmlAttribute SetAttributeNode(XmlAttribute newAttr)
        {
            throw new NotImplementedException();
        }

        public override XmlAttribute RemoveAttributeNode(XmlAttribute oldAttr)
        {
            throw new NotImplementedException();
        }

        public override XmlNodeList GetElementsByTagName(string name)
        {
            throw new NotImplementedException();
        }

        public override string GetAttribute(string localName, string namespaceURI)
        {
            throw new NotImplementedException();
        }

        public override string SetAttribute(string localName, string namespaceURI, string value)
        {
            throw new NotImplementedException();
        }

        public override void RemoveAttribute(string localName, string namespaceURI)
        {
            throw new NotImplementedException();
        }

        public override XmlAttribute GetAttributeNode(string localName, string namespaceURI)
        {
            throw new NotImplementedException();
        }

        public override XmlAttribute SetAttributeNode(string localName, string namespaceURI)
        {
            throw new NotImplementedException();
        }

        public override XmlAttribute RemoveAttributeNode(string localName, string namespaceURI)
        {
            throw new NotImplementedException();
        }

        public override XmlNodeList GetElementsByTagName(string localName, string namespaceURI)
        {
            throw new NotImplementedException();
        }

        public override bool HasAttribute(string localName, string namespaceURI)
        {
            throw new NotImplementedException();
        }

        public override XmlNode RemoveAttributeAt(int i)
        {
            throw new NotImplementedException();
        }

        public override IXmlSchemaInfo SchemaInfo
        {
            get { throw new NotImplementedException(); }
        }

        public override XPathNavigator CreateNavigator()
        {
            throw new NotImplementedException();
        }

        public override XmlNode ReplaceChild(XmlNode newChild, XmlNode oldChild)
        {
            throw new NotImplementedException();
        }

        public override void Normalize()
        {
            throw new NotImplementedException();
        }

        public override bool Supports(string feature, string version)
        {
            throw new NotImplementedException();
        }

        public override string GetNamespaceOfPrefix(string prefix)
        {
            throw new NotImplementedException();
        }

        public override string GetPrefixOfNamespace(string namespaceURI)
        {
            throw new NotImplementedException();
        }

        public override XmlElement this[string name]
        {
            get { throw new NotImplementedException(); }
        }

        public override XmlElement this[string localname, string ns]
        {
            get { throw new NotImplementedException(); }
        }
    }
}
