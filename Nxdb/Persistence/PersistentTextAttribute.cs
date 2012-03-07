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
    /// Stores and fetches the field or property to/from a text node of the container element. This attribute
    /// can only be applied to one field or property (if it is applied more than once and exception will be thrown).
    /// If the container element contains more than one text node (such as with mixed content elements), only the
    /// first text node will be used.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class PersistentTextAttribute : PersistentAttributeBase
    {
        internal override string FetchValue(Element element)
        {
            Text child = element.Children.OfType<Text>().FirstOrDefault();
            return child == null ? null : child.Value;
        }

        internal override void StoreValue(Element element, string value)
        {
            Text child = element.Children.OfType<Text>().FirstOrDefault();
            if(child == null)
            {
                element.Append(new Text(value));
            }
            else
            {
                child.Value = value;
            }
        }
    }
}
