using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace Nxdb.Persistence
{
    public class PersistenceManager
    {
        private static PersistenceManager _default = null;

        /// <summary>
        /// Gets the default PersistenceManager. This is the
        /// instance that is used for the extension methods.
        /// </summary>
        public static PersistenceManager Default
        {
            get { return _default ?? (_default = new PersistenceManager()); }
        }

        private readonly Cache _cache;

        public PersistenceManager() : this(false)
        {
        }

        public PersistenceManager(bool autoRefresh)
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
        /// Gets an object of the specified type from the specified node
        /// and attaches it to the manager if specified.
        /// This requires the specified persistent object type to provide an accessible
        /// empty constructor. If one is not available, an exception will be thrown.
        /// </summary>
        /// <typeparam name="T">The type of persistent object to construct.</typeparam>
        /// <param name="node">The node from which to construct the persistent object.</param>
        /// <param name="attach">If set to <c>true</c> attaches the object if newly created,
        /// otherwise just fetches it.</param>
        /// <param name="searchCache">If set to <c>true</c> returns an existing instance if one
        /// is already attached to the same node, otherwise will always create a new instance
        /// and attach it regardless of if one is already attached.</param>
        /// <returns>
        /// The newly constructed persistent object.
        /// </returns>
        public T GetObject<T>(ContainerNode node, bool attach, bool searchCache) where T : class
        {
            if (node == null) throw new ArgumentNullException("node");
            object obj = _cache.GetObject(typeof(T), node, searchCache);
            if(attach)
            {
                Attach(obj, node);
            }
            else
            {
                Fetch(obj, node);
            }
            return (T)obj;
        }

        public T GetObject<T>(ContainerNode node) where T : class
        {
            return GetObject<T>(node, true, true);
        }

        /// <summary>
        /// Returns a collection of objects created by evaluating the specified query
        /// against the specified parent node and attaches it to the parent node if specified.
        /// This requires the specified object type to provide an accessible
        /// empty constructor. If one is not available, an exception will be thrown.
        /// </summary>
        /// <typeparam name="T">The type of objects to construct.</typeparam>
        /// <param name="parent">The node against which to evaluate the expression to get result
        /// nodes to be used for the construction of new objects.</param>
        /// <param name="expression">The expression to evaluate. Only ContainerNode results
        /// will be used (I.e., no text nodes, attributes nodes, etc. will be used).</param>
        /// <param name="attach">If set to <c>true</c> attaches the newly created
        /// collection, otherwise just populates it.</param>
        /// <param name="searchCache">If set to <c>true</c> returns an existing instance for
        /// each result node if one is already attached to the same node, otherwise will always
        /// create a new instance and attach it regardless of if one is already attached (a new
        /// collection is returned every time).</param>
        /// <param name="attachResults">If set to <c>true</c> attaches result objects, otherwise does
        /// not attach result objects.</param>
        /// <returns>
        /// A collection of persistent objects of the specified type. Though the return
        /// object can be explicitly refreshed and is also automatically refreshed (if enabled
        /// for this manager), explicitly saving or deleting it has no effect.
        /// </returns>
        public IEnumerable<T> GetObjects<T>(ContainerNode parent, string expression, bool attach, bool searchCache, bool attachResults) where T : class
        {
            PersistentCollection<T> collection = new PersistentCollection<T>(this, expression, searchCache, attachResults);
            Attach(collection, parent);
            return collection;
        }

        public IEnumerable<T> GetObjects<T>(ContainerNode parent, string expression) where T : class
        {
            return GetObjects<T>(parent, expression, true, true, true);
        }

        /// <summary>
        /// Attaches the specified object to the specified node. If the object is already
        /// attached to a different node, it will be detached from that node and reattached to the
        /// specified node. Also fetches data for the specified object from the specified node.
        /// </summary>
        /// <param name="obj">The persistent object.</param>
        /// <param name="node">The node to attach the existing object to.</param>
        public void Attach(object obj, ContainerNode node)
        {
            Attach(obj, node, true);
        }

        // Attaches the specific object and optionally fetches it (or stores it)
        private void Attach(object obj, ContainerNode node, bool fetch)
        {
            if (obj == null) throw new ArgumentNullException("obj");
            if (obj.GetType().IsValueType) throw new ArgumentException("obj must not be a value type.");
            if (node == null) throw new ArgumentNullException("node");
            if (!node.Valid) throw new ArgumentException("The specified node is invalid.");

            ObjectWrapper wrapper = _cache.Attach(obj, node);

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
        /// Appends the specified persistent object to the specified parent node by
        /// creating a new element with the same name as the object type and
        /// attaches it to the newly created element. If the object is already
        /// attached to a different node, it will be detached from that node
        /// and reattached to the newly created element.
        /// </summary>
        /// <param name="obj">The persistent object.</param>
        /// <param name="parent">The node to append the object to.</param>
        public void Append(object obj, ContainerNode parent)
        {
            Append(obj, parent, null);
        }

        /// <summary>
        /// Appends the specified persistent object to the specified parent node by
        /// creating a new element with specified element name and
        /// attaches it to the newly created element. If the object is already
        /// attached to a different node, it will be detached from that node
        /// and reattached to the newly created element.
        /// </summary>
        /// <param name="obj">The persistent object.</param>
        /// <param name="parent">The node to append the object to.</param>
        /// <param name="elementName">The name of the new element.</param>
        public void Append(object obj, ContainerNode parent, string elementName)
        {
            if (obj == null) throw new ArgumentNullException("obj");
            Type type = obj.GetType();
            if (type.IsValueType) throw new ArgumentException("obj must not be a value type.");
            if (parent == null) throw new ArgumentNullException("parent");
            if (!parent.Valid) throw new ArgumentException("The specified parent is invalid.");
            if (Updates.Open) throw new Exception("Can not append a new object with open updates.");

            // Get the new element name
            if(String.IsNullOrEmpty(elementName))
            {
                elementName = type.Name;
            }
            
            // Detach the persistent object first because the act of adding the new element will update
            // the database, thus causing the automatic refresh to change the object if it's already
            // attached and automatic refreshing is enabled
            Detach(obj);

            // Create the new element (this is why updates can't be open)
            parent.Append(new Element(elementName));
            ContainerNode node = (ContainerNode) parent.Children.Last();

            // Attach the persistent object to the new element
            Attach(obj, node, false);
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
        /// Fetches data for the specified object from the specified node.
        /// </summary>
        /// <param name="obj">The object to fetch data for.</param>
        /// <param name="node">The node to fetch data from.</param>
        public void Fetch(object obj, ContainerNode node)
        {
            if (obj == null) throw new ArgumentNullException("obj");
            Type type = obj.GetType();
            if (type.IsValueType) throw new ArgumentException("obj must not be a value type.");
            if (node == null) throw new ArgumentNullException("node");
            if (!node.Valid) throw new ArgumentException("The specified node is invalid.");

            TypeCache typeCache = _cache.GetTypeCache(type);
            typeCache.Behavior.Fetch(node, obj, typeCache);
        }

        /// <summary>
        /// Fetches data for the specified object from the attached node.
        /// </summary>
        /// <param name="obj">The object to fetch data for.</param>
        public void Fetch(object obj)
        {
            if (obj == null) throw new ArgumentNullException("obj");
            if (obj.GetType().IsValueType) throw new ArgumentException("obj must not be a value type.");

            ObjectWrapper wrapper;
            if(!_cache.TryGetWrapper(obj, out wrapper))
            {
                throw new ArgumentException("obj is not currently attached.");
            }

            wrapper.Fetch();
        }

        /// <summary>
        /// Fetches data for all attached objects from their attached nodes.
        /// </summary>
        public void FetchAll()
        {
            foreach(ObjectWrapper wrapper in _cache)
            {
                wrapper.Fetch();
            }
        }

        /// <summary>
        /// Stores data for the specified object to the specified node.
        /// </summary>
        /// <param name="obj">The object to store data for.</param>
        /// <param name="node">The node to store data to.</param>
        public void Store(object obj, ContainerNode node)
        {
            if (obj == null) throw new ArgumentNullException("obj");
            Type type = obj.GetType();
            if (type.IsValueType) throw new ArgumentException("obj must not be a value type.");
            if (node == null) throw new ArgumentNullException("node");
            if (!node.Valid) throw new ArgumentException("The specified node is invalid.");

            TypeCache typeCache = _cache.GetTypeCache(type);
            typeCache.Behavior.Store(node, obj, typeCache);
        }

        /// <summary>
        /// Stores data for the specified object to the attached node.
        /// </summary>
        /// <param name="obj">The object to store data for.</param>
        public void Store(object obj)
        {
            if (obj == null) throw new ArgumentNullException("obj");
            if (obj.GetType().IsValueType) throw new ArgumentException("obj must not be a value type.");

            ObjectWrapper wrapper;
            if (!_cache.TryGetWrapper(obj, out wrapper))
            {
                throw new ArgumentException("obj is not currently attached.");
            }

            wrapper.Store();
        }

        /// <summary>
        /// Stores data for all attached objects to their attached nodes.
        /// </summary>
        public void StoreAll()
        {
            foreach (ObjectWrapper wrapper in _cache)
            {
                wrapper.Store();
            }
        }
    }
}
