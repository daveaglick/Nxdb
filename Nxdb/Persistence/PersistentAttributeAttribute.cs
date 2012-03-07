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

namespace Nxdb.Persistence
{
    /// <summary>
    /// Stores and fetches the field or property to/from an attribute of the container element.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class PersistentAttributeAttribute : PersistentAttributeBase
    {
        public PersistentAttributeAttribute()
        {
        }

        public PersistentAttributeAttribute(string name)
        {
            Name = name;
        }

        /// <summary>
        /// Gets or sets the name of the attribute to use or create. If unspecified, the name of
        /// the field or property will be used (as converted to a valid XML name).
        /// </summary>
        public string Name { get; set; }

        internal override void Inititalize(MemberInfo memberInfo)
        {
            base.Inititalize(memberInfo);

            if (String.IsNullOrEmpty(Name))
            {
                Name = XmlConvert.EncodeName(memberInfo.Name);
            }
            else
            {
                XmlConvert.VerifyName(Name);
            }
        }

        internal override string FetchValue(Element element)
        {
            Node.Attribute attribute = element.Attribute(Name);
            return attribute == null ? null : attribute.Value;
        }

        internal override void StoreValue(Element element, string value)
        {
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
