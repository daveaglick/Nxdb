using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.XPath;
using org.basex.data;
using org.basex.query.item;
using org.basex.query.iter;
using Type=org.basex.query.item.Type;

namespace Nxdb.Dom
{
    //Keep in mind different instances could refer to the same document, should compare with equals
    //public class NxDocument : XmlDocument, INxXmlNode
    //{
    //    private readonly NxNode node;
    //    public NxNode NxNode
    //    {
    //        get { return node; }
    //    }

    //    internal NxDocument(NxNode node)
    //    {
    //        this.node = node;
    //        NodeInserting += PreventChanges;
    //        NodeChanging += PreventChanges;
    //        NodeRemoving += PreventChanges;
    //    }

    //    private bool ignoreAttributeChanges = false;
    //    internal bool IgnoreAttributeChanges
    //    {
    //        set { ignoreAttributeChanges = value; }
    //    }

    //    //Must handle attributes here because it's the only way to get them to work
    //    void PreventChanges(object sender, XmlNodeChangedEventArgs e)
    //    {
    //        if (!ignoreAttributeChanges || !(e.Node is NxAttribute))
    //        {
    //            throw new NotSupportedException();
    //        }
    //    }

    //    public override void WriteTo(XmlWriter w)
    //    {
    //        node.WriteTo(w);
    //    }

    //    public override void WriteContentTo(XmlWriter xw)
    //    {
    //        node.WriteContentTo(xw);
    //    }

    //    public string DocumentName
    //    {
    //        get
    //        {
    //            return node.Name;
    //        }
    //        set
    //        {
    //            node.Name = value;
    //        }
    //    }

    //    public override string InnerText
    //    {
    //        get { return node.InnerText; }
    //    }

    //    public override string OuterXml
    //    {
    //        get { return node.OuterXml; }
    //    }

    //    public override XmlNodeList ChildNodes
    //    {
    //        get { return new NxXmlNodeList(node.ChildNodes); }
    //    }

    //    public override bool HasChildNodes
    //    {
    //        get { return node.HasChildNodes; }
    //    }

    //    public override XmlNode PreviousSibling
    //    {
    //        get { return null; }
    //    }

    //    public override XmlNode NextSibling
    //    {
    //        get { return null; }
    //    }

    //    public override XmlNode FirstChild
    //    {
    //        get { return NxNode.GetXmlNode(node.FirstChild); }
    //    }

    //    public override XmlNode LastChild
    //    {
    //        get { return NxNode.GetXmlNode(node.LastChild); }
    //    }

    //    public override string Value
    //    {
    //        get { return null; }
    //    }

    //    public override string NamespaceURI
    //    {
    //        get { return node.NamespaceURI; }
    //    }

    //    public override bool IsReadOnly
    //    {
    //        get { return !ignoreAttributeChanges; }
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

    //    public override XmlNodeList GetElementsByTagName(string name)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public override XmlNodeList GetElementsByTagName(string localName, string namespaceURI)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public override XmlElement GetElementById(string elementId)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public override XmlCDataSection CreateCDataSection(string data)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public override XmlDocumentType CreateDocumentType(string name, string publicId, string systemId, string internalSubset)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public override XmlDocumentFragment CreateDocumentFragment()
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public override XmlEntityReference CreateEntityReference(string name)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public override XmlDeclaration CreateXmlDeclaration(string version, string encoding, string standalone)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public override XmlSignificantWhitespace CreateSignificantWhitespace(string text)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public override XPathNavigator CreateNavigator()
    //    {
    //        throw new NotImplementedException();
    //    }

    //    protected override XPathNavigator CreateNavigator(XmlNode node)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public override XmlWhitespace CreateWhitespace(string text)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public override XmlNode ImportNode(XmlNode node, bool deep)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    protected override XmlAttribute CreateDefaultAttribute(string prefix, string localName, string namespaceURI)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public override XmlNode CreateNode(XmlNodeType type, string prefix, string name, string namespaceURI)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public override XmlNode CreateNode(string nodeTypeString, string name, string namespaceURI)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public override XmlNode CreateNode(XmlNodeType type, string name, string namespaceURI)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public override XmlNode ReadNode(XmlReader reader)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public override void Load(string filename)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public override void Load(Stream inStream)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public override void Load(TextReader txtReader)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public override void Load(XmlReader reader)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public override void LoadXml(string xml)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public override void Save(string filename)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public override void Save(Stream outStream)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public override void Save(TextWriter writer)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public override void Save(XmlWriter w)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public override XmlDocumentType DocumentType
    //    {
    //        get { return null; }
    //    }

    //    public override XmlResolver XmlResolver
    //    {
    //        set { throw new NotImplementedException(); }
    //    }

    //    public override IXmlSchemaInfo SchemaInfo
    //    {
    //        get { throw new NotImplementedException(); }
    //    }

    //    public override XmlNode ReplaceChild(XmlNode newChild, XmlNode oldChild)
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

    //    public override string GetNamespaceOfPrefix(string prefix)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public override string GetPrefixOfNamespace(string namespaceURI)
    //    {
    //        throw new NotImplementedException();
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
