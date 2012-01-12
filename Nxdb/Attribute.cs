using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using Nxdb.Dom;
using org.basex.data;
using org.basex.query.item;

namespace Nxdb
{
    public class Attribute : Node
    {
        //Should only be called from Node.Get()
        internal Attribute(ANode aNode, Database database) : base(aNode, Data.ATTR, database) { }

        public Attribute(string name, string value) : base(new FAttr(new QNm(name.Token()), value.Token()), Data.ATTR, null) { }

        public override System.Xml.XmlNodeType NodeType
        {
            get { return System.Xml.XmlNodeType.Attribute; }
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
            return new DomAttribute(this);
        }
    }
}
