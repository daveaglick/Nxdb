using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using org.basex.query.item;
using org.basex.query.iter;
using org.basex.util;

namespace Nxdb
{
    //Extension methods and other helper functionality
    public static class Helper
    {
        //A couple extension methods to help with the tokenizing of strings
        internal static string Token(this byte[] bytes)
        {
            return org.basex.util.Token.@string(bytes);
        }

        internal static byte[] Token(this string str)
        {
            return org.basex.util.Token.token(str);
        }

        //Static XmlReader and XmlWriter settings
        //Most permisive possible, don't want the writer/reader doing any post-processing
        private static XmlWriterSettings _writerSettings;
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

        private static XmlReaderSettings _readerSettings;
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
        
        internal static NodeCache GetNodeCache(params ANode[] nodes)
        {
            return new NodeCache(nodes, nodes.Length);
        }

        internal static NodeCache GetNodeCache(XmlReader reader)
        {
            List<ANode> nodes = new List<ANode>();
            Stack<FElem> parents = new Stack<FElem>();
            try
            {
                while (reader.Read())
                {
                    string value = reader.Value;
                    switch (reader.NodeType)
                    {
                        case XmlNodeType.Element:
                            //Create the element and add it to the parent or list
                            FElem elem = new FElem(new QNm(reader.Name.Token()));
                            AddNodeToNodeCache(elem, nodes, parents);

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
                            AddNodeToNodeCache(new FComm(reader.Value.Token()), nodes, parents);
                            break;
                        case XmlNodeType.Text:
                        case XmlNodeType.SignificantWhitespace:
                        case XmlNodeType.Whitespace:
                            AddNodeToNodeCache(new FTxt(reader.Value.Token()), nodes, parents);
                            break;
                        case XmlNodeType.ProcessingInstruction:
                            AddNodeToNodeCache(new FPI(new QNm(reader.Name.Token()), reader.Value.Token()), nodes, parents);
                            break;
                    }
                }
                return new NodeCache(nodes.ToArray(), nodes.Count);
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        //Helper method for the GetNodeCache method
        private static void AddNodeToNodeCache(FNode node, List<ANode> nodes, Stack<FElem> parents)
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
    }
}
