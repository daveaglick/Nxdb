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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using Nxdb.Dom;
using org.basex.data;
using org.basex.query.item;
using org.basex.query.up.expr;
using org.basex.query.up.primitives;

namespace Nxdb
{
    /// <summary>
    /// Representation of an XML document node.
    /// </summary>
    public class Document : ContainerNode
    {
        //Should only be called from Node.Get()
        internal Document(ANode aNode, Database database) : base(aNode, Data.DOC, database) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="Document"/> class.
        /// Manually constructed nodes are immutable and are not added to the database.
        /// </summary>
        /// <param name="name">The name of the document.</param>
        public Document(string name) : base(new FDoc(name.Token()), Data.DOC, null) { }

        /// <inheritdoc />
        public override System.Xml.XmlNodeType NodeType
        {
            get { return System.Xml.XmlNodeType.Document; }
        }

        /// <summary>
        /// Document does not support inserting content before it. This method will always throw
        /// a NotSupportedException.
        /// </summary>
        /// <param name="xmlReader">The XML reader to get content from.</param>
        public override void InsertBefore(System.Xml.XmlReader xmlReader)
        {
            throw new NotSupportedException("cannot insert before a document node");
        }

        /// <summary>
        /// Document does not support inserting content after it. This method will always throw
        /// a NotSupportedException.
        /// </summary>
        /// <param name="xmlReader">The XML reader to get content from.</param>
        public override void InsertAfter(System.Xml.XmlReader xmlReader)
        {
            throw new NotSupportedException("cannot insert after a document node");
        }

        /// <inheritdoc />
        public override string Name
        {
            get
            {
                return BaseUri;
            }
            set
            {
                //Need to use the update primitive (as opposed to the expression) because documents can't be renamed through the expression
                if (value == null) throw new ArgumentNullException("value");
                Check(true);
                Updates.Add(new ReplaceValue(DbNode.pre, DbNode.data(), null, value.Token()));
            }
        }

        /// <inheritdoc />
        protected override XmlNode CreateXmlNode()
        {
            return new DomDocument(this);
        }
    }
}
