using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Nxdb.Io
{
    internal class LsbWriter : TextWriter
    {
        private readonly LargeStringBuffer _buffer;

        public LsbWriter(LargeStringBuffer buffer)
        {
            _buffer = buffer;
        }

        public override Encoding Encoding
        {
            get { return Encoding.Default; }
        }

        public override void Write(char value)
        {
            _buffer.Append(new []{value});
        }

        public override void Write(char[] buffer)
        {
            _buffer.Append(buffer);
        }

        public override void Write(char[] buffer, int index, int count)
        {
            _buffer.Append(buffer, index, count);
        }

        public override void Write(string value)
        {
            _buffer.Append(value);
        }

        public override void Write(bool value)
        {
            throw new NotImplementedException();
        }

        public override void Write(int value)
        {
            throw new NotImplementedException();
        }

        public override void Write(uint value)
        {
            throw new NotImplementedException();
        }

        public override void Write(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(ulong value)
        {
            throw new NotImplementedException();
        }

        public override void Write(float value)
        {
            throw new NotImplementedException();
        }

        public override void Write(double value)
        {
            throw new NotImplementedException();
        }

        public override void Write(decimal value)
        {
            throw new NotImplementedException();
        }

        public override void Write(object value)
        {
            throw new NotImplementedException();
        }

        public override void Write(string format, object arg0)
        {
            throw new NotImplementedException();
        }

        public override void Write(string format, object arg0, object arg1)
        {
            throw new NotImplementedException();
        }

        public override void Write(string format, object arg0, object arg1, object arg2)
        {
            throw new NotImplementedException();
        }

        public override void Write(string format, params object[] arg)
        {
            throw new NotImplementedException();
        }

        public override void WriteLine()
        {
            throw new NotImplementedException();
        }

        public override void WriteLine(char value)
        {
            throw new NotImplementedException();
        }

        public override void WriteLine(char[] buffer)
        {
            throw new NotImplementedException();
        }

        public override void WriteLine(char[] buffer, int index, int count)
        {
            throw new NotImplementedException();
        }

        public override void WriteLine(bool value)
        {
            throw new NotImplementedException();
        }

        public override void WriteLine(int value)
        {
            throw new NotImplementedException();
        }

        public override void WriteLine(uint value)
        {
            throw new NotImplementedException();
        }

        public override void WriteLine(long value)
        {
            throw new NotImplementedException();
        }

        public override void WriteLine(ulong value)
        {
            throw new NotImplementedException();
        }

        public override void WriteLine(float value)
        {
            throw new NotImplementedException();
        }

        public override void WriteLine(double value)
        {
            throw new NotImplementedException();
        }

        public override void WriteLine(decimal value)
        {
            throw new NotImplementedException();
        }

        public override void WriteLine(string value)
        {
            throw new NotImplementedException();
        }

        public override void WriteLine(object value)
        {
            throw new NotImplementedException();
        }

        public override void WriteLine(string format, object arg0)
        {
            throw new NotImplementedException();
        }

        public override void WriteLine(string format, object arg0, object arg1)
        {
            throw new NotImplementedException();
        }

        public override void WriteLine(string format, object arg0, object arg1, object arg2)
        {
            throw new NotImplementedException();
        }

        public override void WriteLine(string format, params object[] arg)
        {
            throw new NotImplementedException();
        }
    }
}