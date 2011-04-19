using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using org.basex.data;
using org.basex.util;

namespace Nxdb
{
    //TODO: If the original input has an xml declaration, that appears to get read into BaseX as text
    //content before the opening element. When it gets serialized back out, the XmlWriter throws an
    //exception that there is text content before the opening element.
    internal class XmlWriterSerializer : Serializer, IDisposable
    {
        private readonly XmlWriter xmlWriter;
        private readonly bool writeDocumentContent;

        public XmlWriterSerializer(XmlWriter xmlWriter, bool writeDocumentContent)
        {
            this.xmlWriter = xmlWriter;
            if (writeDocumentContent)
            {
                xmlWriter.WriteStartDocument();
            }
            this.writeDocumentContent = writeDocumentContent;
        }

        //Serializes an item (only called in default org.basex.query.item.Item.serialize)
        public override void item(byte[] barr)
        {
            text(barr);
        }
        
        //Opens a new element tag
        protected override void start(byte[] barr)
        {
            xmlWriter.WriteStartElement(Token.@string(barr));
        }

        public override void attribute(byte[] barr1, byte[] barr2)
        {
            xmlWriter.WriteAttributeString(Token.@string(barr1), Token.@string(barr2));
        }

        //Intended to print out the closing '>' for an open element tag
        protected override void finish()
        {
            //Do nothing
        }

        //Serializes a text node
        public override void text(byte[] barr)
        {
            xmlWriter.WriteString(Token.@string(barr));
        }

        //Not sure what this override does differently
        public override void text(byte[] barr, FTPos ftp)
        {
            text(barr);
        }

        //Serialize a comment
        public override void comment(byte[] barr)
        {
            xmlWriter.WriteComment(Token.@string(barr));
        }

        //Serialize a processing instruction
        public override void pi(byte[] barr1, byte[] barr2)
        {
            xmlWriter.WriteProcessingInstruction(Token.@string(barr1), Token.@string(barr2));
        }

        //Closes an element
        protected override void close(byte[] barr)
        {
            xmlWriter.WriteEndElement();
        }

        //Closes an empty element
        protected override void empty()
        {
            xmlWriter.WriteEndElement();
        }

        //Opens a result set
        public override void openResult()
        {
            throw new NotImplementedException();
        }

        //Closes a result set
        public override void closeResult()
        {
            throw new NotImplementedException();
        }

        //Closes the serializer
        protected override void cls()
        {
            if (writeDocumentContent)
            {
                xmlWriter.WriteEndDocument();
            }
        }

        public void Dispose()
        {
            close();
        }
    }
}
