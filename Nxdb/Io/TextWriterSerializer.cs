using System;
using System.IO;
using org.basex.data;
using org.basex.io.serial;
using org.basex.query.item;
using org.basex.util;

namespace Nxdb.Io
{
    //Serializes the text content (InnerText) of a BaseX node to a TextWriter
    internal class TextWriterSerializer : Serializer, IDisposable
    {
        private readonly TextWriter _textWriter;

        public TextWriterSerializer(TextWriter textWriter)
        {
            _textWriter = textWriter;
        }

        protected override void finishText(byte[] text)
        {
            _textWriter.Write(text.Token());
        }

        //Starts an opening element tag
        protected override void startOpen(byte[] name)
        {
            //Do nothing
        }

        //Writes an attribute
        public override void attribute(byte[] name, byte[] value)
        {
            //Do nothing
        }

        //Finishes an opening element tag
        protected override void finishOpen()
        {
            //Do nothing
        }

        //Finishes an empty element tag
        protected override void finishEmpty()
        {
            //Do nothing
        }

        //Finishes a closing element tag
        protected override void finishClose()
        {
            //Do nothing
        }

        protected override void finishComment(byte[] comment)
        {
            //Do nothing
        }

        protected override void finishPi(byte[] name, byte[] text)
        {
            //Do nothing
        }

        //Not supported - only used internally by BaseX
        protected override void finishItem(Item i)
        {
            throw new NotSupportedException();
        }

        public void Dispose()
        {
        }
    }
}
