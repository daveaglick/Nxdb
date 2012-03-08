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
using System.Reflection;
using System.Xml;
using Nxdb.Node;
using Attribute = System.Attribute;

namespace Nxdb.Persistence.Attributes
{
    /// <summary>
    /// The base class for all persistent attributes.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public abstract class PersistentMemberAttribute : Attribute
    {
        protected PersistentMemberAttribute()
        {
            Store = true;
            Fetch = true;
        }

        /// <summary>
        /// Gets or sets the order in which fields and properties are processed (lower values
        /// are processed first). Ordering is unspecified in the case of duplicate values.
        /// The default order is 0.
        /// </summary>
        public int Order { get; set; }
        
        /// <summary>
        /// Gets or sets a value indicating whether this field or property should be stored.
        /// </summary>
        public bool Store { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this field or property should be fetched.
        /// </summary>
        public bool Fetch { get; set; }

        /// <summary>
        /// Gets or sets a query to use for getting the node to use for a value or the
        /// value itself. Different persistent attributes use this value differently.
        /// Some (such as PersistentnElementAttribute and PersistentAttributeAttribute)
        /// use the query to get an alternate parent node, others (such as
        /// PersistentPathAttribute) use it to get the actual node or value.
        /// </summary>
        public string Query { get; set; }

        /// <summary>
        /// Gets or sets a query to use when the field or property is being
        /// stored and the value query does not result in a usable node. If the value
        /// query does not result in a node and this is not specified, a value will
        /// not be stored for the field or property. Keep in mind that in cases where
        /// the query is intended to refer to a parent node (such as
        /// PersistentnElementAttribute and PersistentAttributeAttribute), the
        /// create query must create both the parent element and the required child.
        /// </summary>
        public string CreateQuery { get; set; }

        /// <summary>
        /// Allows derived classes to initialze state based on attached member.
        /// </summary>
        internal virtual void Inititalize(MemberInfo memberInfo)
        {
        }

        internal abstract object FetchValue(Element element, object target, TypeCache typeCache, Cache cache);
        internal abstract void StoreValue(Element element, object source, TypeCache typeCache, Cache cache);

        // Provide value if the create query should be run
        protected Element GetElementFromQuery(Element element)
        {
            if (!String.IsNullOrEmpty(Query))
            {
                Element target = element.EvalSingle(Query) as Element;
                if (target == null)
                {
                    // We didn't get the target, see if we have a query that can create it
                    if (!String.IsNullOrEmpty(CreateQuery))
                    {
                        using (new Updates(true))
                        {
                            element.Eval(CreateQuery);
                        }

                        //Try to get the target again
                        target = element.EvalSingle(Query) as Element;
                    }
                }
                element = target;
            }
            return element;
        }

        protected string GetName(string name, string defaultName)
        {
            if (String.IsNullOrEmpty(name))
            {
                return XmlConvert.EncodeName(defaultName);
            }
            XmlConvert.VerifyName(name);
            return name;
        }
    }
}
