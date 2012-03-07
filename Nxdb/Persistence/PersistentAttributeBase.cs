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
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using Nxdb.Node;
using Attribute = System.Attribute;

namespace Nxdb.Persistence
{
    /// <summary>
    /// The base class for all persistent attributes.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public abstract class PersistentAttributeBase : Attribute
    {
        protected PersistentAttributeBase()
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
        /// Allows derived classes to initialze state based on attached member.
        /// </summary>
        internal virtual void Inititalize(MemberInfo memberInfo)
        {
        }

        // Returns null if the requested node does not exist, in which case the Default should be used (if available)
        internal abstract string FetchValue(Element element);

        internal abstract void StoreValue(Element element, string value);
    }
}
