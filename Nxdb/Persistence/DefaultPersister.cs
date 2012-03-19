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
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Nxdb.Node;
using Nxdb.Persistence.Attributes;

namespace Nxdb.Persistence
{
    internal class DefaultPersister : Persister
    {
        internal override void Fetch(Element element, object target, TypeCache typeCache, Cache cache)
        {
            if (target == null) return;

            // Get all values first so if something goes wrong we haven't started modifying the object
            List<KeyValuePair<PersistentMemberInfo, object>> values
                = new List<KeyValuePair<PersistentMemberInfo, object>>();
            foreach (PersistentMemberInfo memberInfo
                in typeCache.PersistentMemberInfo.Where(m => m.Attribute.Fetch))
            {
                object member = memberInfo.GetValue(target);
                TypeCache memberTypeCache = cache.GetTypeCache(memberInfo.Type);
                object value = memberInfo.Attribute.FetchValue(element, member, memberTypeCache, cache);
                if (memberInfo.Attribute.Required && value == null) throw new Exception("Could not get required member.");
                values.Add(new KeyValuePair<PersistentMemberInfo, object>(memberInfo, value));
            }

            // Now that all conversions have been succesfully performed, set the values
            foreach (KeyValuePair<PersistentMemberInfo, object> value in values)
            {
                value.Key.SetValue(target, value.Value);
            }

            // Call any custom logic if implemented
            ICustomFetch custom = target as ICustomFetch;
            if(custom != null)
            {
                custom.Fetch(element);
            }
        }

        private class SerializedValue
        {
            public PersistentMemberAttribute PersistentMemberAttribute { get; private set; }
            public object Serialized { get; private set;}
            public object Member { get; private set; }
            public TypeCache MemberTypeCache { get; private set; }

            public SerializedValue(PersistentMemberAttribute persistentMemberAttribute,
                object serialized, object member, TypeCache memberTypeCache)
            {
                PersistentMemberAttribute = persistentMemberAttribute;
                Serialized = serialized;
                Member = member;
                MemberTypeCache = memberTypeCache;
            }
        }

        internal override object Serialize(object source, TypeCache typeCache, Cache cache)
        {
            if (source == null) return null; 
            
            List<SerializedValue> values = new List<SerializedValue>();

            // Do custom serialization
            ICustomStore custom = source as ICustomStore;
            if(custom != null)
            {
                values.Add(new SerializedValue(null, custom.Serialize(), null, null));
            }

            // Serialize the members
            foreach (PersistentMemberInfo memberInfo
                in typeCache.PersistentMemberInfo.Where(m => m.Attribute.Store))
            {
                object member = memberInfo.GetValue(source);
                TypeCache memberTypeCache = cache.GetTypeCache(memberInfo.Type);
                object serialized = memberInfo.Attribute.SerializeValue(member, memberTypeCache, cache);
                values.Add(new SerializedValue(memberInfo.Attribute, serialized, member, memberTypeCache));
            }
            return values;
        }

        // This scans over all instance fields in the object and uses a TypeConverter to convert them to string
        // If any fields cannot be converted an exception is thrown because the entire state was not stored
        internal override void Store(Element element, object serialized, object source, TypeCache typeCache, Cache cache)
        {
            if (source == null || serialized == null) return;

            // Custom store
            List<SerializedValue> values = (List<SerializedValue>) serialized;
            ICustomStore custom = source as ICustomStore;
            if (custom != null && values.Count > 0)
            {
                // The custom serialized value is always first
                custom.Store(element, values[0]);
                values.RemoveAt(0);
            }

            // Store the members
            foreach (SerializedValue value in values)
            {
                value.PersistentMemberAttribute.StoreValue(element, value.Serialized,
                    value.Member, value.MemberTypeCache, cache);
            }
        }
    }
}
