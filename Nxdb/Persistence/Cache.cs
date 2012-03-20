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
using System.Text;
using Nxdb.Node;

namespace Nxdb.Persistence
{
    internal class Cache : IEnumerable<ObjectWrapper>
    {
        private readonly Dictionary<ObjectWrapper, ObjectWrapper> _wrappers
            = new Dictionary<ObjectWrapper, ObjectWrapper>();   // ObjectWrapper compares target / keyed by target
        private readonly Dictionary<Type, TypeCache> _typeCaches
            = new Dictionary<Type, TypeCache>();
        private readonly Dictionary<Database, int> _databases = new Dictionary<Database, int>();
        private bool _autoRefresh = false;

        public Cache(bool autoRefresh)
        {
            _autoRefresh = autoRefresh;
        }

        // Gets or creates a type cache for the given type
        public TypeCache GetTypeCache(Type type)
        {
            TypeCache typeCache;
            if (!_typeCaches.TryGetValue(type, out typeCache))
            {
                typeCache = new TypeCache(type, this);
                _typeCaches.Add(type, typeCache);
            }
            return typeCache;
        }

        // Gets or constructs an object of the specified type and attaches or fetches it
        public object GetObject(Type type, Element element, bool attach)
        {
            return GetObject(GetTypeCache(type), element, attach);
        }

        public object GetObject(TypeCache typeCache, Element element, bool attach)
        {
            Clean();
            object obj = null;
            
            // Search for an existing instance if requested
            if (attach)
            {
                obj = typeCache.FindObject(element);
            }

            // If we didn't find an existing instance, create one
            if(obj == null)
            {
                // Create an instance
                obj = typeCache.CreateInstance();

                // Attach or fetch the new instance
                ObjectWrapper wrapper = null;
                if(obj != null)
                {
                    if (attach)
                    {
                        // Use the ObjectWrapper.Fetch() to take advantage of last update time caching
                        wrapper = Attach(obj, element);
                        wrapper.Fetch();
                    }
                    else
                    {
                        typeCache.Persister.Fetch(element, obj, typeCache, this);
                    }
                }

                // Call it's initialize method if implemented
                ICustomInitialize custom = obj as ICustomInitialize;
                if (custom != null)
                {
                    try
                    {   
                        custom.Initialize(element);
                    }
                    catch (Exception)
                    {
                        if(wrapper != null) Detach(wrapper);
                        obj = null;
                    }
                }
            }
            return obj;
        }

        public bool TryGetWrapper(object obj, out ObjectWrapper wrapper)
        {
            Clean();
            return _wrappers.TryGetValue(new ObjectWrapper(obj, this), out wrapper);
        }

        public IEnumerator<ObjectWrapper> GetEnumerator()
        {
            List<ObjectWrapper> wrappers = new List<ObjectWrapper>(_wrappers.Values);
            return wrappers.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public ObjectWrapper Attach(object obj, Element element)
        {
            Clean();

            // Check if it's already attached to the requested element
            // and detach it if it's in the cache but attached to a different element
            ObjectWrapper wrapper = new ObjectWrapper(obj, this);
            ObjectWrapper existing;
            if(_wrappers.TryGetValue(wrapper, out existing))
            {
                if(existing.Element.Equals(element))
                {
                    return existing;
                }
                wrapper = existing;
                Detach(wrapper);
            }

            // Add it
            wrapper.Element = element;
            AddDatabaseUpdatedHandler(element);
            _wrappers.Add(wrapper, wrapper);
            wrapper.TypeCache.Add(wrapper);

            return wrapper;
        }

        public void Detach(object obj)
        {
            Clean();
            ObjectWrapper wrapper = new ObjectWrapper(obj, this);
            ObjectWrapper existing;
            if (_wrappers.TryGetValue(wrapper, out existing))
            {
                Detach(existing);
            }
        }

        // Detaches a wrapper (must already be in the cache)
        public void Detach(ObjectWrapper wrapper)
        {
            if (wrapper.Element == null) return;    // Sanity check - if the wrapper has been removed already it won't have an Element
            RemoveDatabaseUpdatedHandler(wrapper.Element);
            _wrappers.Remove(wrapper);
            wrapper.TypeCache.Remove(wrapper);
            wrapper.Element = null;
        }

        public void DetachAll()
        {
            // Remove all database events
            foreach(Database database in _databases.Keys)
            {
                database.Updated -= DatabaseUpdated;
            }
            _databases.Clear();

            // Remove all element mappings
            foreach(ObjectWrapper wrapper in _wrappers.Values)
            {
                wrapper.Element = null;    // Removes the invalidated handler
            }
            _wrappers.Clear();

            // Remove all type cache entries
            foreach(TypeCache typeCache in _typeCaches.Values)
            {
                typeCache.Clear();
            }
        }

        // Periodically cleans out the caches to remove collected weak references
        private void Clean()
        {
            if (_wrappers.Count % 100 != 0) return;
            foreach(ObjectWrapper wrapper in _wrappers.Values)
            {
                object obj = wrapper.Object;
                if(obj == null)
                {
                    Detach(wrapper);
                }
            }
        }

        public bool AutoRefresh
        {
            get { return _autoRefresh; }
            set
            {
                if (value == _autoRefresh) return;
                _autoRefresh = value;
                if (value)
                {
                    foreach (ObjectWrapper wrapper in _wrappers.Values)
                    {
                        AddDatabaseUpdatedHandler(wrapper.Element);
                    }
                }
                else
                {
                    foreach (Database database in _databases.Keys)
                    {
                        database.Updated -= DatabaseUpdated;
                    }
                    _databases.Clear();
                }
            }
        }

        private void RemoveDatabaseUpdatedHandler(Element element)
        {
            int count;
            if (_databases.TryGetValue(element.Database, out count))
            {
                count--;
                if (count == 0)
                {
                    element.Database.Updated -= DatabaseUpdated;
                    _databases.Remove(element.Database);
                }
                else
                {
                    _databases[element.Database] = count;
                }
            }
        }

        private void AddDatabaseUpdatedHandler(Element element)
        {
            if (AutoRefresh)
            {
                int count;
                if (_databases.TryGetValue(element.Database, out count))
                {
                    count++;
                }
                else
                {
                    count = 1;
                    element.Database.Updated += DatabaseUpdated;
                }
                _databases[element.Database] = count;
            }
        }

        private void DatabaseUpdated(object sender, EventArgs e)
        {
            Clean();

            foreach(ObjectWrapper wrapper
                in _wrappers.Values.Where(w => w.Element.Database == (Database)sender))
            {
                wrapper.Fetch();
            }
        }
    }
}
