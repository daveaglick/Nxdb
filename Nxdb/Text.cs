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
    public class Text : TreeNode
    {
        //Should only be called from Node.Get()
        internal Text(ANode aNode, Database database) : base(aNode, Data.TEXT, database) { }

        public Text(string text) : base(new FTxt(text.Token()), Data.TEXT, null) { }

        public override System.Xml.XmlNodeType NodeType
        {
            get { return System.Xml.XmlNodeType.Text; }
        }

        protected override XmlNode CreateXmlNode()
        {
            return new DomText(this);
        }
    }
}
