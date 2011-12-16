using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using org.basex.data;
using org.basex.query.item;

namespace Nxdb
{
    public class Text : TreeNode
    {
        internal Text(ANode aNode, Database database) : base(aNode, database, Data.TEXT) { }

        public Text(string text) : base(new FTxt(text.Token()), null, Data.TEXT) { }

        public override System.Xml.XmlNodeType NodeType
        {
            get { return System.Xml.XmlNodeType.Text; }
        }
    }
}
