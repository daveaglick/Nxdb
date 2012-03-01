using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Nxdb.Persistence
{
    // Stores a cache for all attached objects by type as well as other type-specific data such as constructors
    // Also caches any persistence attributes once for the type since fetching those is (somewhat) expensive
    // TODO: The constructor and other truly static information gets reconstructed for each manager - figure out how to make type-only stuff truly static (static generics?)
    internal class TypeCache
    {
        private readonly Type _type;
        private readonly Dictionary<ContainerNode, HashSet<ObjectWrapper>> _nodeToWrappers
            = new Dictionary<ContainerNode, HashSet<ObjectWrapper>>();
        private ConstructorInfo _constructor = null;

        public TypeCache(Type type)
        {
            _type = type;
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

        // Returns the first object in the cache for the specified node (or null if none exists)
        // Also cleans the cache of disposed objects
        public object FindObject(ContainerNode node)
        {
            HashSet<ObjectWrapper> wrappers;
            if (_nodeToWrappers.TryGetValue(node, out wrappers))
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
            if(!_nodeToWrappers.TryGetValue(wrapper.Node, out wrappers))
            {
                wrappers = new HashSet<ObjectWrapper>();
                _nodeToWrappers.Add(wrapper.Node, wrappers);
            }
            wrappers.Add(wrapper);
        }

        public void Remove(ObjectWrapper wrapper)
        {
            HashSet<ObjectWrapper> wrappers;
            if (_nodeToWrappers.TryGetValue(wrapper.Node, out wrappers))
            {
                wrappers.Remove(wrapper);
                if(wrappers.Count == 0)
                {
                    _nodeToWrappers.Remove(wrapper.Node);
                }
            }
        }

        public void Clear()
        {
            _nodeToWrappers.Clear();
        }
    }
}
