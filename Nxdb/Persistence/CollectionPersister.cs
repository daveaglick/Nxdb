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
    internal class CollectionPersister : Persister
    {
        internal override void Fetch(Element element, object target, TypeCache typeCache, Cache cache)
        {
            if (target == null) return;
            ((PersistentCollection)target).Fetch(element, cache);
        }

        internal override object Serialize(object source, TypeCache typeCache, Cache cache)
        {
            if (source == null) return null;
            return ((PersistentCollection)source).Serialize(cache);
        }

        internal override void Store(Element element, object serialized, object source, TypeCache typeCache, Cache cache)
        {
            if (source == null || serialized == null) return;
            ((PersistentCollection)source).Store(element, serialized, cache);
        }
    }
}
