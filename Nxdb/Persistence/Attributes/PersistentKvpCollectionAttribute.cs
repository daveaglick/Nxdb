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
using Nxdb.Node;

namespace Nxdb.Persistence.Attributes
{
    /// <summary>
    /// Stores or fetches a collection of KeyValuePair objects to/from a child element of the
    /// container element. This supports dictionaries, sorted lists, etc. The persistent object
    /// must either implement ICollection&lt;KeyValuePair&lt;TKey,TValue&gt;&gt; or implement
    /// IDictionary and provide KeyType and ValueType.
    /// If more than one element with the given name exists, the first one will be used.
    /// By default this stores and fetches data in the following format:
    /// <code>
    /// <Container>
    ///   <Name>
    ///     <Item><Key>...</Key><Value>...</Value></Item>
    ///     <Item><Key>...</Key><Value>...</Value></Item>
    ///     <Item><Key>...</Key><Value>...</Value></Item>
    ///   </Name>
    /// </Container>
    /// </code> 
    /// There are many parameters that can be used to control the formatting such as changing the
    /// element names of key or value elements, storing keys or values as attributes or evaluating
    /// queries to fetch keys or values.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class PersistentKvpCollectionAttribute : PersistentMemberAttribute
    {
        /// <summary>
        /// Gets or sets the element name to use or create. If unspecified, the name of
        /// the field or property will be used (as converted to a valid XML name).
        /// This is exclusive with Query and both may not be specified.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the element name to use or create for the key/value container. If unspecified, "Item" will be used.
        /// This is exclusive with ItemQuery and both may not be specified.
        /// </summary>
        public string ItemName { get; set; }

        /// <summary>
        /// Gets or sets the query to use for getting an item. This is evaluated from the context of the parent
        /// element. If an item query is used, the dictionary is read only and will not be persisted to the database.
        /// This is exclusive with ItemName and both may not be specified.
        /// </summary>
        public string ItemQuery { get; set; }

        /// <summary>
        /// Gets or sets the element name to use or create for key items. If unspecified, "Key" will be used. This is exclusive
        /// with KeyAttributeName and KeyQuery and only one may be specified.
        /// </summary>
        public string KeyElementName { get; set; }

        /// <summary>
        /// Gets or sets the attribute name to use or create for key items. This is exclusive
        /// with KeyElementName and KeyQuery and only one may be specified.
        /// </summary>
        public string KeyAttributeName { get; set; }

        /// <summary>
        /// Gets or sets the query to use for getting a key. This is evaluated from the context of the parent
        /// item element. If a key query is used, the dictionary is read only and will not be persisted to the database.
        /// This is exclusive with KeyAttributeName and KeyElementName and only one may be specified.
        /// </summary>
        public string KeyQuery { get; set; }

        /// <summary>
        /// Gets or sets the type of keys. If unspecified, the actual type of the keys will be used. If specified,
        /// this must be assignable to the type of the key in the dictionary.
        /// </summary>
        public Type KeyType { get; set; }

        /// <summary>
        /// Gets or sets an explicit type converter to use for converting the value
        /// to and from a string. If this is not specified, the default TypeConverter
        /// for the object type will be used. If it is specified, it should be able to
        /// convert between the object type and a string. As a convenience, simple
        /// custom TypeConverters can be derived from PersistentTypeConverter.
        /// </summary>
        public Type KeyTypeConverter { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the keys are persistent object types.
        /// If this is true, KeyAttributeName must not be specified and KeyQuery if
        /// specified should return an element node.
        /// </summary>
        public bool KeysArePersistentObjects { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether keys should be attached.
        /// Only meaningful if KeysArePersistentObjects is true.
        /// If true (default), the manager cache will be searched and an
        /// existing instance used or a new instance created and attached for each key. If false, a new
        /// detached instance will be created on every fetch for each key.
        /// </summary>
        public bool AttachKeys { get; set; }
        
        /// <summary>
        /// Gets or sets the element name to use or create for value items. If unspecified, "Value" will be used. This is exclusive
        /// with ValueAttributeName and ValueQuery and only one may be specified.
        /// </summary>
        public string ValueElementName { get; set; }

        /// <summary>
        /// Gets or sets the attribute name to use or create for value items. This is exclusive
        /// with ValueElementName and ValueQuery and only one may be specified.
        /// </summary>
        public string ValueAttributeName { get; set; }

        /// <summary>
        /// Gets or sets the query to use for getting a value. This is evaluated from the context of the parent
        /// item element. If a value query is used, the dictionary is read only and will not be persisted to the database.
        /// This is exclusive with ValueAttributeName and ValueElementName and only one may be specified.
        /// </summary>
        public string ValueQuery { get; set; }

        /// <summary>
        /// Gets or sets the type of values. If unspecified, the actual type of the values will be used. If specified,
        /// this must be assignable to the type of the value in the dictionary.
        /// </summary>
        public Type ValueType { get; set; }        
        
        /// <summary>
        /// Gets or sets an explicit type converter to use for converting the value
        /// to and from a string. If this is not specified, the default TypeConverter
        /// for the object type will be used. If it is specified, it should be able to
        /// convert between the object type and a string. As a convenience, simple
        /// custom TypeConverters can be derived from PersistentTypeConverter.
        /// </summary>
        public Type ValueTypeConverter { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the values are persistent object types.
        /// If this is true, ValueAttributeName must not be specified and ValueQuery if
        /// specified should return an element node.
        /// </summary>
        public bool ValuesArePersistentObjects { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether values should be attached.
        /// Only meaningful if ValuesArePersistentObjects is true.
        /// If true (default), the manager cache will be searched and an
        /// existing instance used or a new instance created and attached for each value. If false, a new
        /// detached instance will be created on every fetch for each value.
        /// </summary>
        public bool AttachValues { get; set; }

        private TypeCache _keyTypeCache = null;
        private TypeCache _valueTypeCache = null;
        private TypeConverter _keyTypeConverter = null;
        private TypeConverter _valueTypeConverter = null;
        private Func<object, object> _getCollection = null; // collection (collection)
        private Action<object, object, object> _setCollectionItem = null; // (collection, key, value)
        private Func<object, object> _getKey = null;
        private Func<object, object> _getValue = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="PersistentKvpCollectionAttribute"/> class.
        /// </summary>
        public PersistentKvpCollectionAttribute()
        {
            AttachKeys = true;
            AttachValues = true;
        }

        internal override void Inititalize(Type memberType, string memberName, Cache cache)
        {
            base.Inititalize(memberType, memberName, cache);

            // Get element names
            Name = GetName(Name, memberName, Query, CreateQuery);
            ItemName = GetName(ItemName, "Item", ItemQuery);
            KeyAttributeName = GetName(KeyAttributeName, null, KeyElementName, KeyQuery);
            if (String.IsNullOrEmpty(KeyAttributeName))
            {
                KeyElementName = GetName(KeyElementName, "Key", KeyQuery);
            }
            ValueAttributeName = GetName(ValueAttributeName, null, ValueElementName, ValueQuery);
            if (String.IsNullOrEmpty(ValueAttributeName))
            {
                ValueElementName = GetName(ValueElementName, "Value", ValueQuery);
            }

            // Get attribute names
            if(KeysArePersistentObjects && !String.IsNullOrEmpty(KeyAttributeName))
                throw new Exception("KeyAttributeName must not be specified if KeysArePersistentObjects is true.");
            if (ValuesArePersistentObjects && !String.IsNullOrEmpty(ValueAttributeName))
                throw new Exception("ValueAttributeName must not be specified if ValuesArePersistentObjects is true.");

            // Get the TypeConverters
            if (KeyTypeConverter != null)
            {
                if (KeysArePersistentObjects) throw new Exception("A TypeConverter can not be specified for persistent member objects.");
                _keyTypeConverter = InitializeTypeConverter(KeyTypeConverter);
            }
            if (ValueTypeConverter != null)
            {
                if (ValuesArePersistentObjects) throw new Exception("A TypeConverter can not be specified for persistent member objects.");
                _valueTypeConverter = InitializeTypeConverter(ValueTypeConverter);
            }
            
            // Resolve the type of collection and the key/value type
            // Key = ICollection<>, Value = KeyValuePair<,>
            List<KeyValuePair<Type, Type>> kvpInterfaces =
                new List<KeyValuePair<Type, Type>>(memberType.GetInterfaces()
                .Where(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(ICollection<>))
                .Select(t => new KeyValuePair<Type, Type>(t, t.GetGenericArguments()[0]))
                .Where(k => k.Value.IsGenericType && k.Value.GetGenericTypeDefinition() == typeof(KeyValuePair<,>)));
            if(kvpInterfaces.Count > 0)
            {
                // The member implements ICollection<KeyValuePair<TKey,TValue>>
                // Verify KeyType and ValueType are compatable (if specified)
                KeyValuePair<Type, Type>? kvpType = null;
                foreach (KeyValuePair<Type, Type> kvp in kvpInterfaces)
                {
                    Type keyType = kvp.Value.GetGenericArguments()[0];
                    Type valueType = kvp.Value.GetGenericArguments()[1];
                    if((KeyType == null || keyType.IsAssignableFrom(KeyType))
                        && (ValueType == null || valueType.IsAssignableFrom(ValueType)))
                    {
                        kvpType = kvp;
                        if(KeyType == null) KeyType = keyType;
                        if(ValueType == null) ValueType = valueType;
                        break;
                    }
                }

                // Make sure we got a kvp type
                if (!kvpType.HasValue) throw new Exception("No appropriate key or value type could be found.");

                // Get the constructor and other functions
                ConstructorInfo constructor = memberType.GetConstructor(Type.EmptyTypes);
                if (constructor == null) throw new Exception("Persistent collection member must implement an empty constructor.");
                MethodInfo clearMethod = kvpType.Value.Key.GetMethod("Clear");
                _getCollection = c =>
                    {
                        if (c == null) return constructor.Invoke(null);
                        clearMethod.Invoke(c, null);
                        return c;
                    };
                ConstructorInfo kvpConstructor = kvpType.Value.Value.GetConstructor(new[] { KeyType, ValueType });
                if (kvpConstructor == null) throw new Exception("Could not get KeyValuePair constructor.");
                MethodInfo addMethod = kvpType.Value.Key.GetMethod("Add");
                _setCollectionItem = (c, k, v) => addMethod.Invoke(c, new[] { kvpConstructor.Invoke(new []{k, v}) });

                PropertyInfo keyProperty = kvpType.Value.Value.GetProperty("Key");
                if(keyProperty == null) throw new Exception("Could not get Key property.");
                _getKey = i => keyProperty.GetValue(i, null);

                PropertyInfo valueProperty = kvpType.Value.Value.GetProperty("Value");
                if (valueProperty == null) throw new Exception("Could not get Value property.");
                _getValue = i => valueProperty.GetValue(i, null);
            }
            else if(typeof(IDictionary).IsAssignableFrom(memberType))
            {
                // The member implements IDictionary
                if (KeyType == null) throw new Exception("A persistent collection that implements IDictionary must provide a KeyType.");
                if (ValueType == null) throw new Exception("A persistent collection that implements IDictionary must provide a ValueType.");
                ConstructorInfo constructor = memberType.GetConstructor(Type.EmptyTypes);
                if (constructor == null) throw new Exception("Persistent collection member must implement an empty constructor.");
                _getCollection = c =>
                    {
                        if (c == null) return constructor.Invoke(null);
                        ((IDictionary)c).Clear();
                        return c;
                    };
                _setCollectionItem = (c, k, v) => ((IDictionary)c).Add(k, v);
                _getKey = i => ((DictionaryEntry)i).Key;
                _getValue = i => ((DictionaryEntry)i).Value;
            }
            else
            {
                throw new Exception("Persistent kvp collection member must implement ICollection<KeyValuePair<TKey,TValue>> or implement IDictionary.");
            }

            _keyTypeCache = cache.GetTypeCache(KeyType);
            _valueTypeCache = cache.GetTypeCache(ValueType);
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

            // Get all the item nodes (this will throw an exception on Cast<>() if an ItemQuery returns other than Element nodes)
            IList<Element> items = new List<Element>(!String.IsNullOrEmpty(ItemQuery) ? child.Eval(ItemQuery).Cast<Element>()
                : child.Children.OfType<Element>().Where(e => e.Name.Equals(ItemName)));

            // Create the collection
            object collection = _getCollection(target);

            // Populate with values
            foreach(Element item in items)
            {
                object keyValue = FetchValue(item, KeyAttributeName, KeyElementName, KeyQuery,
                    KeysArePersistentObjects, AttachKeys, _keyTypeCache, _keyTypeConverter, cache);
                object valueValue = FetchValue(item, ValueAttributeName, ValueElementName, ValueQuery,
                    ValuesArePersistentObjects, AttachValues, _valueTypeCache, _valueTypeConverter, cache);
                if(keyValue != null && valueValue != null) _setCollectionItem(collection, keyValue, valueValue);
            }

            return collection;
        }

        private object FetchValue(Element item, string attributeName, string elementName, string query,
            bool arePersistentObjects, bool attach, TypeCache typeCache, TypeConverter typeConverter, Cache cache)
        {
            object value = null;
            if (!String.IsNullOrEmpty(attributeName))
            {
                Node.Attribute attribute = item.Attribute(attributeName);
                value = attribute == null ? null : GetObjectFromString(attribute.Value, null, null, typeCache.Type, typeConverter);
            }
            else
            {
                object result = !String.IsNullOrEmpty(query) ? item.EvalSingle(query)
                    : item.Children.OfType<Element>().FirstOrDefault(e => e.Name.Equals(elementName));
                if (arePersistentObjects)
                {
                    // Get, attach, and fetch the persistent object instance
                    Element element = result as Element;
                    if (element == null) throw new Exception("Persistent value node must be an element.");
                    value = cache.GetObject(typeCache, element, attach);
                }
                else
                {
                    Node.Node itemNode = result as Node.Node;
                    value = GetObjectFromString(itemNode != null ? itemNode.Value
                        : result == null ? null : result.ToString(), null, null, typeCache.Type, typeConverter);
                }
            }
            return value;
        }

        internal override object SerializeValue(object source, TypeCache typeCache, Cache cache)
        {
            if (!String.IsNullOrEmpty(ItemQuery)
                || !String.IsNullOrEmpty(KeyQuery)
                || !String.IsNullOrEmpty(ValueQuery))
                throw new Exception("Can not store persistent collection that uses queries.");

            // Key/key = key source object, key/value = serialized key data
            // Value/key = value source object, value/value = serialized value data
            List<KeyValuePair<KeyValuePair<object, object>, KeyValuePair<object, object>>> values
                = new List<KeyValuePair<KeyValuePair<object, object>, KeyValuePair<object, object>>>();
            
            // Iterate over the source object
            if (source != null)
            {
                IEnumerable items = (IEnumerable)source;
                foreach (object item in items)
                {
                    object key = _getKey(item);
                    object keyValue = KeysArePersistentObjects
                        ? _keyTypeCache.Persister.Serialize(key, _keyTypeCache, cache)
                        : GetStringFromObject(key, KeyType, _keyTypeConverter);

                    object value = _getValue(item);
                    object valueValue = ValuesArePersistentObjects
                        ? _valueTypeCache.Persister.Serialize(value, _valueTypeCache, cache)
                        : GetStringFromObject(value, ValueType, _valueTypeConverter);

                    values.Add(new KeyValuePair<KeyValuePair<object, object>, KeyValuePair<object, object>>(
                        new KeyValuePair<object, object>(key, keyValue), new KeyValuePair<object, object>(value, valueValue)));
                }
            }

            return values;
        }

        internal override void StoreValue(Element element, object serialized, object source, TypeCache typeCache, Cache cache)
        {
            if (!String.IsNullOrEmpty(ItemQuery)
                || !String.IsNullOrEmpty(KeyQuery)
                || !String.IsNullOrEmpty(ValueQuery))
                throw new Exception("Can not store persistent collection that uses queries.");

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
            foreach (KeyValuePair<KeyValuePair<object, object>, KeyValuePair<object, object>> kvp in
                (IEnumerable<KeyValuePair<KeyValuePair<object, object>, KeyValuePair<object, object>>>)serialized)
            {
                child.Append(new Element(ItemName));
                Element itemElement = (Element)child.Children.Last();

                // Store the key
                if(!String.IsNullOrEmpty(KeyElementName))
                {
                    itemElement.Append(new Element(KeyElementName));
                    Element keyElement = (Element) itemElement.Children.Last();
                    if(KeysArePersistentObjects)
                    {
                        _keyTypeCache.Persister.Store(keyElement, kvp.Key.Value, kvp.Key.Key, _keyTypeCache, cache);
                    }
                    else
                    {
                        keyElement.Value = (string) kvp.Key.Value;
                    }
                }
                else
                {
                    itemElement.InsertAttribute(KeyAttributeName, (string)kvp.Key.Value);
                }

                // Store the value
                if (!String.IsNullOrEmpty(ValueElementName))
                {
                    itemElement.Append(new Element(ValueElementName));
                    Element valueElement = (Element)itemElement.Children.Last();
                    if (ValuesArePersistentObjects)
                    {
                        _valueTypeCache.Persister.Store(valueElement, kvp.Value.Value, kvp.Value.Key, _valueTypeCache, cache);
                    }
                    else
                    {
                        valueElement.Value = (string)kvp.Value.Value;
                    }
                }
                else
                {
                    itemElement.InsertAttribute(ValueAttributeName, (string)kvp.Value.Value);
                }
            }
        }
    }
}
