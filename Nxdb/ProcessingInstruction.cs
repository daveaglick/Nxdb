using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using org.basex.data;
using org.basex.query.item;

namespace Nxdb
{
    public class ProcessingInstruction : TreeNode
    {
        //Should only be called from Node.Get()
        internal ProcessingInstruction(ANode aNode) : base(aNode, Data.PI) { }

        public ProcessingInstruction(string name, string value) : base(new FPI(new QNm(name.Token()), value.Token()), Data.PI) { }

        public override System.Xml.XmlNodeType NodeType
        {
            get { return System.Xml.XmlNodeType.ProcessingInstruction; }
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
