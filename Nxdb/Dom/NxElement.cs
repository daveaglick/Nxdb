using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
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
    public class NxElement : XmlElement, INxXmlNode
    {
        private readonly NxNode node;
        public NxNode NxNode
        {
            get { return node; }
        }

        internal NxElement(NxNode node)
            : base(node.Prefix, node.LocalName, node.NamespaceURI, (XmlDocument)node.OwnerDocument.XmlNode)
        {
            this.node = node;
        }

        public override void WriteTo(XmlWriter w)
        {
            node.WriteTo(w);
        }

        public override void WriteContentTo(XmlWriter xw)
        {
            node.WriteContentTo(xw);
        }

        public override string Name
        {
            get { return node.Name; }
        }

        public override string LocalName
        {
            get { return node.LocalName; }
        }

        public override string Prefix
        {
            get { return node.Prefix; }
        }

        public override XmlNode ParentNode
        {
            get { return NxNode.GetXmlNode(node.ParentNode); }
        }

        public override string InnerXml
        {
            get { return node.InnerXml; }
        }

        public override string InnerText
        {
            get { return node.InnerText; }
        }

        public override XmlNode NextSibling
        {
            get { return NxNode.GetXmlNode(node.NextSibling); }
        }

        public override XmlNode PreviousSibling
        {
            get { return NxNode.GetXmlNode(node.PreviousSibling); }
        }

        public override XmlNodeList ChildNodes
        {
            get { return new NxXmlNodeList(node.ChildNodes); }
        }

        public override XmlNode FirstChild
        {
            get { return NxNode.GetXmlNode(node.FirstChild); }
        }

        public override XmlNode LastChild
        {
            get { return NxNode.GetXmlNode(node.LastChild); }
        }

        public override bool HasChildNodes
        {
            get { return node.HasChildNodes; }
        }

        public override string OuterXml
        {
            get { return node.OuterXml; }
        }

        private bool addingAttributes = false;
        public override bool IsReadOnly
        {
            get { return !addingAttributes; }
        }

        public override XmlAttributeCollection Attributes
        {
            get
            {
                NxDocument ownerDocument = (NxDocument)OwnerDocument;
                ownerDocument.IgnoreAttributeChanges = true;
                addingAttributes = true;
                XmlAttributeCollection attributes = base.Attributes;
                base.Attributes.RemoveAll();
                foreach(NxNode attributeNode in node.Attributes)
                {
                    NxAttribute attribute = (NxAttribute)attributeNode.XmlNode;
                    attribute.EnableOwnerElement = false;
                    attributes.Append(attribute);
                    attribute.EnableOwnerElement = true;
                }
                addingAttributes = false;
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
            get { return node.NamespaceURI; }
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
