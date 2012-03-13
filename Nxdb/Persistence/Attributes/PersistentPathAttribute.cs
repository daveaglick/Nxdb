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

namespace Nxdb.Persistence.Attributes
{
    /// <summary>
    /// Stores and fetches the field or property to/from a specified path expression. If no node is found at the
    /// specified path during storage, an query can be specified that indicates how the field or property should
    /// be stored.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class PersistentPathAttribute : PersistentMemberAttribute
    {
        /// <summary>
        /// Gets or sets a default value to use if the specified node isn't found during fetch.
        /// This value is passed to the type converter to create an instance of the target object.
        /// </summary>
        public string Default { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PersistentPathAttribute"/> class.
        /// </summary>
        /// <param name="query">The query to use for fetching the value. If the specified
        /// query returns a sequence, only the first item is used. If the specified query returns
        /// a node, it's value is used. If the specified query returns an atomic value, it's
        /// ToString() result is used as the value.</param>
        /// <param name="createQuery">The query to use when the field or property is being
        /// stored and the value query does not result in a node. If the value query does not result
        /// in a node and this is not specified, a value will not be stored for the
        /// field or property.</param>
        public PersistentPathAttribute(string query, string createQuery)
        {
            if (String.IsNullOrEmpty(query)) throw new ArgumentNullException("query");
            Query = query;
            CreateQuery = createQuery;
        }

        internal override object FetchValue(Element element, object target, TypeCache typeCache, Cache cache)
        {
            object result = element.EvalSingle(Query);
            if (result == null) return null;
            Node.Node node = result as Node.Node;
            return GetObjectFromString(
                node != null ? node.Value : result.ToString(), Default, target, typeCache);
        }

        internal override object SerializeValue(object source, TypeCache typeCache, Cache cache)
        {
            return GetStringFromObject(source, typeCache);
        }

        internal override void StoreValue(Element element, object serialized, object source, TypeCache typeCache, Cache cache)
        {
            Node.Node node = element.EvalSingle(Query) as Node.Node;
            string value = (string) serialized;

            if (node == null && value != null && !String.IsNullOrEmpty(CreateQuery))
            {
                element.Eval(CreateQuery);
                node = element.EvalSingle(Query) as Node.Node;
            }
            else if (node != null && value == null)
            {
                node.Remove();
                return;
            }

            if (node != null)
            {
                node.Value = value;
            }
        }
    }
}
