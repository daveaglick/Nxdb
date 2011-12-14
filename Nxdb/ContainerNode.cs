using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using Nxdb.Io;
using org.basex.query.item;
using org.basex.query.iter;
using org.basex.query.up.primitives;

namespace Nxdb
{
    /// <summary>
    /// Base class for nodes that can contain other nodes (document and element).
    /// </summary>
    public abstract class ContainerNode : TreeNode
    {
        protected ContainerNode(ANode aNode, Database database, int kind) : base(aNode, database, kind) { }

        #region Content

        /// <summary>
        /// Removes all child nodes (including attribute nodes).
        /// </summary>
        public virtual void RemoveAll()
        {
            Check(true);
            using (new UpdateContext())
            {
                foreach (DBNode node in EnumerateANodes(ANode.children()).OfType<DBNode>())
                {
                    Update(new DeleteNode(node.pre, Database.Data, null));
                }
            }
        }

        /// <summary>
        /// Appends the specified content to this node.
        /// </summary>
        /// <param name="xmlReader">The XML reader to get content from.</param>
        public void Append(XmlReader xmlReader)
        {
            if (xmlReader == null) throw new ArgumentNullException("xmlReader");
            Check(true);
            NodeCache nodeCache = Helper.GetNodeCache(xmlReader);
            if (nodeCache != null)
            {
                using (new UpdateContext())
                {
                    Update(new InsertInto(DbNode.pre, Database.Data, null, nodeCache, true));
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
            Check(true);
            NodeCache nodeCache = Helper.GetNodeCache(xmlReader);
            if (nodeCache != null)
            {
                using (new UpdateContext())
                {
                    Update(new InsertIntoFirst(DbNode.pre, Database.Data, null, nodeCache));
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
                if (value == null) throw new ArgumentNullException("value");
                using (StringReader stringReader = new StringReader(value))
                {
                    using (XmlReader xmlReader = XmlReader.Create(stringReader, Helper.ReaderSettings))
                    {
                        ReadInnerXml(xmlReader);
                    }
                }
            }
        }

        /// <summary>
        /// Writes the inner XML content of this node.
        /// </summary>
        /// <param name="xmlWriter">The XML writer to write to.</param>
        public void WriteInnerXml(XmlWriter xmlWriter)
        {
            if (xmlWriter == null) throw new ArgumentNullException("xmlWriter");
            Check();
            using (XmlWriterSerializer serializer = new XmlWriterSerializer(xmlWriter, false))
            {
                foreach (ANode node in EnumerateANodes(ANode.children()))
                {
                    node.serialize(serializer);
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
            Check(true);
            ReplaceChildren(Helper.GetNodeCache(xmlReader));
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
            Check();
            using (XmlWriterSerializer serializer = new XmlWriterSerializer(xmlWriter, false))
            {
                ANode.serialize(serializer);
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
                Check(true);
                ReplaceChildren(Helper.GetNodeCache(new ANode[] { new FTxt(value.Token()) }));
            }
        }

        /// <summary>
        /// Gets a TextReader that can be used to stream inner text content for this node.
        /// </summary>
        public TextReader InnerTextReader
        {
            get
            {
                Check();
                return new InnerTextReader(EnumerateANodes(ANode.descendant())
                    .Where(n => n.ndType() == NodeType.TXT).GetEnumerator());
            }
        }

        /// <summary>
        /// Writes the inner text content of this node.
        /// </summary>
        /// <param name="textWriter">The text writer to write to.</param>
        public void WriteInnerText(TextWriter textWriter)
        {
            if (textWriter == null) throw new ArgumentNullException("textWriter");
            Check();
            using (TextWriterSerializer serializer = new TextWriterSerializer(textWriter))
            {
                ANode.serialize(serializer);
            }
        }

        /// <summary>
        /// Reads and sets the inner text content of this node.
        /// </summary>
        /// <param name="textReader">The text reader to read from.</param>
        public void ReadInnerText(TextReader textReader)
        {
            if (textReader == null) throw new ArgumentNullException("textReader");
            Check(true);
            ReplaceChildren(Helper.GetNodeCache(new ANode[] { new FTxt(textReader.ReadToEnd().Token()) }));
        }

        // Used by ReadInnerXml(), ReadInnerText(), and Value.set
        // Deletes all the child nodes and adds the nodeCache nodes, does nothing if nodeCache is null
        private void ReplaceChildren(NodeCache nodeCache)
        {
            if (nodeCache != null)
            {
                using (new UpdateContext())
                {
                    // Remove all child elements
                    foreach (DBNode node in EnumerateANodes(ANode.children()).OfType<DBNode>())
                    {
                        Update(new DeleteNode(node.pre, Database.Data, null));
                    }

                    // Append the new content
                    Update(new InsertInto(DbNode.pre, Database.Data, null, nodeCache, true));
                }
            }
        }

        public override string Value
        {
            get { return InnerText; }
            set { InnerText = value; }
        }

        #endregion
    }
}
