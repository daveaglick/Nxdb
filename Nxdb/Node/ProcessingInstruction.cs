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

using System.Xml;
using Nxdb.Dom;
using org.basex.data;
using org.basex.query.item;

namespace Nxdb.Node
{
    /// <summary>
    /// Representation of an XML processing instruction node.
    /// </summary>
    public class ProcessingInstruction : TreeNode
    {
        //Should only be called from Node.Get()
        internal ProcessingInstruction(ANode aNode, Database database) : base(aNode, Data.PI, database) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProcessingInstruction"/> class.
        /// Manually constructed nodes are immutable and are not added to the database.
        /// </summary>
        /// <param name="name">The name of the processing instruction.</param>
        /// <param name="value">The value of the processing instruction.</param>
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
