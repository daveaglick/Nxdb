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
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using Nxdb.Node;

namespace Nxdb.Persistence.Attributes
{
    /// <summary>
    /// Stores or fetches a collection to/from a child element of the container element.
    /// If more than one element with the given name exists, the first one will be used.
    /// This supports arrays, lists, etc. The persistent object must either be an array,
    /// implement ICollection&lt;T&gt;, or implement IList and provide an ItemType.
    /// By default this stores and fetches data in the following format:
    /// <code>
    /// <Container>
    ///   <Name>
    ///     <Item>...</Item>
    ///     <Item>...</Item>
    ///     <Item>...</Item>
    ///   </Name>
    /// </Container>
    /// </code> 
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class PersistentCollectionAttribute : PersistentMemberAttribute
    {
        /// <summary>
        /// Gets or sets the element name to use or create. If unspecified, the name of
        /// the field or property will be used (as converted to a valid XML name).
        /// This is exclusive with Query and both may not be specified.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the element name to use or create for array items. If unspecified, "Item" will be used.
        /// This is exclusive with ItemQuery and both may not be specified.
        /// </summary>
        public string ItemName { get; set; }

        /// <summary>
        /// Gets or sets the query to use for getting an item. This is evaluated from the context of the parent
        /// element. If an item query is used, the array is read only and will not be persisted to the database.
        /// This is exclusive with ItemName and both may not be specified.
        /// </summary>
        public string ItemQuery { get; set; }

        /// <summary>
        /// Gets or sets the type of items. If unspecified, the actual type of the items will be used. If specified,
        /// this must be assignable to the type of the array.
        /// </summary>
        public Type ItemType { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the items are persistent object types.
        /// </summary>
        public bool ItemsArePersistentObjects { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether items should be attached.
        /// Only meaningful if ItemsArePersistentObjects is true.
        /// If true (default), the manager cache will be searched and an
        /// existing instance used or a new instance created and attached for each item. If false, a new
        /// detached instance will be created on every fetch for each item.
        /// </summary>
        public bool AttachItems { get; set; }

        private TypeCache _itemTypeCache = null;
        private Func<int, object> _getCollection = null;
        private Action<object, int, object> _setCollectionItem = null; // (collection, index, value)

        /// <summary>
        /// Initializes a new instance of the <see cref="PersistentCollectionAttribute"/> class.
        /// </summary>
        public PersistentCollectionAttribute()
        {
            AttachItems = true;
        }

        internal override void Inititalize(MemberInfo memberInfo, Cache cache)
        {
            base.Inititalize(memberInfo, cache);

            Name = GetName(Name, memberInfo.Name, Query, CreateQuery);
            ItemName = GetName(ItemName, "Item", ItemQuery);

            // Resolve the type of collection and the item type
            Type memberType = DefaultPersister.GetMemberType(memberInfo);
            if (memberType.IsArray)
            {
                // It's an array, get the array item type
                Type itemType = memberType.GetElementType();
                if (itemType == null)
                {
                    throw new Exception("Could not determine array item type.");
                }
                if (ItemType == null)
                {
                    ItemType = itemType;
                }
                else if (!itemType.IsAssignableFrom(ItemType))
                {
                    throw new Exception("The specified ItemType must be assignable to the array type.");
                }
                _getCollection = s => Array.CreateInstance(ItemType, s);
                _setCollectionItem = (c, i, v) => ((Array)c).SetValue(v, (Int64)i);
            }
            else
            {
                // Not an array, check interfaces   
                List<Type> collectionInterfaces = new List<Type>(memberType.GetInterfaces()
                    .Where(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(ICollection<>)));
                if (collectionInterfaces.Count > 0)
                {
                    // The member implements ICollection<T>, verify the ItemType (if specified) is compatable
                    Type collectionType = null;
                    foreach (Type collectionInterface in collectionInterfaces)
                    {
                        Type itemType = collectionInterface.GetGenericArguments()[0];
                        if (ItemType == null || itemType.IsAssignableFrom(ItemType))
                        {
                            if(ItemType == null) ItemType = itemType;
                            collectionType = collectionInterface;
                            break;
                        }
                    }

                    // Make sure we got an item type
                    if (collectionType == null) throw new Exception("No appropriate item type could be found.");

                    // Get the constructor and add functions
                    ConstructorInfo constructor = memberType.GetConstructor(Type.EmptyTypes);
                    if (constructor == null) throw new Exception("Persistent collection member must implement an empty constructor.");
                    _getCollection = s => constructor.Invoke(null);
                    MethodInfo addMethod = collectionType.GetMethod("Add");
                    _setCollectionItem = (c, i, v) => addMethod.Invoke(c, new[] {v});
                }
                else if (typeof(IList).IsAssignableFrom(memberType))
                {
                    // The member implements IList
                    if (ItemType == null) throw new Exception("A persistent collection that implements IList must provide an ItemType.");
                    ConstructorInfo constructor = memberType.GetConstructor(Type.EmptyTypes);
                    if (constructor == null) throw new Exception("Persistent collection member must implement an empty constructor.");
                    _getCollection = s => constructor.Invoke(null);
                    _setCollectionItem = (c, i, v) => ((IList) c).Add(v);
                }
                else
                {
                    throw new Exception("Persistent collection member must be an array, implement ICollection<T>, or implement IList.");
                }
            }

            _itemTypeCache = cache.GetTypeCache(ItemType);
        }

        internal override object FetchValue(Element element, object target, TypeCache typeCache, Cache cache)
        {
            // Get the primary element
            Element child;
            if (!GetNodeFromQuery(Query, null, element, out child))
            {
                child = element.Children.OfType<Element>().Where(e => e.Name.Equals(Name)).FirstOrDefault();
            }

            if (child == null)
            {
                return null;
            }

            // Get all the item nodes
            IList<object> items = new List<object>(!String.IsNullOrEmpty(ItemQuery) ? child.Eval(ItemQuery)
                : child.Children.OfType<Element>().Where(e => e.Name.Equals(ItemName)).Cast<object>());

            // Create the collection
            object collection = _getCollection(items.Count);

            // Populate with values
            int c = 0;
            foreach(object item in items)
            {
                if(ItemsArePersistentObjects)
                {
                    // Get, attach, and fetch the persistent object instance
                    Element itemElement = item as Element;
                    if (itemElement == null) throw new Exception("Persistent value node must be an element.");
                    object itemObject = cache.GetObject(_itemTypeCache, itemElement, AttachItems);
                    _setCollectionItem(collection, c++, itemObject);
                }
                else
                {
                    Node.Node itemNode = item as Node.Node;
                    object itemObject = GetObjectFromString(
                        itemNode != null ? itemNode.Value : item.ToString(), null, null, _itemTypeCache.Type);
                    _setCollectionItem(collection, c++, itemObject);
                }
            }

            return collection;
        }

        internal override object SerializeValue(object source, TypeCache typeCache, Cache cache)
        {
            if (!String.IsNullOrEmpty(ItemQuery)) return null;
            
            // Key = source object, Value = serialized data
            List<KeyValuePair<object, object>> values = new List<KeyValuePair<object, object>>();

            // Iterate over the source object
            if (source != null)
            {
                IEnumerable items = (IEnumerable) source;
                foreach (object item in items)
                {
                    object value = ItemsArePersistentObjects
                        ? _itemTypeCache.Persister.Serialize(item, _itemTypeCache, cache)
                        : GetStringFromObject(item, _itemTypeCache.Type);
                    values.Add(new KeyValuePair<object, object>(item, value));
                }
            }

            return values;
        }

        internal override void StoreValue(Element element, object serialized, object source, TypeCache typeCache, Cache cache)
        {
            if (!String.IsNullOrEmpty(ItemQuery)) return;

            // Get the child element
            Element child;
            if (!GetNodeFromQuery(Query, CreateQuery, element, out child))
            {
                child = element.Children.OfType<Element>().Where(e => e.Name.Equals(Name)).FirstOrDefault();
            }
            else if (child == null)
            {
                return;
            }

            // Create or remove the child if needed
            if (child == null && serialized != null)
            {
                element.Append(new Element(Name));
                child = (Element)element.Children.Last();
            }
            else if (child != null && serialized == null)
            {
                child.Remove();
                return;
            }
            else if (child == null)
            {
                return;
            }

            // Remove all elements with the item name
            List<Element> removeElements =
                child.Children.OfType<Element>()
                .Where(e => e.Name == ItemName).ToList();
            foreach (Element removeElement in removeElements)
            {
                removeElement.Remove();
            }

            // Store all items
            foreach (KeyValuePair<object, object> kvp in
                (IEnumerable<KeyValuePair<object, object>>) serialized)
            {
                child.Append(new Element(ItemName));
                Element itemElement = (Element) child.Children.Last();
                if (ItemsArePersistentObjects)
                {
                    _itemTypeCache.Persister.Store(itemElement, kvp.Value, kvp.Key, _itemTypeCache, cache);
                }
                else
                {
                    itemElement.Value = (string) kvp.Value;
                }
            }
        }
    }
}
