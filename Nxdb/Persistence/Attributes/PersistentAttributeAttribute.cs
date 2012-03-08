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
    /// Stores and fetches the field or property to/from an attribute of the container element.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class PersistentAttributeAttribute : NamedPersistentAttribute
    {
        protected override string FetchValue(Element element)
        {
            element = GetElementFromQuery(element);
            if (element == null) return null;

            Node.Attribute attribute = element.Attribute(Name);
            return attribute == null ? null : attribute.Value;
        }

        protected override void StoreValue(Element element, string value)
        {
            element = GetElementFromQuery(element);
            if (element == null) return;

            Node.Attribute attribute = element.Attribute(Name);
            if (attribute == null)
            {
                element.InsertAttribute(Name, value);
            }
            else
            {
                attribute.Value = value;
            }
        }
    }
}
