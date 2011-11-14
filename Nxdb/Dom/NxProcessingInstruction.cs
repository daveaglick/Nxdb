using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.XPath;

namespace Nxdb.Dom
{
    //public class NxProcessingInstruction : XmlProcessingInstruction, INxXmlNode
    //{
    //    private readonly NxNode node;
    //    public NxNode NxNode
    //    {
    //        get { return node; }
    //    }

    //    internal NxProcessingInstruction(NxNode node)
    //        : base(node.Name, node.Value, (XmlDocument)node.OwnerDocument.XmlNode)
    //    {
    //        this.node = node;
    //    }

    //    public override void WriteTo(XmlWriter w)
    //    {
    //        node.WriteTo(w);
    //    }

    //    public override XmlNode ParentNode
    //    {
    //        get { return NxNode.GetXmlNode(node.ParentNode); }
    //    }

    //    public override string Value
    //    {
    //        get { return node.Value; }
    //    }

    //    public override string InnerText
    //    {
    //        get { return node.InnerText; }
    //    }

    //    public override XmlNode PreviousSibling
    //    {
    //        get { return NxNode.GetXmlNode(node.PreviousSibling); }
    //    }

    //    public override XmlNode NextSibling
    //    {
    //        get { return NxNode.GetXmlNode(node.NextSibling); }
    //    }

    //    public override bool IsReadOnly
    //    {
    //        get { return true; }
    //    }

    //    public override string OuterXml
    //    {
    //        get { return node.OuterXml; }
    //    }

    //    public override string InnerXml
    //    {
    //        get { return node.InnerXml; }
    //    }

    //    public override string BaseURI
    //    {
    //        get { return String.Empty; }
    //    }

    //    //** Not implemented

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

    //    public override void Normalize()
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public override bool Supports(string feature, string version)
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

    //    public override string NamespaceURI
    //    {
    //        get { throw new NotImplementedException(); }
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
    //}
}
