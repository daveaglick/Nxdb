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
using Nxdb.Node;

namespace Nxdb.Persistence
{
    /// <summary>
    /// Implement this interface to provide custom persistence initialization logic.
    /// The custom persistence method will be called after any persistence attributes
    /// are processed.
    /// </summary>
    public interface ICustomInitialize
    {
        /// <summary>
        /// Initializes a persistent object after construction. Since the object must
        /// have an empty default constructor and none of the persistent members are
        /// populated at construction, this allows the object to provide more complete
        /// initialization that uses persistent members if required.
        /// </summary>
        void Initialize(Element element);
    }
}
