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

namespace Nxdb.Persistence
{
    public class DataContractSerializerPersistenceAttribute : PersistenceAttribute
    {
        private DataContractSerializerBehavior _behavior = null;

        internal override PersistenceBehavior Behavior
        {
            get
            {
                if (_behavior == null)
                {
                    _behavior = new DataContractSerializerBehavior();
                }
                return _behavior;
            }
        }
    }

    public class DataContractSerializerBehavior : PersistenceBehavior
    {
        internal override void Fetch(Element element, object obj, TypeCache typeCache)
        {
            throw new NotImplementedException();
        }

        internal override void Store(Element element, object obj, TypeCache typeCache)
        {
            throw new NotImplementedException();
        }
    }
}
