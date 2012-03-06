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
using System.Runtime.CompilerServices;
using System.Text;
using Nxdb.Node;

namespace Nxdb.Persistence
{
    internal class ObjectWrapper : IEquatable<ObjectWrapper>
    {
        private readonly Type _type;
        private readonly WeakReference _weakReference;
        private readonly int _hash;
        private readonly Cache _cache;
        private Element _element = null;

        // These are lazy initialized for performance
        private TypeCache _typeCache = null;
        private PersistenceBehavior _behavior = null;   
        
        public ObjectWrapper(object obj, Cache cache)
        {
            _type = obj.GetType();
            _weakReference = new WeakReference(obj);
            _hash = RuntimeHelpers.GetHashCode(obj);    // Store the hash in case the object is collected (hashed should be immutable)
            _cache = cache;
        }

        // Directly specifies a behavior to use
        // TODO: Implement overloads in the PersistenceManager that allows passing an override behavior in
        public ObjectWrapper(object obj, Cache cache, PersistenceBehavior behavior)
            : this(obj, cache)
        {
            _behavior = behavior;
        }

        private PersistenceBehavior Behavior
        {
            get { return _behavior ?? (_behavior = TypeCache.Behavior); }
        }

        public object Object
        {
            get { return _weakReference.Target; }
        }

        public TypeCache TypeCache
        {
            get { return _typeCache ?? (_typeCache = _cache.GetTypeCache(_type)); }
        }
        
        public override int GetHashCode()
        {
            return _hash;
        }

        public bool Equals(ObjectWrapper other)
        {
            object a = _weakReference.Target;
            object b = _weakReference.Target;
            return ReferenceEquals(a, b);
        }

        public override bool Equals(object obj)
        {
            ObjectWrapper other = obj as ObjectWrapper;
            return other != null && Equals(other);
        }

        public Element Element
        {
            get { return _element; } 
            set
            {
                if (_element == value) return;

                // Remove the old invalidated handler
                if(_element != null)
                {
                    _element.Invalidated -= ElementInvalidated;
                }

                // Add the new invalidated handler
                if(value != null)
                {
                    value.Invalidated += ElementInvalidated;
                }

                _element = value;
            }
        }

        private void ElementInvalidated(object sender, EventArgs e)
        {
            _element.Invalidated -= ElementInvalidated;
            _cache.Detach(this, false);
        }

        public void Fetch()
        {
            if(Element == null) return;
            object obj = _weakReference.Target;
            if(obj == null)
            {
                _cache.Detach(this, false);
                return;
            }
            Behavior.Fetch(Element, obj, TypeCache);
        }

        public void Store()
        {
            if (Element == null) return;
            object obj = _weakReference.Target;
            if (obj == null)
            {
                _cache.Detach(this, false);
                return;
            }
            Behavior.Store(Element, obj, TypeCache);
        }
    }
}
