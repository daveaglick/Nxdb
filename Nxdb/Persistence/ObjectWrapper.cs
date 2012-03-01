using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace Nxdb.Persistence
{
    internal class ObjectWrapper : IEquatable<ObjectWrapper>
    {
        private readonly Type _type;
        private readonly WeakReference _weakReference;
        private readonly int _hash;
        private readonly Cache _cache;
        private ContainerNode _node = null;

        // These are lazy initialized for performance
        private TypeCache _typeCache = null;
        private IPersistenceBehavior _behavior = null;   
        
        public ObjectWrapper(object obj, Cache cache)
        {
            _type = obj.GetType();
            _weakReference = new WeakReference(obj);
            _hash = RuntimeHelpers.GetHashCode(obj);    // Store the hash in case the object is collected (hashed should be immutable)
            _cache = cache;
        }

        public static IPersistenceBehavior GetBehavior(object obj)
        {
            if (obj is ICustomPersistentObject)
            {
                return new CustomBehavior();
            }

            if (obj is IPersistentObject)
            {
                return new ExplicitBehavior();
            }

            return new ImplicitBehavior();
        }

        private IPersistenceBehavior GetCachedBehavior(object obj)
        {
            return _behavior ?? (_behavior = GetBehavior(obj));
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

        public ContainerNode Node
        {
            get { return _node; } 
            set
            {
                if (_node == value) return;

                // Remove the old invalidated handler
                if(_node != null)
                {
                    _node.Invalidated -= NodeInvalidated;
                }

                // Add the new invalidated handler
                if(value != null)
                {
                    value.Invalidated += NodeInvalidated;
                }

                _node = value;
            }
        }

        private void NodeInvalidated(object sender, EventArgs e)
        {
            _node.Invalidated -= NodeInvalidated;
            _cache.Detach(this, false);
        }

        public void Fetch()
        {
            if(Node == null) return;
            object obj = _weakReference.Target;
            if(obj == null)
            {
                _cache.Detach(this, false);
                return;
            }
            GetCachedBehavior(obj).Fetch(Node, obj);
        }

        public void Store()
        {
            if (Node == null) return;
            object obj = _weakReference.Target;
            if (obj == null)
            {
                _cache.Detach(this, false);
                return;
            }
            GetCachedBehavior(obj).Store(Node, obj);
        }
    }
}
