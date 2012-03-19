﻿/*
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

namespace Nxdb.Persistence.Attributes
{
    /// <summary>
    /// Indicates that the class this is applied to should use the default persister.
    /// Normally this does not have to be explicitly added as classes without a persister
    /// attribute will always use the default persister.
    /// </summary>
    public class DefaultPersisterAttribute : PersisterAttribute
    {
        private readonly DefaultPersister _persister = new DefaultPersister();

        internal override Persister Persister
        {
            get { return _persister; }
        }
    }
}
