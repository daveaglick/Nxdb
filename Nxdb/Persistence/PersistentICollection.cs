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
using Nxdb.Persistence.Attributes;

namespace Nxdb.Persistence
{
    /// <summary>
    /// Provides a persistent ICollection that does not need to be tied to a persistent object.
    /// If a plain List is desired, the PersistentList class can be used instead.
    /// </summary>
    public class PersistentICollection<TCollection, TItem>
        : PersistentCollection, ICollection<TItem>
        where TCollection : class, ICollection<TItem>, new()
    {
        private TCollection _collection = new TCollection();

        /// <summary>
        /// Initializes a new instance of the <see cref="PersistentICollection&lt;TCollection, TItem&gt;"/> class.
        /// </summary>
        /// <param name="attribute">Specifies the properties of the persistent collection.</param>
        public PersistentICollection(PersistentCollectionAttribute attribute) : base(attribute)
        {
        }

        internal override object Collection
        {
            get { return _collection; }
            set { _collection = (TCollection)value; }
        }

        public IEnumerator<TItem> GetEnumerator()
        {
            return _collection.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(TItem item)
        {
            _collection.Add(item);
        }

        public void Clear()
        {
            _collection.Clear();
        }

        public bool Contains(TItem item)
        {
            return _collection.Contains(item);
        }

        public void CopyTo(TItem[] array, int arrayIndex)
        {
            _collection.CopyTo(array, arrayIndex);
        }

        public bool Remove(TItem item)
        {
            return _collection.Remove(item);
        }

        public int Count
        {
            get { return _collection.Count; }
        }

        public bool IsReadOnly
        {
            get { return _collection.IsReadOnly; }
        }
    }
}
