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
using System.Reflection;
using System.Text;
using Nxdb.Node;
using Nxdb.Persistence.Attributes;

namespace Nxdb.Persistence
{
    // Stores a cache for all attached objects by type as well as other type-specific data such as constructors
    // Also caches any persistence attributes once for the type since fetching those is (somewhat) expensive
    // TODO: The constructor and other truly static information gets reconstructed for each manager - figure out how to make type-only stuff truly static (static generics?)
    internal class TypeCache
    {
        private readonly Type _type;
        private readonly Cache _cache;

        private readonly Dictionary<Element, HashSet<ObjectWrapper>> _elementToWrappers
            = new Dictionary<Element, HashSet<ObjectWrapper>>();

        // These are lazy initialized for performance
        private ConstructorInfo _constructor = null;
        private List<FieldInfo> _fields = null;
        private List<PropertyInfo> _properties = null;
        private Persister _persister = null;
        private PersisterAttribute _persisterAttribute = null;
        private List<KeyValuePair<MemberInfo, PersistentMemberAttribute>> _persistentMembers;

        public TypeCache(Type type, Cache cache)
        {
            _type = type;
            _cache = cache;
        }

        public Type Type
        {
            get { return _type; }
        }

        public object CreateInstance()
        {
            // Get the constructor if we don't already have one
            if (_constructor == null)
            {
                _constructor = _type.GetConstructor(
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                    null, Type.EmptyTypes, null);
                if (_constructor == null)
                {
                    throw new Exception("No empty constructor available for persistent object type " + _type.Name);
                }
            }

            // Construct the new object
            return _constructor.Invoke(new object[0]);
        }

        // Gets all fields up the hierarchy
        public IEnumerable<FieldInfo> Fields
        {
            get
            {
                if (_fields == null)
                {
                    _fields = new List<FieldInfo>();
                    Type type = _type;
                    while (type != null)
                    {
                        _fields.AddRange(type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance));
                        type = type.BaseType;
                    }
                }
                return _fields;
            }
        }

        // Gets all properties up the hierarchy
        public IEnumerable<PropertyInfo> Properties
        {
            get
            {
                if (_properties == null)
                {
                    _properties = new List<PropertyInfo>();
                    Type type = _type;
                    while (type != null)
                    {
                        _properties.AddRange(type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance));
                        type = type.BaseType;
                    }
                }
                return _properties;
            }
        }

        public Persister Persister
        {
            get { return _persister ?? (_persister = PersisterAttribute.Persister); }
        }

        public PersisterAttribute PersisterAttribute
        {
            get
            {
                if(_persisterAttribute == null)
                {
                    object[] attributes = _type.GetCustomAttributes(typeof(PersisterAttribute), false);
                    if(attributes.Length > 0)
                    {
                        _persisterAttribute = attributes[0] as PersisterAttribute;
                    }
                    if(_persisterAttribute == null)
                    {
                        _persisterAttribute = new DefaultPersisterAttribute();
                    }
                }
                return _persisterAttribute;
            }
        }

        public IEnumerable<KeyValuePair<MemberInfo, PersistentMemberAttribute>> PersistentMembers
        {
            get
            {
                if (_persistentMembers == null)
                {
                    _persistentMembers = new List<KeyValuePair<MemberInfo, PersistentMemberAttribute>>();

                    // Fields
                    foreach (FieldInfo fieldInfo in Fields)
                    {
                        CheckMemberForPersistentAttribute(fieldInfo, _persistentMembers);
                    }

                    // Properties
                    foreach (PropertyInfo propertyInfo in Properties)
                    {
                        CheckMemberForPersistentAttribute(propertyInfo, _persistentMembers);
                    }

                    // Sort by order
                    _persistentMembers.Sort((a, b) => a.Value.Order.CompareTo(b.Value.Order));
                }
                return _persistentMembers;
            }
        }

        private void CheckMemberForPersistentAttribute(MemberInfo memberInfo,
            List<KeyValuePair<MemberInfo, PersistentMemberAttribute>> persistentMembers)
        {
            object[] attributes = memberInfo.GetCustomAttributes(typeof (PersistentMemberAttribute), true);
            if(attributes.Length > 0)
            {
                if(attributes.Length != 1) throw new Exception("Only one PersistentAttribute can be used per field or property.");
                PersistentMemberAttribute persistentAttribute = (PersistentMemberAttribute) attributes[0];
                persistentAttribute.Inititalize(memberInfo, _cache);
                persistentMembers.Add(new KeyValuePair<MemberInfo, PersistentMemberAttribute>(memberInfo, persistentAttribute));
            }
        }

        // Returns the first object in the cache for the specified element (or null if none exists)
        // Also cleans the cache of disposed objects
        public object FindObject(Element element)
        {
            HashSet<ObjectWrapper> wrappers;
            if (_elementToWrappers.TryGetValue(element, out wrappers))
            {
                // Try to get an active instance
                foreach (ObjectWrapper wrapper in wrappers)
                {
                    object obj = wrapper.Object;
                    if (obj != null)
                    {
                        return obj;
                    }
                }
            }
            return null;
        }

        public void Add(ObjectWrapper wrapper)
        {
            HashSet<ObjectWrapper> wrappers;
            if(!_elementToWrappers.TryGetValue(wrapper.Element, out wrappers))
            {
                wrappers = new HashSet<ObjectWrapper>();
                _elementToWrappers.Add(wrapper.Element, wrappers);
            }
            wrappers.Add(wrapper);
        }

        public void Remove(ObjectWrapper wrapper)
        {
            HashSet<ObjectWrapper> wrappers;
            if (_elementToWrappers.TryGetValue(wrapper.Element, out wrappers))
            {
                wrappers.Remove(wrapper);
                if(wrappers.Count == 0)
                {
                    _elementToWrappers.Remove(wrapper.Element);
                }
            }
        }

        public void Clear()
        {
            _elementToWrappers.Clear();
        }
    }
}
