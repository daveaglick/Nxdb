/*
 * Copyright 2012 WildCard, LLC
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 * 
 */

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
