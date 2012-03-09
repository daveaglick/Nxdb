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
    /// create a derived class and override Fetch, Serialize, and/or Store.
    /// </summary>
    public abstract class Persister
    {
        internal virtual void Fetch(Element element, object target, TypeCache typeCache, Cache cache)
        {
            Fetch(element, target);
        }

        internal virtual object Serialize(object source, TypeCache typeCache, Cache cache)
        {
            return Serialize(source);
        }

        internal virtual void Store(Element element, object serialized, object source, TypeCache typeCache, Cache cache)
        {
            Store(element, serialized, source);
        }

        // This just does both steps in one operation
        internal void Store(Element element, object source, TypeCache typeCache, Cache cache)
        {
            Store(element, Serialize(source, typeCache, cache), source, typeCache, cache);
        }

        /// <summary>
        /// Fetches data for the specified object from the specified element.
        /// </summary>
        /// <param name="element">The element to fetch data from.</param>
        /// <param name="target">The object to fetch data for.</param>
        protected virtual void Fetch(Element element, object target)
        {
        }

        /// <summary>
        /// Serializes the instance to an arbitrary object that will be passed
        /// to Store.Doing storage in two phases allows failure without
        /// manipulating the database.
        /// </summary>
        /// <param name="source">The object to store data for.</param>
        /// <returns>An arbitrary object that will be passed to Store.</returns>
        protected virtual object Serialize(object source)
        {
            return null;
        }

        /// <summary>
        /// Stores data for the specified object to the specified element.
        /// </summary>
        /// <param name="element">The element to store data to.</param>
        /// <param name="serialized">The serialized object returned by Serialize.</param>
        /// <param name="source">The object to store data for.</param>
        protected virtual void Store(Element element, object serialized, object source)
        {
        }
    }
}
