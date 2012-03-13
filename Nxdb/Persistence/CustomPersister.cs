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
    /// Implement this interface to provide custom persistence logic on a per-object basis.
    /// </summary>
    public interface ICustomPersister
    {
        /// <summary>
        /// Refreshes the persistent object's state from the specified database element.
        /// </summary>
        /// <param name="element">The element the object is currently attached to.</param>
        void Fetch(Element element);

        /// <summary>
        /// Serializes this to an arbitrary object that contains the content to store.
        /// The return value will be passed to Store.
        /// </summary>
        object Serialize();

        /// <summary>
        /// Saves the serialized content to the specified database element.
        /// </summary>
        /// <param name="serialized">The arbitrary serialized content returned by Serialize.</param>
        /// <param name="element">The element the object is currently attached to.</param>
        void Store(Element element, object serialized);
    }

    internal class CustomPersister : Persister
    {
        internal override void Fetch(Element element, object target, TypeCache typeCache, Cache cache)
        {
            if (target == null) return;
            ((ICustomPersister)target).Fetch(element);
        }

        internal override object Serialize(object source, TypeCache typeCache, Cache cache)
        {
            if (source == null) return null;
            return ((ICustomPersister)source).Serialize();
        }

        internal override void Store(Element element, object serialized, object source, TypeCache typeCache, Cache cache)
        {
            if (source == null || serialized == null) return;
            ((ICustomPersister)source).Store(element, serialized);
        }
    }
}
