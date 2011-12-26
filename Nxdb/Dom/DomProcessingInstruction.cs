using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.XPath;

namespace Nxdb.Dom
{
    public class DomProcessingInstruction : XmlProcessingInstruction, IDomNode
    {
        private readonly ProcessingInstruction _node;
        public Node Node
        {
            get { return _node; }
        }

        internal DomProcessingInstruction(ProcessingInstruction node)
            : base(node.Name, node.Value, (XmlDocument)node.Document.XmlNode)
        {
            _node = node;
        }

        public override void WriteTo(XmlWriter w)
        {
            w.WriteProcessingInstruction(_node.Name, _node.Value);
        }

        public override XmlNode ParentNode
        {
            get { return _node.Parent.XmlNode; }
        }

        public override string Value
        {
            get { return _node.Value; }
        }

        public override string InnerText
        {
            get { return _node.Value; }
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

        public override bool IsReadOnly
        {
            get { return true; }
        }

        public override string OuterXml
        {
            get { return _node.Value; }
        }

        public override string InnerXml
        {
            get { return _node.Value; }
        }

        public override string BaseURI
        {
            get { return String.Empty; }
        }

        //** Not implemented

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

        public override void Normalize()
        {
            throw new NotImplementedException();
        }

        public override bool Supports(string feature, string version)
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

        public override string NamespaceURI
        {
            get { throw new NotImplementedException(); }
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
    }
}
