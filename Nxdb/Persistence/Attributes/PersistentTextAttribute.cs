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
    /// Stores and fetches the field or property to/from a text node of the container element.
    /// If the container element contains more than one text node (such as with mixed content elements), only the
    /// first text node will be used.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class PersistentTextAttribute : PersistentMemberAttribute
    {
        /// <summary>
        /// Gets or sets a default value to use if the specified node isn't found during fetch.
        /// This value is passed to the type converter to create an instance of the target object.
        /// </summary>
        public string Default { get; set; }

        internal override object FetchValue(Element element, object target, TypeCache typeCache, Cache cache)
        {
            Text child;
            if (!GetNodeFromQuery(Query, null, element, out child))
            {
                child = element.Children.OfType<Text>().FirstOrDefault();
            }
            return child == null ? null : GetObjectFromString(child.Value, Default, target, typeCache.Type);
        }

        internal override object SerializeValue(object source, TypeCache typeCache, Cache cache)
        {
            return GetStringFromObject(source, typeCache.Type);
        }

        internal override void StoreValue(Element element, object serialized, object source, TypeCache typeCache, Cache cache)
        {
            Text child;
            if (!GetNodeFromQuery(Query, CreateQuery, element, out child))
            {
                child = element.Children.OfType<Text>().FirstOrDefault();
            }
            else if (child == null)
            {
                return;
            }

            string value = (string) serialized;
            if (child == null && value != null)
            {
                element.Append(new Text(value));
            }
            else if (child != null && value == null)
            {
                child.Remove();
            }
            else if (child != null)
            {
                child.Value = value;
            }
        }
    }
}
