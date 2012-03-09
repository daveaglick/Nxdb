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
using Nxdb.Node;

namespace Nxdb.Persistence.Attributes
{
    /// <summary>
    /// Stores and fetches the field or property to/from a specified path expression. If no node is found at the
    /// specified path during storage, an query can be specified that indicates how the field or property should
    /// be stored.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class PersistentPathAttribute : StringBasedPersistentAttribute
    {
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

        protected override string FetchValue(Element element)
        {
            object result = element.EvalSingle(Query);
            if (result == null) return null;
            Node.Node node = result as Node.Node;
            return node != null ? node.Value : result.ToString();
        }

        protected override void StoreValue(Element element, string value)
        {
            Node.Node node = element.EvalSingle(Query) as Node.Node;

            if (node == null && value != null && !String.IsNullOrEmpty(CreateQuery))
            {
                element.Eval(CreateQuery);
                node = element.EvalSingle(Query) as Node.Node;
            }
            else if(node != null && value == null)
            {
                node.Remove();
                return;
            }

            if(node != null)
            {
                node.Value = value;
            }
        }
    }
}
