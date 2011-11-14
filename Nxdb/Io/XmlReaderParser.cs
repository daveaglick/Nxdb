using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using org.basex.build;
using org.basex.io;
using org.basex.io.@in;
using org.basex.util;
using org.xml.sax;
using Parser = org.basex.build.Parser;

namespace Nxdb.Io
{
    //Implements a BaseX Parser from a .NET XmlReader
    internal class XmlReaderParser : Parser
    {
        private readonly string _name;
        private readonly XmlReader _reader;

        public XmlReaderParser(string name, XmlReader reader) : base((IO)null)
        {
            _name = name;
            _reader = reader;
        }

        //Based on org.basex.build.xml.XMLParser.parse()
        public override void parse(Builder builder)
        {
            builder.startDoc(Token.token(_name));
            while(_reader.Read())
            {
                switch(_reader.NodeType)
                {
                    case XmlNodeType.XmlDeclaration:
                        builder.encoding(_reader.GetAttribute("encoding") ?? Token.UTF8);
                        break;
                    case XmlNodeType.Text:
                    case XmlNodeType.SignificantWhitespace:
                    case XmlNodeType.Whitespace:
                        builder.text(Token.token(_reader.Value));
                        break;
                    case XmlNodeType.Comment:
                        builder.comment(Token.token(_reader.Value));
                        break;
                    case XmlNodeType.ProcessingInstruction:
                        builder.pi(Token.token(_reader.Name + " " + _reader.Value));
                        break;
                    case XmlNodeType.Element:
                        ParseAttributes(builder);
                        if(_reader.IsEmptyElement)
                        {
                            builder.emptyElem(Token.token(_reader.Name), atts);
                        }
                        else
                        {
                            builder.startElem(Token.token(_reader.Name), atts);
                        }
                        break;
                    case XmlNodeType.EndElement:
                        builder.endElem();
                        break;
                }
            }
            builder.endDoc();
        }

        private void ParseAttributes(Builder builder)
        {
            atts.reset();
            if (_reader.HasAttributes)
            {
                while (_reader.MoveToNextAttribute())
                {
                    byte[] attName = Token.token(_reader.Name);
                    byte[] attValue = Token.token(_reader.Value);
                    if (Token.startsWith(attName, Token.XMLNSC))
                    {
                        builder.startNS(Token.ln(attName), attValue);
                    }
                    else if (Token.eq(attName, Token.XMLNS))
                    {
                        builder.startNS(Token.EMPTY, attValue);
                    }
                    else
                    {
                        atts.add(attName, attValue);
                    }
                }
                _reader.MoveToElement();
            }
        }
    }
}
