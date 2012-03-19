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
using System.Reflection;
using System.Text;
using Nxdb.Node;
using Nxdb.Persistence.Attributes;

namespace Nxdb.Persistence
{
    /// <summary>
    /// Base class for collections of persistent data or objects.
    /// </summary>
    public abstract class PersistentCollection
    {
        private readonly PersistentMemberAttribute _attribute;
        private Type _type = null;

        protected PersistentCollection(PersistentMemberAttribute attribute)
        {
            _attribute = attribute;
        }

        internal abstract object Collection { get; set; }

        // Need to lazily initialize the attribute since we don't have an object instance when
        // the CollectionPersister is created for the overall type
        internal void Initialize(Cache cache)
        {
            if (_type != null) return;
            _type = Collection.GetType();
            _attribute.Inititalize(_type, "Collection", cache);
        }

        internal void Fetch(Element element, Cache cache)
        {
            Initialize(cache);
            Collection = _attribute.FetchValue(element, Collection, cache.GetTypeCache(_type), cache);
        }

        internal object Serialize(Cache cache)
        {
            Initialize(cache);
            return _attribute.SerializeValue(Collection, cache.GetTypeCache(_type), cache);
        }

        internal void Store(Element element, object serialized, Cache cache)
        {
            Initialize(cache);
            _attribute.StoreValue(element, serialized, Collection, cache.GetTypeCache(_type), cache);
        }
    }
}
