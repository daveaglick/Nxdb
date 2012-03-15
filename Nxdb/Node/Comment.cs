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
    /// Representation of an XML comment node.
    /// </summary>
    public class Comment : TreeNode
    {
        //Should only be called from Node.Get()
        internal Comment(ANode aNode, Database database) : base(aNode, Data.COMM, database) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="Comment"/> class.
        /// Manually constructed nodes are immutable and are not added to the database.
        /// </summary>
        /// <param name="comment">The comment content.</param>
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
