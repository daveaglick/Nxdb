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

namespace Nxdb.Persistence
{
    // Stores a cache for all attached objects by type as well as other type-specific data such as constructors
    // Also caches any persistence attributes once for the type since fetching those is (somewhat) expensive
    // TODO: The constructor and other truly static information gets reconstructed for each manager - figure out how to make type-only stuff truly static (static generics?)
    internal class TypeCache
    {
        private readonly Type _type;
        private readonly Dictionary<Element, HashSet<ObjectWrapper>> _elementToWrappers
            = new Dictionary<Element, HashSet<ObjectWrapper>>();

        // These are lazy initialized for performance
        private ConstructorInfo _constructor = null;
        private List<FieldInfo> _fields = null;
        private PersistenceBehavior _behavior = null;
        private PersistenceAttribute _persistenceAttribute = null;

        public TypeCache(Type type)
        {
            _type = type;
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

        public PersistenceBehavior Behavior
        {
            get
            {
                if(_behavior == null)
                {
                    _behavior = typeof(ICustomPersistence).IsAssignableFrom(_type)
                        ? new CustomBehavior() : PersistenceAttribute.Behavior;
                }
                return _behavior;
            }
        }

        public PersistenceAttribute PersistenceAttribute
        {
            get
            {
                if(_persistenceAttribute == null)
                {
                    object[] attributes = _type.GetCustomAttributes(typeof(PersistenceAttribute), false);
                    if(attributes.Length > 0)
                    {
                        _persistenceAttribute = attributes[0] as PersistenceAttribute;
                    }
                    if(_persistenceAttribute == null)
                    {
                        _persistenceAttribute = new DefaultPersistenceAttribute();
                    }
                }
                return _persistenceAttribute;
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
