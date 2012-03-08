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
        /// Saves the persistent object's state to the specified database element.
        /// </summary>
        /// <param name="element">The element the object is currently attached to.</param>
        void Store(Element element);
    }

    internal class CustomPersister : Persister
    {
        internal override void Fetch(Element element, object obj, TypeCache typeCache)
        {
            ((ICustomPersister)obj).Fetch(element);
        }

        internal override void Store(Element element, object obj, TypeCache typeCache)
        {
            ((ICustomPersister)obj).Store(element);
        }
    }
}
