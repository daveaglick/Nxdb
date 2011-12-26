using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using org.basex.query.item;
using org.basex.query.iter;

namespace Nxdb.Dom
{
    public class DomNodeList : XmlNodeList
    {
        private readonly List<Node> _nodeList;

        internal DomNodeList(IEnumerable<Node> nodes)
        {
            _nodeList = new List<Node>(nodes);
        }

        public override XmlNode Item(int index)
        {
            return _nodeList[index].XmlNode;
        }

        public override int Count
        {
            get { return _nodeList.Count; }
        }

        public override IEnumerator GetEnumerator()
        {
            return _nodeList.GetEnumerator();
        }
    }
}
