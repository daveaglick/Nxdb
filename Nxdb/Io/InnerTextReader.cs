/*
 * Copyright 2012 WildCard, LLC
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 * 
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using org.basex.data;
using org.basex.query.item;

namespace Nxdb.Io
{
    internal class InnerTextReader : TextReader
    {
        private readonly IEnumerator<ANode> _textNodes;

        //The text buffer - when empty go to the next text node
        private readonly StringBuilder _buffer = new StringBuilder();

        public InnerTextReader(IEnumerator<ANode> textNodes)
        {
            _textNodes = textNodes;
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
    }
}
