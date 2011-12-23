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
        //Should only be called from Node.Get()
        internal Text(ANode aNode) : base(aNode, Data.TEXT) { }

        public Text(string text) : base(new FTxt(text.Token()), Data.TEXT) { }

        public override System.Xml.XmlNodeType NodeType
        {
            get { return System.Xml.XmlNodeType.Text; }
        }
    }
}
