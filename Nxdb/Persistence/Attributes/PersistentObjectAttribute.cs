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
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using Nxdb.Node;

namespace Nxdb.Persistence.Attributes
{
    /// <summary>
    /// Stores or fetches a nested persistent object to/from a child element of the container element. If more
    /// than one element with the given name exists, the first one will be used.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    class PersistentObjectAttribute : PersistentMemberAttribute
    {
        /// <summary>
        /// Gets or sets the name to use or create. If unspecified, the name of
        /// the field or property will be used (as converted to a valid XML name).
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// Gets or sets a value indicating whether this field or property should be attached
        /// to the manager. If true (default), the manager cache will be searched and an
        /// existing instance used or a new instance created and attached. If false, a new
        /// detached instance will be created on every fetch.
        /// </summary>
        public bool Attach { get; set; }

        public PersistentObjectAttribute()
        {
            Attach = true;
        }

        internal override void Inititalize(MemberInfo memberInfo)
        {
            base.Inititalize(memberInfo);
            Name = GetName(Name, memberInfo.Name);
        }

        internal override object FetchValue(Element element, object target, TypeCache typeCache, Cache cache)
        {
            element = GetElementFromQuery(element);
            if (element == null) return null;

            element = element.Children.OfType<Element>().Where(e => e.Name.Equals(Name)).FirstOrDefault();
            if (element == null) return null;

            // Get, attach, and fetch the persistent object instance
            object value = cache.GetObject(typeCache, element, Attach);
            if(Attach)
            {
                ObjectWrapper wrapper = cache.Attach(value, element);
                wrapper.Fetch();    // Use the ObjectWrapper.Fetch() to take advantage of last update time caching
            }
            else
            {   
                typeCache.Persister.Fetch(element, value, typeCache, cache);
            }
            return value;
        }

        internal override void StoreValue(Element element, object source, TypeCache typeCache, Cache cache)
        {
            //element = GetElementFromQuery(element);
            //if (element == null) return;

            //Element child = element.Children.OfType<Element>().Where(e => e.Name.Equals(Name)).FirstOrDefault();
            //if (child == null)
            //{
            //    element.Append(String.Format("<{0}>{1}</{0}>", Name, value));
            //}
            //else
            //{
            //    child.InnerXml = ((Element) value).InnerXml;
            //}

            // TODO: Do I need to deal with attributes on the value element?

        }
    }
}
