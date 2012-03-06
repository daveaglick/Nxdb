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
            = new Dictionary<ObjectWrapper, ObjectWrapper>();
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
                typeCache = new TypeCache(type);
                _typeCaches.Add(type, typeCache);
            }
            return typeCache;
        }

        // Gets or constructs an object of the specified type
        public object GetObject(Type type, Element element, bool searchCache)
        {
            Clean();
            TypeCache typeCache = GetTypeCache(type);
            object obj = null;
            if(searchCache)
            {
                obj = typeCache.FindObject(element);
            }
            return obj ?? typeCache.CreateInstance();
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

            // Remove it first (no effect if not present)
            ObjectWrapper wrapper = Detach(new ObjectWrapper(obj, this), true);

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

            Detach(new ObjectWrapper(obj, this), true);
        }

        // Returns the detached wrapper or the one that was passed in if none was found
        // Set search == false only if the passed-in wrapper is from the cache already
        public ObjectWrapper Detach(ObjectWrapper wrapper, bool search)
        {
            // Get the existing wrapper
            if (search)
            {
                ObjectWrapper found;
                if (!_wrappers.TryGetValue(wrapper, out found)) return wrapper;
                wrapper = found;
            }

            // Remove it
            RemoveDatabaseUpdatedHandler(wrapper.Element);
            _wrappers.Remove(wrapper);
            wrapper.TypeCache.Remove(wrapper);
            wrapper.Element = null;
            return wrapper;
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
                    Detach(wrapper, false);
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
