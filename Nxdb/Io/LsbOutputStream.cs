using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using java.io;

namespace Nxdb.Io
{
    //Provides an OutputStream that org.basex.io.PrintOutput can use to store the output of commands
    internal class LsbOutputStream : OutputStream
    {
        private readonly Encoding _encoding = Encoding.UTF8;
        private readonly LargeStringBuffer _buffer = new LargeStringBuffer();

        public LargeStringBuffer Buffer
        {
            get { return _buffer; }
        }

        public override void write(int i)
        {
            _buffer.Append(_encoding.GetChars(new []{(byte)i}));
        }

        //The following two methods aren't currently used by org.basex.io.PrintOutput
        public override void write(byte[] b, int off, int len)
        {
            throw new NotImplementedException();
        }

        public override void write(byte[] b)
        {
            throw new NotImplementedException();
        }
    }
}
