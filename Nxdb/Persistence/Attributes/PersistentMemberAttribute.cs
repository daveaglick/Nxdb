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
        /// Gets or sets a default value to use if the specified node isn't found during fetch.
        /// This value is passed to the type converter to create an instance of the target object.
        /// </summary>
        public string Default { get; set; }

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

        // Returns null if the requested node does not exist, in which case the Default should be used (if available)
        internal virtual string FetchValue(Element element)
        {
            Element target = !String.IsNullOrEmpty(Query)
                ? element.EvalSingle(Query) as Element : element;
            return target == null ? null : DoFetchValue(target);
        }

        internal virtual void StoreValue(Element element, string value)
        {
            if (!String.IsNullOrEmpty(Query))
            {
                Element target = element.EvalSingle(Query) as Element;
                if (target == null)
                {
                    if (!String.IsNullOrEmpty(CreateQuery))
                    {
                        Query query = new Query(element);
                        query.SetVariable("value", value);
                        query.Eval(CreateQuery);
                    }
                    return;
                }
                DoStoreValue(target, value);
                return;
            }
            DoStoreValue(element, value);
        }

        protected virtual string DoFetchValue(Element element)
        {
            return null;
        }

        protected virtual void DoStoreValue(Element element, string value)
        {
        }
    }
}
