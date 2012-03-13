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
using System.Text;
using Nxdb.Node;

namespace Nxdb.Persistence.Attributes
{
    /// <summary>
    /// Stores or fetches a nested persistent array (or list) to/from a child element of the container element.
    /// If more than one element with the given name exists, the first one will be used. This attribute can be applied
    /// to arrays and objects assignable by List&lt;ItemType&gt;. If the object is not an array and ItemType is not
    /// specified, all implemented IEnumerable&lt;T&gt; interfaces will be searched for the first that provides a
    /// type that results in an assignable List&lt;T&gt;.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class PersistentArrayAttribute : PersistentMemberAttribute
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

        private bool _array = false;
        private ConstructorInfo _listConstructor = null;

        public PersistentArrayAttribute()
        {
            AttachItems = true;
        }

        internal override void Inititalize(MemberInfo memberInfo)
        {
            base.Inititalize(memberInfo);

            Name = GetName(Name, memberInfo.Name, Query, CreateQuery);
            ItemName = GetName(ItemName, "Item", ItemQuery);

            // Resolve the type of collection and the item type
            Type memberType = DefaultPersister.GetMemberType(memberInfo);
            if(memberType.IsArray)
            {
                // Get the array item type
                _array = true;
                Type itemType = memberType.GetElementType();
                if(itemType == null)
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
                _listConstructor = typeof(List<>).MakeGenericType(ItemType).GetConstructor(Type.EmptyTypes);
            }
            else if(ItemType != null)
            {
                // See if a List<ItemType> can be assigned to the member
                Type listType = typeof(List<>).MakeGenericType(ItemType);
                if(!memberType.IsAssignableFrom(listType))
                    throw new Exception("The target object must be assignable from a List<ItemType>.");
                _listConstructor = listType.GetConstructor(Type.EmptyTypes);
            }
            else
            {
                // Get an appropriate item type
                Type listType = typeof(List<>);
                foreach(Type itemType in memberType.GetInterfaces()
                    .Where(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                    .Select(t => t.GetGenericArguments()[0]))
                {
                    Type genericType = listType.MakeGenericType(itemType);
                    if(memberType.IsAssignableFrom(genericType))
                    {
                        ItemType = itemType;
                        _listConstructor = genericType.GetConstructor(Type.EmptyTypes);
                        break;
                    }
                }

                // Did we find one?
                if(ItemType == null) throw new Exception("No appropriate item type could be found.");
            }
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
            IEnumerable<object> items = !String.IsNullOrEmpty(ItemQuery) ? child.Eval(ItemQuery)
                : child.Children.OfType<Element>().Where(e => e.Name.Equals(ItemName)).Cast<object>();

            // Create the list
            IList list = (IList)_listConstructor.Invoke(null);

            // Populate with values
            TypeCache itemTypeCache = cache.GetTypeCache(ItemType);
            foreach(object item in items)
            {
                if(ItemsArePersistentObjects)
                {
                    // Get, attach, and fetch the persistent object instance
                    Element itemElement = item as Element;
                    if (itemElement == null) throw new Exception("Array item nodes must be elements.");
                    object itemObject = cache.GetObject(itemTypeCache, itemElement, AttachItems);
                    if (AttachItems)
                    {
                        ObjectWrapper wrapper = cache.Attach(itemObject, itemElement);
                        wrapper.Fetch();    // Use the ObjectWrapper.Fetch() to take advantage of last update time caching
                    }
                    else
                    {
                        typeCache.Persister.Fetch(itemElement, itemObject, itemTypeCache, cache);
                    }
                    list.Add(itemObject);
                }
                else
                {
                    Node.Node itemNode = item as Node.Node;
                    object itemObject = GetObjectFromString(
                        itemNode != null ? itemNode.Value : item.ToString(), null, null, itemTypeCache);
                    list.Add(itemObject);
                }
            }

            // Return either an array or list
            if(_array)
            {
                Array array = Array.CreateInstance(ItemType, list.Count);
                list.CopyTo(array, 0);
                return array;
            }
            return list;
        }

        internal override object SerializeValue(object source, TypeCache typeCache, Cache cache)
        {
            if (!String.IsNullOrEmpty(ItemQuery)) return null;
            
            // Key = source object, Value = serialized data
            List<KeyValuePair<object, object>> values = new List<KeyValuePair<object, object>>();

            // Iterate over the source object
            if (source != null)
            {
                TypeCache itemTypeCache = cache.GetTypeCache(ItemType);
                IEnumerable items = (IEnumerable) source;
                foreach (object item in items)
                {
                    object value = ItemsArePersistentObjects ?
                                                                 typeCache.Persister.Serialize(item, itemTypeCache, cache)
                                       : GetStringFromObject(item, itemTypeCache);
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

            // Remove all item elements with the item name
            List<Element> removeElements =
                child.Children.OfType<Element>()
                .Where(e => e.Name == ItemName).ToList();
            foreach (Element removeElement in removeElements)
            {
                removeElement.Remove();
            }

            // Store all items
            TypeCache itemTypeCache = cache.GetTypeCache(ItemType);
            foreach (KeyValuePair<object, object> kvp in
                (IEnumerable<KeyValuePair<object, object>>) serialized)
            {
                child.Append(new Element(ItemName));
                Element itemElement = (Element) child.Children.Last();
                if (ItemsArePersistentObjects)
                {
                    itemTypeCache.Persister.Store(itemElement, kvp.Value, kvp.Key, itemTypeCache, cache);
                }
                else
                {
                    itemElement.Value = (string) kvp.Value;
                }
            }
        }
    }
}
