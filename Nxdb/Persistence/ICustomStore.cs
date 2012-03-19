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
using Nxdb.Node;

namespace Nxdb.Persistence
{
    /// <summary>
    /// Implement this interface to provide custom persistence store logic.
    /// The custom persistence methods will be called after any persistence attributes
    /// are processed.
    /// </summary>
    public interface ICustomStore
    {
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
}
