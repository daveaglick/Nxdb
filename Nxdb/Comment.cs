using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using org.basex.data;
using org.basex.query.item;

namespace Nxdb
{
    public class Comment : TreeNode
    {
        //Should only be called from Node.Get()
        internal Comment(ANode aNode) : base(aNode, Data.COMM) { }

        public Comment(string comment) : base(new FComm(comment.Token()), Data.COMM) { }

        public override System.Xml.XmlNodeType NodeType
        {
            get { return System.Xml.XmlNodeType.Comment; }
        }
    }
}
