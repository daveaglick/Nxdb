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

using Nxdb.Node;

namespace Nxdb.Persistence
{
    /// <summary>
    /// This is the base class for reusable persisters. To add new persisters,
    /// create a derived class and override Fetch, Store, or both.
    /// </summary>
    public abstract class Persister
    {
        internal virtual void Fetch(Element element, object obj, TypeCache typeCache)
        {
            Fetch(element, obj);
        }

        internal virtual void Store(Element element, object obj, TypeCache typeCache)
        {
            Store(element, obj);
        }

        /// <summary>
        /// Fetches data for the specified object from the specified element.
        /// </summary>
        /// <param name="element">The element to fetch data from.</param>
        /// <param name="obj">The object to fetch data for.</param>
        public virtual void Fetch(Element element, object obj)
        {
        }

        /// <summary>
        /// Stores data for the specified object to the specified element.
        /// </summary>
        /// <param name="element">The element to store data to.</param>
        /// <param name="obj">The object to store data for.</param>
        public virtual void Store(Element element, object obj)
        {
        }
    }
}
