using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using org.basex.query.item;
using org.basex.query.iter;

namespace Nxdb.Dom
{
    public class NxXmlNodeList : XmlNodeList
    {
        private readonly List<NxNode> nodeList;

        internal NxXmlNodeList(IEnumerable<NxNode> nodes)
        {
            nodeList = new List<NxNode>(nodes);
        }

        public override XmlNode Item(int index)
        {
            return nodeList[index].XmlNode;
        }

        public override int Count
        {
            get { return nodeList.Count; }
        }

        public override IEnumerator GetEnumerator()
        {
            return nodeList.GetEnumerator();
        }
    }
}
