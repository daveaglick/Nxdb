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
    public class Comment : TreeNode
    {
        //Should only be called from Node.Get()
        internal Comment(ANode aNode, Database database) : base(aNode, Data.COMM, database) { }

        public Comment(string comment) : base(new FComm(comment.Token()), Data.COMM, null) { }

        public override System.Xml.XmlNodeType NodeType
        {
            get { return System.Xml.XmlNodeType.Comment; }
        }

        protected override XmlNode CreateXmlNode()
        {
            return new DomComment(this);
        }
    }
}
