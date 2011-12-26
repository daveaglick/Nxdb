using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.XPath;
using org.basex.query.item;
using org.basex.util;

namespace Nxdb.Dom
{
    public class DomAttribute : XmlAttribute, IDomNode
    {
        private readonly Attribute _node;
        public Node Node
        {
            get { return _node; }
        }

        //Need to return null for OwnerElement when adding attributes to an XmlAttributeCollection
        private bool _enableOwnerElement = true;
        internal bool EnableOwnerElement
        { set { _enableOwnerElement = value; } }

        internal DomAttribute(Attribute node)
            : base(node.Prefix, node.LocalName, node.NamespaceUri, (XmlDocument)node.Document.XmlNode)
        {
            _node = node;
        }

        protected internal DomAttribute(string prefix, string localName, string namespaceUri, XmlDocument document)
            : base(prefix, localName, namespaceUri, document) { }

        public override void WriteTo(XmlWriter w)
        {
            w.WriteAttributeString(_node.Prefix, _node.LocalName, _node.NamespaceUri, _node.Value);
        }

        public override void WriteContentTo(XmlWriter w)
        {
            WriteTo(w);
        }

        public override string Value
        {
            get { return _node.Value; }
        }

        public override bool IsReadOnly
        {
            get { return true; }
        }

        public override string InnerText
        {
            get { return _node.Value; }
        }

        public override XmlElement OwnerElement
        {
            get
            {
                if (_enableOwnerElement)
                {
                    return (XmlElement)ParentNode;
                }
                return null;
            }
        }

        public override string OuterXml
        {
            get { return String.Empty; }
        }

        public override string InnerXml
        {
            get { return String.Empty; }
        }

        public override XmlNode ParentNode
        {
            get { return Node.Parent.XmlNode; }
        }

        public override XmlNode PreviousSibling
        {
            get { return null; }
        }

        public override XmlNode NextSibling
        {
            get { return null; }
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

        public override string NamespaceURI
        {
            get { return _node.NamespaceUri; }
        }

        public override string BaseURI
        {
            get { return String.Empty; }
        }

        //** Not implemented

        public override void Normalize()
        {
            throw new NotImplementedException();
        }

        public override bool Supports(string feature, string version)
        {
            throw new NotImplementedException();
        }

        public override XPathNavigator CreateNavigator()
        {
            throw new NotImplementedException();
        }

        public override XmlNode InsertBefore(XmlNode newChild, XmlNode refChild)
        {
            throw new NotImplementedException();
        }

        public override XmlNode InsertAfter(XmlNode newChild, XmlNode refChild)
        {
            throw new NotImplementedException();
        }

        public override XmlNode ReplaceChild(XmlNode newChild, XmlNode oldChild)
        {
            throw new NotImplementedException();
        }

        public override XmlNode PrependChild(XmlNode newChild)
        {
            throw new NotImplementedException();
        }

        public override XmlNode AppendChild(XmlNode newChild)
        {
            throw new NotImplementedException();
        }

        public override void RemoveAll()
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

        public override IXmlSchemaInfo SchemaInfo
        {
            get { throw new NotImplementedException(); }
        }

        public override XmlElement this[string name]
        {
            get { throw new NotImplementedException(); }
        }

        public override XmlElement this[string localname, string ns]
        {
            get { throw new NotImplementedException(); }
        }

        public override bool Specified
        {
            get { return true; }
        }
    }
}
