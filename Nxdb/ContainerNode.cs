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
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using NiceThreads;
using Nxdb.Io;
using org.basex.query.item;
using org.basex.query.iter;
using org.basex.query.up.expr;
using org.basex.query.up.primitives;

namespace Nxdb
{
    /// <summary>
    /// Base class for nodes that can contain other nodes (document and element).
    /// </summary>
    public abstract class ContainerNode : TreeNode
    {
        protected ContainerNode(ANode aNode, int kind, Database database) : base(aNode, kind, database) { }

        #region Content

        /// <summary>
        /// Removes all child nodes (including attribute nodes).
        /// </summary>
        public virtual void RemoveAll()
        {
            using (UpgradeableReadLock())
            {
                Check(true);
                ANode[] nodes = EnumerateANodes(ANode.children()).ToArray();
                Updates.Add(new Delete(null, Seq.get(nodes, nodes.Length)));
            }
        }

        /// <summary>
        /// Appends the specified content to this node.
        /// </summary>
        /// <param name="xmlReader">The XML reader to get content from.</param>
        public void Append(XmlReader xmlReader)
        {
            if (xmlReader == null) throw new ArgumentNullException("xmlReader");
            Append(Helper.GetNodeCache(xmlReader));
        }

        /// <summary>
        /// Appends the specified XML content to this node.
        /// </summary>
        /// <param name="content">The content to append.</param>
        public void Append(string content)
        {
            Helper.CallWithString(content, Append);
        }

        /// <summary>
        /// Appends the specified nodes to this node.
        /// </summary>
        /// <param name="nodes">The nodes to append.</param>
        public void Append(params Node[] nodes)
        {
            if (nodes == null) throw new ArgumentNullException("nodes");
            Append(Helper.GetNodeCache(nodes));
        }

        /// <summary>
        /// Appends the specified nodes to this node.
        /// </summary>
        /// <param name="nodes">The nodes to append.</param>
        public void Append(IEnumerable<Node> nodes)
        {
            if (nodes == null) throw new ArgumentNullException("nodes");
            Append(Helper.GetNodeCache(nodes));
        }

        private void Append(NodeCache nodeCache)
        {
            using (UpgradeableReadLock())
            {
                Check(true);
                if (nodeCache != null)
                {
                    Updates.Add(new Insert(null, nodeCache.value(), false, true, false, false, DbNode));
                }
            }
        }

        /// <summary>
        /// Prepends the specified content to this node.
        /// </summary>
        /// <param name="xmlReader">The XML reader to get content from.</param>
        public void Prepend(XmlReader xmlReader)
        {
            if (xmlReader == null) throw new ArgumentNullException("xmlReader");
            Prepend(Helper.GetNodeCache(xmlReader));
        }

        /// <summary>
        /// Prepends the specified XML content to this node.
        /// </summary>
        /// <param name="content">The content to prepend.</param>
        public void Prepend(string content)
        {
            Helper.CallWithString(content, Prepend);
        }

        /// <summary>
        /// Prepends the specified nodes to this node.
        /// </summary>
        /// <param name="nodes">The nodes to prepend.</param>
        public void Prepend(params Node[] nodes)
        {
            if (nodes == null) throw new ArgumentNullException("nodes");
            Prepend(Helper.GetNodeCache(nodes));
        }

        /// <summary>
        /// Prepends the specified nodes to this node.
        /// </summary>
        /// <param name="nodes">The nodes to prepend.</param>
        public void Prepend(IEnumerable<Node> nodes)
        {
            if (nodes == null) throw new ArgumentNullException("nodes");
            Prepend(Helper.GetNodeCache(nodes));
        }

        private void Prepend(NodeCache nodeCache)
        {
            using (UpgradeableReadLock())
            {
                Check(true);
                if (nodeCache != null)
                {
                    Updates.Add(new Insert(null, nodeCache.value(), true, false, false, false, DbNode));
                }
            }
        }

        /// <summary>
        /// Gets or sets the inner XML content of this node.
        /// </summary>
        /// <value>
        /// The inner XML content.
        /// </value>
        public string InnerXml
        {
            get
            {
                StringBuilder builder = new StringBuilder();
                using (XmlWriter xmlWriter = XmlWriter.Create(builder, Helper.WriterSettings))
                {
                    WriteInnerXml(xmlWriter);
                }
                return builder.ToString();
            }
            set
            {
                Helper.CallWithString(value, ReadInnerXml);
            }
        }

        /// <summary>
        /// Writes the inner XML content of this node.
        /// </summary>
        /// <param name="xmlWriter">The XML writer to write to.</param>
        public void WriteInnerXml(XmlWriter xmlWriter)
        {
            if (xmlWriter == null) throw new ArgumentNullException("xmlWriter");
            using (ReadLock())
            {
                Check();
                using (XmlWriterSerializer serializer = new XmlWriterSerializer(xmlWriter, false))
                {
                    foreach (ANode node in EnumerateANodes(ANode.children()))
                    {
                        node.serialize(serializer);
                    }
                }
            }
        }

        /// <summary>
        /// Reads and sets the inner XML content of this node.
        /// </summary>
        /// <param name="xmlReader">The XML reader to read from.</param>
        public void ReadInnerXml(XmlReader xmlReader)
        {
            if (xmlReader == null) throw new ArgumentNullException("xmlReader");
            using (UpgradeableReadLock())
            {
                Check(true);
                ReplaceChildren(Helper.GetNodeCache(xmlReader));
            }
        }

        /// <summary>
        /// Gets the outer XML content of this node.
        /// </summary>
        public string OuterXml
        {
            get
            {
                StringBuilder builder = new StringBuilder();
                using (XmlWriter xmlWriter = XmlWriter.Create(builder, Helper.WriterSettings))
                {
                    WriteOuterXml(xmlWriter);
                }
                return builder.ToString();
            }
        }

        /// <summary>
        /// Writes the outer XML content of this node.
        /// </summary>
        /// <param name="xmlWriter">The XML writer to write to.</param>
        public void WriteOuterXml(XmlWriter xmlWriter)
        {
            if (xmlWriter == null) throw new ArgumentNullException("xmlWriter");
            using (ReadLock())
            {
                Check();
                using (XmlWriterSerializer serializer = new XmlWriterSerializer(xmlWriter, false))
                {
                    ANode.serialize(serializer);
                }
            }
        }

        /// <summary>
        /// Gets or sets the inner text content of this node.
        /// </summary>
        /// <value>
        /// The inner text content.
        /// </value>
        public string InnerText
        {
            get
            {
                using (StringWriter stringWriter = new StringWriter())
                {
                    WriteInnerText(stringWriter);
                    stringWriter.Close();
                    return stringWriter.ToString();
                }
            }
            set
            {
                if (value == null) throw new ArgumentNullException("value");
                using (UpgradeableReadLock())
                {
                    Check(true);
                    ReplaceChildren(Helper.GetNodeCache(new FTxt(value.Token())));
                }
            }
        }

        /// <summary>
        /// Gets a TextReader that can be used to stream inner text content for this node. The TextReader
        /// must be disposed and while it is not, the database cannot be accessed except through the TextReader
        /// </summary>
        public TextReader InnerTextReader
        {
            get
            {
                ReadLock readLock = ReadLock(); //Don't dispose - the InnerTextReader will do that
                Check();
                return new InnerTextReader(EnumerateANodes(ANode.descendant())
                    .Where(n => n.nodeType() == org.basex.query.item.NodeType.TXT).GetEnumerator(),
                    readLock);
            }
        }

        /// <summary>
        /// Writes the inner text content of this node.
        /// </summary>
        /// <param name="textWriter">The text writer to write to.</param>
        public void WriteInnerText(TextWriter textWriter)
        {
            if (textWriter == null) throw new ArgumentNullException("textWriter");
            using (ReadLock())
            {
                Check();
                using (TextWriterSerializer serializer = new TextWriterSerializer(textWriter))
                {
                    ANode.serialize(serializer);
                }
            }
        }

        /// <summary>
        /// Reads and sets the inner text content of this node.
        /// </summary>
        /// <param name="textReader">The text reader to read from.</param>
        public void ReadInnerText(TextReader textReader)
        {
            if (textReader == null) throw new ArgumentNullException("textReader");
            using (UpgradeableReadLock())
            {
                Check(true);
                ReplaceChildren(Helper.GetNodeCache(new FTxt(textReader.ReadToEnd().Token())));
            }
        }

        // Used by ReadInnerXml(), ReadInnerText(), and set_Value()
        // Deletes all the child nodes and adds the nodeCache nodes, does nothing if nodeCache is null
        // This method is not thread-safe, called should lock the database
        private void ReplaceChildren(NodeCache nodeCache)
        {
            if (nodeCache != null)
            {
                using (new Updates())
                {
                    ANode[] nodes = EnumerateANodes(ANode.children()).ToArray();
                    Updates.Add(new Delete(null, Seq.get(nodes, nodes.Length)));
                    Updates.Add(new Insert(null, nodeCache.value(), false, true, false, false, DbNode));
                }
                
            }
                    
        }

        /// <inheritdoc />
        public override string Value
        {
            get { return InnerText; }
            set { InnerText = value; }
        }

        #endregion
    }
}
