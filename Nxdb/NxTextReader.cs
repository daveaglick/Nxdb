using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using org.basex.data;

namespace Nxdb
{
    internal class NxTextReader : TextReader
    {
        private readonly NxCollection collection;
        private readonly IEnumerator<int> descendants;

        //The current descendant pre
        private int pre = -1;

        //The text buffer - when empty go to the next descendant
        private readonly StringBuilder bufferBuilder = new StringBuilder();

        public NxTextReader(NxCollection collection, int pre, int kind)
        {
            this.collection = collection;
            this.pre = pre;
            descendants = NxNode.GetDescendantPres(collection.Data, pre, kind).GetEnumerator();
            FillBuffer();
        }

        private bool FillBuffer()
        {
            while(descendants.MoveNext())
            {
                pre = descendants.Current;
                if(NxNode.GetKind(collection.Data, pre) == Data.TEXT)
                {
                    bufferBuilder.Append(NxNode.GetValue(collection.Data, pre, Data.TEXT));
                    return true;
                }
            }
            return false;
        }

        public override int Peek()
        {
            while (bufferBuilder.Length == 0 && FillBuffer()) { }
            if (bufferBuilder.Length == 0)
            {
                return -1;
            }
            return bufferBuilder[0];
        }


        public override int Read()
        {
            while (bufferBuilder.Length == 0 && FillBuffer()) { }
            if (bufferBuilder.Length == 0)
            {
                return -1;
            }
            char c = bufferBuilder[0];
            bufferBuilder.Remove(0, 1);
            return c;
        }

        public override string ReadToEnd()
        {
            while (FillBuffer()) { }
            return bufferBuilder.ToString();
        }

        public override int Read(char[] buffer, int index, int count)
        {
            while (bufferBuilder.Length < count && FillBuffer()) { }
            int available = count > bufferBuilder.Length ? bufferBuilder.Length : count;
            bufferBuilder.CopyTo(0, buffer, index, available);
            bufferBuilder.Remove(0, available);
            return available;
        }

        public override string ReadLine()
        {
            //TODO - NxTextReader.ReadLine()
            throw new NotSupportedException();
        }

        public override int ReadBlock(char[] buffer, int index, int count)
        {
            throw new NotSupportedException();
        }

    }
}
