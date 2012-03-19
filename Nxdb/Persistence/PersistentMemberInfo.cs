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
using Nxdb.Persistence.Attributes;

namespace Nxdb.Persistence
{
    // Encapsulates member information in a consistent interface
    internal class PersistentMemberInfo
    {
        public readonly Type Type;
        public readonly string Name;
        public readonly PersistentMemberAttribute Attribute;
        public readonly Func<object, object> GetValue;
        public readonly Action<object, object> SetValue;

        public PersistentMemberInfo(Type type, string name,
            PersistentMemberAttribute attribute,
            Func<object, object> getValue, Action<object, object> setValue)
        {
            Type = type;
            Name = name;
            Attribute = attribute;
            GetValue = getValue;
            SetValue = setValue;
        }
    }
}
