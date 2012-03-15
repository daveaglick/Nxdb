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
    /// Representation of an XML attribute node.
    /// </summary>
    public class Attribute : Node
    {
        //Should only be called from Node.Get()
        internal Attribute(ANode aNode, Database database) : base(aNode, Data.ATTR, database) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="Attribute"/> class.
        /// Manually constructed nodes are immutable and are not added to the database.
        /// </summary>
        /// <param name="name">The name of the attribute.</param>
        /// <param name="value">The value of the attribute.</param>
        public Attribute(string name, string value) : base(new FAttr(new QNm(name.Token()), value.Token()), Data.ATTR, null) { }
        
        public override System.Xml.XmlNodeType NodeType
        {
            get { return System.Xml.XmlNodeType.Attribute; }
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
            return new DomAttribute(this);
        }
    }
}
