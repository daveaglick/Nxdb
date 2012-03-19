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
using System.Runtime.CompilerServices;
using System.Text;
using Nxdb.Node;

namespace Nxdb.Persistence
{
    /// <summary>
    /// The manager for the object persistence framework. More than one Manager can be
    /// created, each with different settings and object caches. If only one is going to
    /// be used, it is recommended to use the static Manager.Default manager.
    /// </summary>
    public class Manager
    {
        private static Manager _default = null;

        /// <summary>
        /// Gets the default PersistenceManager. This is the
        /// instance that is used for the extension methods.
        /// </summary>
        public static Manager Default
        {
            get { return _default ?? (_default = new Manager()); }
        }

        private readonly Cache _cache;

        public Manager() : this(false)
        {
        }

        public Manager(bool autoRefresh)
        {
            _cache = new Cache(autoRefresh);
        }

        /// <summary>
        /// Gets or sets a value indicating whether all attached persistent objects
        /// should be automatically refreshed when their database changes.
        /// </summary>
        /// <value>
        ///   <c>true</c> if all attached persistent objects
        /// should be automatically refreshed when their database
        /// changes; otherwise, <c>false</c>.
        /// </value>
        public bool AutoRefresh
        {
            get { return _cache.AutoRefresh; }
            set { _cache.AutoRefresh = value; }
        }

        /// <summary>
        /// Gets an object of the specified type from the specified element
        /// and attaches it to the manager if specified.
        /// This requires the specified persistent object type to provide an accessible
        /// empty constructor. If one is not available, an exception will be thrown.
        /// </summary>
        /// <param name="type">The type of object to get.</param>
        /// <param name="element">The element from which to construct the persistent object.</param>
        /// <param name="attach">If set to <c>true</c> attaches the object if newly created,
        /// otherwise just fetches it.</param>
        /// <returns>
        /// The newly constructed persistent object.
        /// </returns>
        public object GetObject(Type type, Element element, bool attach)
        {
            if (type.IsValueType) throw new ArgumentException("obj must not be a value type.");
            if (element == null) throw new ArgumentNullException("element");
            return _cache.GetObject(type, element, attach);
        }

        public object GetObject(Type type, Element element)
        {
            return GetObject(type, element, true);
        }

        public T GetObject<T>(Element element, bool attach) where T : class
        {
            return (T)GetObject(typeof(T), element, attach);
        }

        public T GetObject<T>(Element element) where T : class
        {
            return GetObject<T>(element, true);
        }
        
        /// <summary>
        /// Attaches the specified object to the specified element. If the object is already
        /// attached to a different element, it will be detached from that element and reattached to the
        /// specified element. Also fetches data for the specified object from the specified element.
        /// </summary>
        /// <param name="target">The persistent object.</param>
        /// <param name="element">The element to attach the existing object to.</param>
        public void Attach(object target, Element element)
        {
            Attach(target, element, true);
        }

        // Attaches the specific object and optionally fetches it (or stores it)
        private void Attach(object obj, Element element, bool fetch)
        {
            if (obj == null) throw new ArgumentNullException("obj");
            if (obj.GetType().IsValueType) throw new ArgumentException("obj must not be a value type.");
            if (element == null) throw new ArgumentNullException("element");
            if (!element.Valid) throw new ArgumentException("The specified element is invalid.");

            ObjectWrapper wrapper = _cache.Attach(obj, element);

            if(fetch)
            {
                wrapper.Fetch();
            }
            else
            {
                wrapper.Store();
            }
        }

        /// <summary>
        /// Appends the specified persistent object to the specified parent element by
        /// creating a new element with specified element name and
        /// attaches it to the newly created element. If the object is already
        /// attached to a different element, it will be detached from that element
        /// and reattached to the newly created element.
        /// </summary>
        /// <param name="source">The persistent object.</param>
        /// <param name="parent">The element to append the object to.</param>
        /// <param name="elementName">The name of the new element.</param>
        /// <param name="attach">Indicates whether the object should be attached.</param>
        public void Append(object source, Element parent, string elementName, bool attach)
        {
            if (source == null) throw new ArgumentNullException("source");
            Type type = source.GetType();
            if (type.IsValueType) throw new ArgumentException("obj must not be a value type.");
            if (parent == null) throw new ArgumentNullException("parent");
            if (!parent.Valid) throw new ArgumentException("The specified parent is invalid.");

            // Get the new element name
            if(String.IsNullOrEmpty(elementName))
            {
                elementName = type.Name;
            }
            
            // Detach the persistent object first because the act of adding the new element will update
            // the database, thus causing the automatic refresh to change the object if it's already
            // attached and automatic refreshing is enabled
            if (attach)
            {
                Detach(source);
            }

            // Create the new element
            parent.Append(new Element(elementName));
            Element element = (Element) parent.Children.Last();

            // Attach the persistent object to the new element
            if (attach)
            {
                Attach(source, element, false);
            }
            else
            {
                Store(source, element);
            }
        }

        public void Append(object source, Element parent)
        {
            Append(source, parent, null, true);
        }

        public void Append(object source, Element parent, string elementName)
        {
            Append(source, parent, elementName, true);
        }

        /// <summary>
        /// Detaches the specified object.
        /// </summary>
        /// <param name="obj">The object to detach.</param>
        public void Detach(object obj)
        {
            if (obj == null) throw new ArgumentNullException("obj");
            if (obj.GetType().IsValueType) throw new ArgumentException("obj must not be a value type.");
            _cache.Detach(obj);
        }

        /// <summary>
        /// Detaches all object from this manager.
        /// </summary>
        public void DetachAll()
        {
            _cache.DetachAll();
        }

        /// <summary>
        /// Fetches data for the specified object from the specified element.
        /// This method does not attach the object to the specified element.
        /// </summary>
        /// <param name="target">The object to fetch data for.</param>
        /// <param name="element">The element to fetch data from.</param>
        public void Fetch(object target, Element element)
        {
            if (target == null) throw new ArgumentNullException("target");
            Type type = target.GetType();
            if (type.IsValueType) throw new ArgumentException("obj must not be a value type.");
            if (element == null) throw new ArgumentNullException("element");
            if (!element.Valid) throw new ArgumentException("The specified element is invalid.");

            TypeCache typeCache = _cache.GetTypeCache(type);
            typeCache.Persister.Fetch(element, target, typeCache, _cache);
        }

        /// <summary>
        /// Fetches data for the specified object from the attached element.
        /// </summary>
        /// <param name="target">The object to fetch data for.</param>
        public void Fetch(object target)
        {
            if (target == null) throw new ArgumentNullException("target");
            if (target.GetType().IsValueType) throw new ArgumentException("obj must not be a value type.");

            ObjectWrapper wrapper;
            if(!_cache.TryGetWrapper(target, out wrapper))
            {
                throw new ArgumentException("obj is not currently attached.");
            }

            wrapper.Fetch();
        }

        /// <summary>
        /// Fetches data for all attached objects from their attached elements.
        /// </summary>
        public void FetchAll()
        {
            foreach(ObjectWrapper wrapper in _cache)
            {
                wrapper.Fetch();
            }
        }

        /// <summary>
        /// Stores data for the specified object to the specified element.
        /// This method does not attach the object to the specified element.
        /// </summary>
        /// <param name="source">The object to store data for.</param>
        /// <param name="element">The element to store data to.</param>
        public void Store(object source, Element element)
        {
            if (source == null) throw new ArgumentNullException("source");
            Type type = source.GetType();
            if (type.IsValueType) throw new ArgumentException("obj must not be a value type.");
            if (element == null) throw new ArgumentNullException("element");
            if (!element.Valid) throw new ArgumentException("The specified element is invalid.");

            TypeCache typeCache = _cache.GetTypeCache(type);
            typeCache.Persister.Store(element, source, typeCache, _cache);
        }

        /// <summary>
        /// Stores data for the specified object to the attached element.
        /// </summary>
        /// <param name="source">The object to store data for.</param>
        public void Store(object source)
        {
            if (source == null) throw new ArgumentNullException("source");
            if (source.GetType().IsValueType) throw new ArgumentException("obj must not be a value type.");

            ObjectWrapper wrapper;
            if (!_cache.TryGetWrapper(source, out wrapper))
            {
                throw new ArgumentException("obj is not currently attached.");
            }

            wrapper.Store();
        }
    }
}
