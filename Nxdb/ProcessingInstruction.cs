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
    public class ProcessingInstruction : TreeNode
    {
        //Should only be called from Node.Get()
        internal ProcessingInstruction(ANode aNode, Database database) : base(aNode, Data.PI, database) { }

        public ProcessingInstruction(string name, string value) : base(new FPI(new QNm(name.Token()), value.Token()), Data.PI, null) { }

        public override System.Xml.XmlNodeType NodeType
        {
            get { return System.Xml.XmlNodeType.ProcessingInstruction; }
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
            return new DomProcessingInstruction(this);
        }
    }
}
