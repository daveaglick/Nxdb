using System;
using System.Xml;
using org.basex.io.serial;
using org.basex.query.item;

namespace Nxdb.Io
{
    //TODO: Fix treating of xml declaration as text for serialization
    //If the original input has an xml declaration, that appears to get read into BaseX as text
    //content before the opening element. When it gets serialized back out, the XmlWriter throws an
    //exception that there is text content before the opening element.
    internal class XmlWriterSerializer : Serializer, IDisposable
    {
        private readonly XmlWriter _xmlWriter;
        private readonly bool _writeDocument;

        public XmlWriterSerializer(XmlWriter xmlWriter, bool writeDocument)
        {
            _xmlWriter = xmlWriter;
            _writeDocument = writeDocument;

            if (writeDocument)
            {
                xmlWriter.WriteStartDocument();
            }
        }

        //Starts an opening element tag
        protected override void startOpen(byte[] name)
        {
            _xmlWriter.WriteStartElement(name.Token());
        }

        //Writes an attribute
        public override void attribute(byte[] name, byte[] value)
        {
            _xmlWriter.WriteAttributeString(name.Token(), value.Token());
        }

        //Finishes an opening element tag
        protected override void finishOpen()
        {
            //Don't need to do anything
        }

        //Finishes an empty element tag
        protected override void finishEmpty()
        {
            _xmlWriter.WriteEndElement();
        }

        //Finishes a closing element tag
        protected override void finishClose()
        {
            _xmlWriter.WriteEndElement();
        }

        protected override void finishText(byte[] text)
        {
            _xmlWriter.WriteString(text.Token());
        }

        protected override void finishComment(byte[] comment)
        {
            _xmlWriter.WriteComment(comment.Token());
        }

        protected override void finishPi(byte[] name, byte[] text)
        {
            _xmlWriter.WriteProcessingInstruction(name.Token(), text.Token());
        }

        //Not supported - only used internally by BaseX
        protected override void finishItem(Item i)
        {
            throw new NotSupportedException();
        }
        
        public void Dispose()
        {
            if (_writeDocument)
            {
                _xmlWriter.WriteEndDocument();
            }
        }
    }
}
