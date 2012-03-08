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

namespace Nxdb.Persistence.Attributes
{
    /// <summary>
    /// Base class for persistent attributes that use a name.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public abstract class NamedPersistentAttribute : StringBasedPersistentAttribute
    {
        /// <summary>
        /// Gets or sets the name to use or create. If unspecified, the name of
        /// the field or property will be used (as converted to a valid XML name).
        /// </summary>
        public string Name { get; set; }

        internal override void Inititalize(MemberInfo memberInfo)
        {
            base.Inititalize(memberInfo);
            Name = GetName(Name, memberInfo.Name);
        }
    }
}
