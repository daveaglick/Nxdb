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
    /// <summary>
    /// Implements an enumerable type for top-level persistent collections.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class PersistentCollection<T> : ICustomPersister, IEnumerable<T> where T : class
    {
        private readonly Manager _manager;
        private readonly string _expression;
        private readonly bool _attachItems;
        private Dictionary<Element, T> _elementCache = new Dictionary<Element, T>(); 
        private List<T> _persistentObjects = new List<T>(); 

        public PersistentCollection(Manager manager, string expression, bool attachItems)
        {
            _manager = manager;
            _expression = expression;
            _attachItems = attachItems;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _persistentObjects.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Fetch(Element element)
        {
            Dictionary<Element, T> elementCache = new Dictionary<Element, T>(); 
            List<T> persistentObjects = new List<T>(); 

            foreach (Element result in element.Eval<Element>(_expression))
            {
                T persistentObject;
                if (!_elementCache.TryGetValue(result, out persistentObject))
                {
                    persistentObject = _manager.GetObject<T>(result, _attachItems);
                }
                elementCache.Add(result, persistentObject);
                persistentObjects.Add(persistentObject);
            }

            _elementCache = elementCache;
            _persistentObjects = persistentObjects;
        }

        public void Store(Element element) { }
    }
}
