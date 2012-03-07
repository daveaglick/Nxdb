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
using System.Text;
using Nxdb.Node;

namespace Nxdb.Persistence
{
    /// <summary>
    /// Stores and fetches the field or property to/from a specified path expression. If no node is found at the
    /// specified path during storage, an query can be specified that indicates how the field or property should
    /// be stored.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class PersistentPathAttribute : PersistentAttributeBase
    {
        private readonly string _valueQuery = null;

        /// <summary>
        /// Gets or sets the query to use when the field or property is being stored and
        /// the value query does not result in a node. If the value query does not result
        /// in a node and this is not specified, a value will not be stored for the
        /// field or property.
        /// </summary>
        public string CreateQuery { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PersistentPathAttribute"/> class.
        /// </summary>
        /// <param name="valueQuery">The query to use for fetching the value. If the specified
        /// query returns a sequence, only the first item is used. If the specified query returns
        /// a node, it's value is used. If the specified query returns an atomic value, it's
        /// ToString() result is used as the value.</param>
        public PersistentPathAttribute(string valueQuery)
        {
            if (String.IsNullOrEmpty(valueQuery)) throw new ArgumentNullException("valueQuery");
            _valueQuery = valueQuery;
        }

        internal override string FetchValue(Element element)
        {
            object result = element.EvalSingle(_valueQuery);
            if (result == null) return null;
            Node.Node node = result as Node.Node;
            return node != null ? node.Value : result.ToString();
        }

        internal override void StoreValue(Element element, string value)
        {
            Node.Node node = element.EvalSingle(_valueQuery) as Node.Node;
            if(node == null)
            {
                if(!String.IsNullOrEmpty(CreateQuery))
                {
                    element.Eval(CreateQuery);
                }
            }
            else
            {
                node.Value = value;
            }
        }
    }
}
