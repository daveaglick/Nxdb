using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nxdb.Persistence
{
    /// <summary>
    /// Implements an enumerable type for top-level persistent collections.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class PersistentCollection<T> : ICustomPersistentObject, IEnumerable<T> where T : class
    {
        private readonly PersistenceManager _manager;
        private readonly string _expression;
        private readonly bool _searchCache;
        private readonly bool _attach;
        private Dictionary<ContainerNode, T> _nodeCache = new Dictionary<ContainerNode, T>(); 
        private List<T> _persistentObjects = new List<T>(); 

        public PersistentCollection(PersistenceManager manager, string expression, bool searchCache, bool attach)
        {
            _manager = manager;
            _expression = expression;
            _searchCache = searchCache;
            _attach = attach;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _persistentObjects.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Fetch(ContainerNode node)
        {
            Dictionary<ContainerNode, T> nodeCache = new Dictionary<ContainerNode, T>(); 
            List<T> persistentObjects = new List<T>(); 

            foreach (ContainerNode result in node.Eval<ContainerNode>(_expression))
            {
                T persistentObject;
                if (!_nodeCache.TryGetValue(result, out persistentObject))
                {
                    persistentObject = _manager.GetObject<T>(result, _searchCache, _attach);
                }
                nodeCache.Add(result, persistentObject);
                persistentObjects.Add(persistentObject);
            }

            _nodeCache = nodeCache;
            _persistentObjects = persistentObjects;
        }

        public void Store(ContainerNode node) { }
    }
}
