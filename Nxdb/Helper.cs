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
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using com.sun.org.apache.xerces.@internal.jaxp.datatype;
using java.math;
using javax.xml.datatype;
using javax.xml.@namespace;
using org.basex.query.func;
using org.basex.query.item;
using org.basex.query.iter;
using org.basex.util;

namespace Nxdb
{
    /// <summary>
    /// Static class that includes some extension methods and other helper functionality.
    /// </summary>
    public static class Helper
    {
        private static XmlWriterSettings _writerSettings;
        private static XmlReaderSettings _readerSettings;

        /// <summary>
        /// Gets an instance of very permissive XmlWriterSettings designed to restrict post-processing by an XmlWriter.
        /// </summary>
        public static XmlWriterSettings WriterSettings
        {
            get
            {
                if (_writerSettings == null)
                {
                    _writerSettings = new XmlWriterSettings();
                    _writerSettings.Indent = false;
                    _writerSettings.OmitXmlDeclaration = true;
                    _writerSettings.CheckCharacters = false;
                    _writerSettings.NewLineHandling = NewLineHandling.None;
                    _writerSettings.NewLineOnAttributes = false;
                    _writerSettings.ConformanceLevel = ConformanceLevel.Auto;
                }
                return _writerSettings;
            }
        }

        /// <summary>
        /// Gets an instance of very permissive XmlReaderSettings designed to restrict post-processing by an XmlReader.
        /// </summary>
        public static XmlReaderSettings ReaderSettings
        {
            get
            {
                if (_readerSettings == null)
                {
                    _readerSettings = new XmlReaderSettings();
                    _readerSettings.IgnoreComments = false;
                    _readerSettings.IgnoreProcessingInstructions = false;
                    _readerSettings.IgnoreWhitespace = false;
                    _readerSettings.CheckCharacters = false;
                    _readerSettings.ConformanceLevel = ConformanceLevel.Fragment;
                }
                return _readerSettings;
            }
        }

        internal static NodeCache GetNodeCache(IEnumerable<Node> nodes)
        {
            return GetNodeCache(nodes.Select(n => n.ANode).ToArray());
        }
        
        internal static NodeCache GetNodeCache(params ANode[] nodes)
        {
            return new NodeCache(nodes, nodes.Length);
        }

        internal static NodeCache GetNodeCache(XmlReader reader)
        {
            IList<ANode> nodes = GetNodes(reader);
            return nodes != null ? new NodeCache(nodes.ToArray(), nodes.Count) : null;
        }

        internal static ANode[] GetNodes(IEnumerable<Node> nodes)
        {
            return nodes.Select(n => n.ANode).ToArray();
        }

        internal static ANode[] GetNodes(XmlReader reader)
        {
            List<ANode> nodes = new List<ANode>();
            Stack<FElem> parents = new Stack<FElem>();
            try
            {
                if (reader.ReadState == ReadState.Initial)
                {
                    reader.Read();
                }
                while (reader.ReadState == ReadState.Interactive)
                {
                    switch (reader.NodeType)
                    {
                        case XmlNodeType.Element:
                            //Create the element and add it to the parent or list
                            FElem elem = new FElem(new QNm(reader.Name.Token()));
                            AddNode(elem, nodes, parents);

                            //Add attributes
                            if (reader.HasAttributes)
                            {
                                while (reader.MoveToNextAttribute())
                                {
                                    elem.add(new FAttr(new QNm(reader.Name.Token()), reader.Value.Token()));
                                }
                                reader.MoveToElement();
                            }

                            //Push to the parents stack if not empty
                            if (!reader.IsEmptyElement)
                            {
                                parents.Push(elem);
                            }

                            break;
                        case XmlNodeType.EndElement:
                            parents.Pop();
                            break;
                        case XmlNodeType.Comment:
                            AddNode(new FComm(reader.Value.Token()), nodes, parents);
                            break;
                        case XmlNodeType.Text:
                        case XmlNodeType.SignificantWhitespace:
                        case XmlNodeType.Whitespace:
                            AddNode(new FTxt(reader.Value.Token()), nodes, parents);
                            break;
                        case XmlNodeType.ProcessingInstruction:
                            AddNode(new FPI(new QNm(reader.Name.Token()), reader.Value.Token()), nodes, parents);
                            break;
                    }
                    reader.Read();
                }
                return nodes.ToArray();
            }
            catch (Exception)
            {
                return null;
            }
        }

        //Helper method for the GetNodes method
        private static void AddNode(FNode node, List<ANode> nodes, Stack<FElem> parents)
        {
            if (parents.Count > 0)
            {
                parents.Peek().add(node);
            }
            else
            {
                nodes.Add(node);
            }
        }
        
        // Helper to execute a method that takes an XmlReader given a string
        internal static void CallWithString(string content, Action<XmlReader> action)
        {
            if (content == null) throw new ArgumentNullException("content");
            using (StringReader stringReader = new StringReader(content))
            {
                using (XmlReader xmlReader = XmlReader.Create(stringReader, Helper.ReaderSettings))
                {
                    action(xmlReader);
                }
            }
        }

        internal static void CallWithString<T>(string content, T param, Action<T, XmlReader> action)
        {
            if (content == null) throw new ArgumentNullException("content");
            using (StringReader stringReader = new StringReader(content))
            {
                using (XmlReader xmlReader = XmlReader.Create(stringReader, Helper.ReaderSettings))
                {
                    action(param, xmlReader);
                }
            }
        }

        internal static T CallWithString<T>(string content, Func<XmlReader, T> func)
        {
            if (content == null) throw new ArgumentNullException("content");
            using (StringReader stringReader = new StringReader(content))
            {
                using (XmlReader xmlReader = XmlReader.Create(stringReader, Helper.ReaderSettings))
                {
                    return func(xmlReader);
                }
            }
        }

        internal static TR CallWithString<TP, TR>(string content, TP param, Func<TP, XmlReader, TR> func)
        {
            if (content == null) throw new ArgumentNullException("content");
            using (StringReader stringReader = new StringReader(content))
            {
                using (XmlReader xmlReader = XmlReader.Create(stringReader, Helper.ReaderSettings))
                {
                    return func(param, xmlReader);
                }
            }
        }
    }
}
