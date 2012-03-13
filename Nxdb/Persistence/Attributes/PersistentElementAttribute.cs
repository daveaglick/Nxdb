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
using System.Linq;
using Nxdb.Node;

namespace Nxdb.Persistence.Attributes
{
    /// <summary>
    /// Stores and fetches the field or property to/from a child element of the container element. If more
    /// than one element with the given name exists, the first one will be used.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class PersistentElementAttribute : PersistentMemberAttribute
    {
        /// <summary>
        /// Gets or sets the name of the element to use or create. If unspecified,
        /// the name of the field or property will be used (as converted to a valid
        /// XML name).
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets a default value to use if the specified node isn't found during fetch.
        /// This value is passed to the type converter to create an instance of the target object.
        /// </summary>
        public string Default { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the field or property is a persistent
        /// object type.
        /// </summary>
        public bool IsPersistentObject { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this field or property should be attached
        /// to the manager. Only valid if IsPersistentObject is true. If true (default), the
        /// manager cache will be searched and an existing instance used or a new instance
        /// created and attached. If false, a new detached instance will be created on every fetch.
        /// </summary>
        public bool Attach { get; set; }

        public PersistentElementAttribute()
        {
            Attach = true;
        }

        internal override void Inititalize(System.Reflection.MemberInfo memberInfo)
        {
            base.Inititalize(memberInfo);
            Name = GetName(Name, memberInfo.Name, Query);
            if(IsPersistentObject && !String.IsNullOrEmpty(Default))
                throw new Exception("Persistent object based members cannot specify a default value.");
        }

        internal override object FetchValue(Element element, object target, TypeCache typeCache, Cache cache)
        {
            Element child;
            if (!GetNodeFromQuery(Query, CreateQuery, element, out child))
            {
                child = element.Children.OfType<Element>().Where(e => e.Name.Equals(Name)).FirstOrDefault();
            }

            if (child == null)
            {
                return null;
            }

            if (IsPersistentObject)
            {
                // Get, attach, and fetch the persistent object instance
                object value = cache.GetObject(typeCache, child, Attach);
                if (Attach)
                {
                    ObjectWrapper wrapper = cache.Attach(value, child);
                    wrapper.Fetch();    // Use the ObjectWrapper.Fetch() to take advantage of last update time caching
                }
                else
                {
                    typeCache.Persister.Fetch(child, value, typeCache, cache);
                }
                return value;
            }
            else
            {
                return GetObjectFromString(child.Value, Default, target, typeCache);
            }
        }

        internal override object SerializeValue(object source, TypeCache typeCache, Cache cache)
        {
            return IsPersistentObject
                ? typeCache.Persister.Serialize(source, typeCache, cache)
                : GetStringFromObject(source, typeCache);
        }

        internal override void StoreValue(Element element, object serialized, object source, TypeCache typeCache, Cache cache)
        {
            Element child;
            if (!GetNodeFromQuery(Query, CreateQuery, element, out child))
            {
                child = element.Children.OfType<Element>().Where(e => e.Name.Equals(Name)).FirstOrDefault();
            }
            else if (child == null)
            {
                return;
            }

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

            if(IsPersistentObject)
            {
                typeCache.Persister.Store(child, serialized, source, typeCache, cache);
            }
            else
            {
                child.Value = (string)serialized;
            }
        }
    }
}
