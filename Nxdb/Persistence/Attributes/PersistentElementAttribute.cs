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
    public class PersistentElementAttribute : NamedPersistentMemberAttribute
    {
        public PersistentElementAttribute() : base()
        {
        }

        public PersistentElementAttribute(string name) : base()
        {
        }

        internal override string FetchValue(Element element)
        {
            Element child = element.Children.OfType<Element>().Where(e => e.Name.Equals(Name)).FirstOrDefault();
            return child == null ? null : child.Value;
        }

        internal override void StoreValue(Element element, string value)
        {
            Element child = element.Children.OfType<Element>().Where(e => e.Name.Equals(Name)).FirstOrDefault();
            if (child == null)
            {
                element.Append(String.Format("<{0}>{1}</{0}>", Name, value));
            }
            else
            {
                child.Value = value;
            }
        }
    }
}
