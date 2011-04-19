using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using org.basex.data;
using org.basex.util;

namespace Nxdb
{
    //Serializes the text content (InnerText) of a BaseX node to a TextWriter
    internal class TextWriterSerializer : Serializer, IDisposable
    {
        private readonly TextWriter textWriter;

        public TextWriterSerializer(TextWriter textWriter)
        {
            this.textWriter = textWriter;
        }
        
        //Serializes an item (only called in default org.basex.query.item.Item.serialize)
        public override void item(byte[] barr)
        {
            textWriter.Write(barr);
        }

        //Serializes a text node
        public override void text(byte[] barr)
        {
            textWriter.Write(Token.@string(barr));
        }

        //Not sure what this override does differently
        public override void text(byte[] barr, FTPos ftp)
        {
            text(barr);
        }

        //Opens a new element tag
        protected override void start(byte[] barr)
        {
            //Do nothing
        }

        public override void attribute(byte[] barr1, byte[] barr2)
        {
            //Do nothing
        }

        //Intended to print out the closing '>' for an open element tag
        protected override void finish()
        {
            //Do nothing
        }

        //Serialize a comment
        public override void comment(byte[] barr)
        {
            //Do nothing
        }

        //Serialize a processing instruction
        public override void pi(byte[] barr1, byte[] barr2)
        {
            //Do nothing
        }

        //Closes an element
        protected override void close(byte[] barr)
        {
            //Do nothing
        }

        //Closes an empty element
        protected override void empty()
        {
            //Do nothing
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
            //Do nothing
        }

        public void Dispose()
        {
            close();
        }
    }
}
