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
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Xml;
using Nxdb.Node;

namespace Nxdb.Persistence.Attributes
{
    /// <summary>
    /// The base class for all persistent attributes.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public abstract class PersistentMemberAttribute : System.Attribute
    {
        protected PersistentMemberAttribute()
        {
            Store = true;
            Fetch = true;
        }
        
        /// <summary>
        /// Gets or sets a value indicating whether this field or property should be stored.
        /// </summary>
        public bool Store { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this field or property should be fetched.
        /// </summary>
        public bool Fetch { get; set; }

        /// <summary>
        /// Gets or sets the order in which fields and properties are processed (lower values
        /// are processed first). Ordering is unspecified in the case of duplicate values.
        /// The default order is 0.
        /// </summary>
        public int Order { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this member is required.
        /// </summary>
        public bool Required { get; set; }

        /// <summary>
        /// Gets or sets a query to use for getting the node to use for a value.
        /// This is exclusive with Name (if an available property) and both may
        /// not be used.
        /// </summary>
        public string Query { get; set; }

        /// <summary>
        /// Gets or sets a query to use when the field or property is being
        /// stored and the query does not result in a usable node. If the
        /// query does not result in a node and this is not specified, a value will
        /// not be stored for the field or property. This is exclusive with Name
        /// (if an available property) and both may not be used.
        /// </summary>
        public string CreateQuery { get; set; }

        /// <summary>
        /// Allows derived classes to initialze state based on attached member.
        /// </summary>
        internal virtual void Inititalize(Type memberType, string memberName, Cache cache)
        {
        }

        internal TypeConverter InitializeTypeConverter(Type typeConverterType)
        {
            if (typeConverterType != null)
            {
                if (!typeof (TypeConverter).IsAssignableFrom(typeConverterType)) 
                    throw new Exception("TypeConverter must specify a valid TypeConverter type.");
                ConstructorInfo constructor = typeConverterType.GetConstructor(Type.EmptyTypes);
                if (constructor == null) throw new Exception("Explicit TypeConverter must implement an empty constructor.");
                return constructor.Invoke(null) as TypeConverter;
            }
            return null;
        }

        internal abstract object FetchValue(Element element, object target, TypeCache typeCache, Cache cache);
        internal abstract object SerializeValue(object source, TypeCache typeCache, Cache cache);
        internal abstract void StoreValue(Element element, object serialized, object source, TypeCache typeCache, Cache cache);

        internal object GetObjectFromString(string value, string defaultValue, object target, Type type, TypeConverter typeConverter)
        {
            value = value ?? defaultValue;
            TypeConverter converter = typeConverter ?? (target == null
                ? TypeDescriptor.GetConverter(type) : TypeDescriptor.GetConverter(target));
            if (converter == null) throw new Exception("Could not get TypeConverter for member.");
            if (!converter.CanConvertFrom(typeof(string))) throw new Exception("Can not convert member from string.");
            return converter.ConvertFromString(value);
        }

        internal string GetStringFromObject(object source, Type type, TypeConverter typeConverter)
        {
            TypeConverter converter = typeConverter ?? (source == null
                ? TypeDescriptor.GetConverter(type) : TypeDescriptor.GetConverter(source));
            if (converter == null) throw new Exception("Could not get TypeConverter for member.");
            if (!converter.CanConvertTo(typeof(string))) throw new Exception("Can not convert member to string.");
            return converter.ConvertToString(source);
        }

        // Returns true if a query was specified (even if it didn't return a node)
        protected static bool GetNodeFromQuery<T>(string query, string createQuery,
            Element element, out T node) where T : Node.Node
        {
            node = null;
            if (!String.IsNullOrEmpty(query))
            {
                node = element.EvalSingle(query) as T;
                if (node == null)
                {
                    // We didn't get the target, see if we have a query that can create it
                    if (!String.IsNullOrEmpty(createQuery))
                    {
                        element.Eval(createQuery);
                        node = element.EvalSingle(query) as T;  //Try to get the target again
                    }
                }
                return true;
            }
            return false;
        }

        protected string GetName(string name, string defaultName, params string[] exclusive)
        {
            if (exclusive.Any(e => !String.IsNullOrEmpty(e)))
            {
                if (!String.IsNullOrEmpty(name))
                {
                    throw new Exception("Multiple exclusive properties specified.");
                }
                return null;
            }
            if (String.IsNullOrEmpty(name))
            {
                return String.IsNullOrEmpty(defaultName)
                    ? null : XmlConvert.EncodeName(defaultName);
            }
            XmlConvert.VerifyName(name);
            return name;
        }
    }
}
