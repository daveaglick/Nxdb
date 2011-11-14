using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Nxdb.Io
{
    internal class LsbReader : TextReader
    {
        private readonly LargeStringBuffer _buffer;
        private int _loc = 0;

        public LsbReader(LargeStringBuffer buffer)
        {
            _buffer = buffer;
        }

        public override int Peek()
        {
            if( _loc >= _buffer.Length )
            {
                return -1;
            }
            return _buffer[_loc];
        }

        public override int Read()
        {
            if (_loc >= _buffer.Length)
            {
                return -1;
            }
            return _buffer[_loc++];
        }

        public override string ReadToEnd()
        {
            return _buffer.Substring(_loc, _buffer.Length - _loc);
        }
    }
}