using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using org.basex.data;
using org.basex.query.item;

namespace Nxdb
{
    public class Attribute : Node
    {
        //Should only be called from Node.Get()
        internal Attribute(ANode aNode) : base(aNode, Data.ATTR) { }

        public Attribute(string name, string value) : base(new FAttr(new QNm(name.Token()), value.Token()), Data.ATTR) { }

        public override System.Xml.XmlNodeType NodeType
        {
            get { return System.Xml.XmlNodeType.Attribute; }
        }

        #region Content

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
