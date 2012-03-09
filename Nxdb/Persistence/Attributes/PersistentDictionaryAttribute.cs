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
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class PersistentDictionaryAttribute : PersistentMemberAttribute
    {
        public string Name { get; set; }

        public string ItemName { get; set; }

        public string KeyAttributeName { get; set; }

        public string KeyElementName { get; set; }

        public string ValueAttributeName { get; set; }

        public string ValueElementName { get; set; }

        internal override void Inititalize(System.Reflection.MemberInfo memberInfo)
        {
            base.Inititalize(memberInfo);

            // TODO: Verify/convert all the names as appropriate
            // TODO: Make sure we don't have both KeyAttributeName and KeyElementName and Value...
        }

        internal override object FetchValue(Element element, object target, TypeCache typeCache, Cache cache)
        {
            throw new NotImplementedException();
        }

        internal override object SerializeValue(object source, TypeCache typeCache, Cache cache)
        {
            throw new NotImplementedException();
        }

        internal override void StoreValue(Element element, object serialized, object source, TypeCache typeCache, Cache cache)
        {
            throw new NotImplementedException();
        }
    }
}
