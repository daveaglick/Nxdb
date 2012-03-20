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
    /// Provides a persistent Dictionary that does not need to be tied to a persistent object.
    /// </summary>
    public class PersistentDictionary<TKey, TValue> : PersistentIDictionary<Dictionary<TKey, TValue>, TKey, TValue>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PersistentDictionary&lt;TKey, TValue&gt;"/> class.
        /// </summary>
        /// <param name="attribute">Specifies the properties of the persistent Dictionary.</param>
        public PersistentDictionary(PersistentKvpCollectionAttribute attribute) : base(attribute)
        {
        }
    }
}
