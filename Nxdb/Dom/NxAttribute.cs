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
    //public class NxAttribute : XmlAttribute, INxXmlNode
    //{
    //    private readonly NxNode node;
    //    public NxNode NxNode
    //    {
    //        get { return node; }
    //    }

    //    //Need to return null for OwnerElement when adding attributes to an XmlAttributeCollection
    //    private bool enableOwnerElement = true;
    //    internal bool EnableOwnerElement
    //    { set { enableOwnerElement = value; } }

    //    internal NxAttribute(NxNode node)
    //        : base(node.Prefix, node.LocalName, node.NamespaceURI, (XmlDocument)node.OwnerDocument.XmlNode)
    //    {
    //        this.node = node;
    //    }

    //    protected internal NxAttribute(string prefix, string localName, string namespaceURI, XmlDocument document)
    //        : base(prefix, localName, namespaceURI, document) {}

    //    public override void WriteTo(XmlWriter w)
    //    {
    //        node.WriteTo(w);
    //    }

    //    public override void WriteContentTo(XmlWriter w)
    //    {
    //        node.WriteContentTo(w);
    //    }

    //    public override string Value
    //    {
    //        get { return node.Value; }
    //    }

    //    public override bool IsReadOnly
    //    {
    //        get { return true; }
    //    }

    //    public override string InnerText
    //    {
    //        get { return node.InnerText; }
    //    }

    //    public override XmlElement OwnerElement
    //    {
    //        get
    //        {
    //            if (enableOwnerElement)
    //            {
    //                return (XmlElement) ParentNode;
    //            }
    //            return null;
    //        }
    //    }

    //    public override string OuterXml
    //    {
    //        get { return node.OuterXml; }
    //    }

    //    public override string InnerXml
    //    {
    //        get { return node.InnerXml; }
    //    }

    //    public override XmlNode ParentNode
    //    {
    //        get { return NxNode.GetXmlNode(node.ParentNode); }
    //    }

    //    public override XmlNode PreviousSibling
    //    {
    //        get { return NxNode.GetXmlNode(node.PreviousSibling); }
    //    }

    //    public override XmlNode NextSibling
    //    {
    //        get { return NxNode.GetXmlNode(node.NextSibling); }
    //    }

    //    public override string Name
    //    {
    //        get { return node.Name; }
    //    }

    //    public override string LocalName
    //    {
    //        get { return node.LocalName; }
    //    }

    //    public override string Prefix
    //    {
    //        get { return node.Prefix; }
    //    }

    //    public override string NamespaceURI
    //    {
    //        get { return node.NamespaceURI; }
    //    }

    //    public override string BaseURI
    //    {
    //        get { return String.Empty; }
    //    }

    //    //** Not implemented

    //    public override void Normalize()
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public override bool Supports(string feature, string version)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public override XPathNavigator CreateNavigator()
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public override XmlNode InsertBefore(XmlNode newChild, XmlNode refChild)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public override XmlNode InsertAfter(XmlNode newChild, XmlNode refChild)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public override XmlNode ReplaceChild(XmlNode newChild, XmlNode oldChild)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public override XmlNode PrependChild(XmlNode newChild)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public override XmlNode AppendChild(XmlNode newChild)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public override void RemoveAll()
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public override string GetNamespaceOfPrefix(string prefix)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public override string GetPrefixOfNamespace(string namespaceURI)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public override IXmlSchemaInfo SchemaInfo
    //    {
    //        get { throw new NotImplementedException(); }
    //    }

    //    public override XmlElement this[string name]
    //    {
    //        get { throw new NotImplementedException(); }
    //    }

    //    public override XmlElement this[string localname, string ns]
    //    {
    //        get { throw new NotImplementedException(); }
    //    }

    //    public override bool Specified
    //    {
    //        get { return true; }
    //    }
    //}
}
