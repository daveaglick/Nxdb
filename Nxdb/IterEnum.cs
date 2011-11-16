using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using org.basex.query.item;
using org.basex.query.iter;

namespace Nxdb
{
    //Provides an enumerator for the BaseX Iter class
    internal class IterEnum : IEnumerator<object>, IEnumerable<object>
    {
        private readonly NxDatabase _database;
        private Iter _iter;
        private Item _current = null;

        public IterEnum(NxDatabase database, Iter iter)
        {
            _database = database;
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
                return _database.GetObjectForItem(_current);
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
