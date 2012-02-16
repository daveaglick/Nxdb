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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using org.basex.query.item;
using org.basex.query.iter;

namespace Nxdb
{
    // Provides an enumerator for the BaseX Iter class
    internal class IterEnum : IEnumerator<object>, IEnumerable<object>
    {
        private Iter _iter;
        private Item _current = null;

        public IterEnum(Iter iter)
        {
            if (iter == null) throw new ArgumentNullException("iter");
            _iter = iter;
        }

        public void Dispose()
        {
            _iter = null;
            _current = null;
        }

        public void Reset()
        {
            if(_iter == null) throw new ObjectDisposedException("IterEnum");
            if(!_iter.reset())
            {
                throw new NotSupportedException("This iterator cannot be reset.");
            }
        }

        public bool MoveNext()
        {
            if (_iter == null) throw new ObjectDisposedException("IterEnum");
            _current = _iter.next();
            return _current != null;
        }

        public object Current
        {
            get
            {
                if (_iter == null) throw new ObjectDisposedException("IterEnum");
                return _current.ToObject();
            }
        }

        object IEnumerator.Current
        {
            get { return Current; }
        }

        public IEnumerator<object> GetEnumerator()
        {
            return this;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
