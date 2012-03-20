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
using System.Linq;
using System.Text;
using Nxdb.Persistence.Attributes;

namespace Nxdb.Persistence
{
    /// <summary>
    /// Provides a persistent IList that does not need to be tied to a persistent object.
    /// If a plain List is desired, the PersistentList class can be used instead.
    /// </summary>
    public class PersistentIList<TList, TItem>
        : PersistentICollection<TList, TItem>, IList<TItem>
        where TList : class, IList<TItem>, new()
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PersistentIList&lt;TList, TItem&gt;"/> class.
        /// </summary>
        /// <param name="attribute">Specifies the properties of the persistent List.</param>
        public PersistentIList(PersistentCollectionAttribute attribute) : base(attribute)
        {
        }

        public int IndexOf(TItem item)
        {
            return ((TList) Collection).IndexOf(item);
        }

        public void Insert(int index, TItem item)
        {
            ((TList) Collection).Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            ((TList) Collection).RemoveAt(index);
        }

        public TItem this[int index]
        {
            get { return ((TList) Collection)[index]; }
            set { ((TList) Collection)[index] = value; }
        }
    }
}
