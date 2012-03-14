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
    /// Stores and fetches the field or property to/from an attribute of the container element.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class PersistentAttributeAttribute : PersistentMemberAttribute
    {
        /// <summary>
        /// Gets or sets the name of the attribute to use or create. If unspecified,
        /// the name of the field or property will be used (as converted to a valid
        /// XML name). This is exclusive with Query and both may not be specified.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets a default value to use if the specified node isn't found during fetch.
        /// This value is passed to the type converter to create an instance of the target object.
        /// </summary>
        public string Default { get; set; }

        internal override void Inititalize(MemberInfo memberInfo, Cache cache)
        {
            base.Inititalize(memberInfo, cache);
            Name = GetName(Name, memberInfo.Name, Query, CreateQuery);
        }

        internal override object FetchValue(Element element, object target, TypeCache typeCache, Cache cache)
        {
            Node.Attribute attribute;
            if (!GetNodeFromQuery(Query, null, element, out attribute))
            {
                attribute = element.Attribute(Name);
            }
            return attribute == null ? null : GetObjectFromString(attribute.Value, Default, target, typeCache.Type);
        }

        internal override object SerializeValue(object source, TypeCache typeCache, Cache cache)
        {
            return GetStringFromObject(source, typeCache.Type);
        }

        internal override void StoreValue(Element element, object serialized, object source, TypeCache typeCache, Cache cache)
        {
            Node.Attribute attribute;
            if (!GetNodeFromQuery(Query, CreateQuery, element, out attribute))
            {
                attribute = element.Attribute(Name);
            }
            else if (attribute == null)
            {
                return;
            }

            string value = (string) serialized;
            if (attribute == null && value != null)
            {
                element.InsertAttribute(Name, value);
            }
            else if (attribute != null && value == null)
            {
                attribute.Remove();
            }
            else if (attribute != null)
            {
                attribute.Value = value;
            }
        }
    }
}
