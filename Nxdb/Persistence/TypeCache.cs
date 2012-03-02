using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Nxdb.Persistence.Behaviors;

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

        // These are lazy initialized for performance
        private ConstructorInfo _constructor = null;
        private FieldInfo[] _fields = null;
        private PersistenceBehavior _behavior = null;
        private PersistentObjectAttribute _persistentObjectAttribute = null;

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

        public IEnumerable<FieldInfo> Fields
        {
            get { return _fields ?? (_fields = _type.GetFields(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)); }
        }

        public PersistenceBehavior Behavior
        {
            get
            {
                if(_behavior == null)
                {
                    // Does it implement custom behaviors?
                    if(typeof(ICustomPersistence).IsAssignableFrom(_type))
                    {
                        _behavior = new CustomBehavior();
                    }
                    else
                    {
                        // Does it declare a behavior type via the attribute
                        Type behaviorType = PersistentObjectAttribute.BehaviorType;
                        if(behaviorType != null && typeof(PersistenceBehavior).IsAssignableFrom(behaviorType))
                        {
                            // Create the behavior instance
                            ConstructorInfo ctor = behaviorType.GetConstructor(
                                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                                null, Type.EmptyTypes, null);
                            if(ctor != null)
                            {
                                _behavior = ctor.Invoke(new object[0]) as PersistenceBehavior;
                            }
                        }

                        // If we still don't have one, use the default
                        _behavior = new DefaultBehavior();
                    }
                }
                return _behavior;
            }
        }

        public PersistentObjectAttribute PersistentObjectAttribute
        {
            get
            {
                if(_persistentObjectAttribute == null)
                {
                    object[] attributes = _type.GetCustomAttributes(typeof(PersistentObjectAttribute), false);
                    if(attributes.Length > 0)
                    {
                        _persistentObjectAttribute = attributes[0] as PersistentObjectAttribute;
                    }
                    if(_persistentObjectAttribute == null)
                    {
                        _persistentObjectAttribute = new PersistentObjectAttribute();
                    }
                }
                return _persistentObjectAttribute;
            }
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
