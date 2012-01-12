using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Drac.Threading;
using org.basex.data;
using org.basex.query.item;

namespace Nxdb.Io
{
    internal class InnerTextReader : TextReader
    {
        private readonly IEnumerator<ANode> _textNodes;
        private readonly ReadLock _readLock;

        //The text buffer - when empty go to the next text node
        private readonly StringBuilder _buffer = new StringBuilder();

        public InnerTextReader(IEnumerator<ANode> textNodes, ReadLock readLock)
        {
            _textNodes = textNodes;
            _readLock = readLock;
            FillBuffer();
        }

        private bool FillBuffer()
        {
            while (_textNodes.MoveNext() && _textNodes.Current != null)
            {
                _buffer.Append(_textNodes.Current.@string().Token());
                return true;
            }
            return false;
        }

        public override int Peek()
        {
            while (_buffer.Length == 0 && FillBuffer()) { }
            if (_buffer.Length == 0)
            {
                return -1;
            }
            return _buffer[0];
        }

        public override int Read()
        {
            while (_buffer.Length == 0 && FillBuffer()) { }
            if (_buffer.Length == 0)
            {
                return -1;
            }
            char c = _buffer[0];
            _buffer.Remove(0, 1);
            return c;
        }

        public override string ReadToEnd()
        {
            while (FillBuffer()) { }
            return _buffer.ToString();
        }

        public override int Read(char[] buffer, int index, int count)
        {
            while (_buffer.Length < count && FillBuffer()) { }
            int available = count > _buffer.Length ? _buffer.Length : count;
            _buffer.CopyTo(0, buffer, index, available);
            _buffer.Remove(0, available);
            return available;
        }

        public override string ReadLine()
        {
            //TODO - InnerTextReader.ReadLine()
            throw new NotSupportedException();
        }

        public override int ReadBlock(char[] buffer, int index, int count)
        {
            throw new NotSupportedException();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            _readLock.Dispose();
        }
    }
}
