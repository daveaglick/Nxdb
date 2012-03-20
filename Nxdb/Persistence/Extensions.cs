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
    /// Provides extension methods for the object persistence framework. These extension methods
    /// operate on the Manager.Default persistence manager. If other persistence managers are used,
    /// their methods will need to be called directly.
    /// </summary>
    public static class Extensions
    {
        public static T GetObject<T>(this Element element) where T : class
        {
            return Manager.Default.GetObject<T>(element);
        }

        public static T GetObject<T>(this Element element, bool attach) where T : class
        {
            return Manager.Default.GetObject<T>(element, attach);
        }

        public static Element GetElement(this object attachedObject)
        {
            return Manager.Default.GetElement(attachedObject);
        }

        public static void Attach(this object target, Element element)
        {
            Manager.Default.Attach(target, element);
        }

        public static void Attach(this Element element, object target)
        {
            Manager.Default.Attach(target, element);
        }

        public static void Append(this object source, Element parent)
        {
            Manager.Default.Append(source, parent);
        }

        public static void Append(this object source, Element parent, string elementName)
        {
            Manager.Default.Append(source, parent, elementName);
        }

        public static void Append(this object source, Element parent, string elementName, bool attach)
        {
            Manager.Default.Append(source, parent, elementName, attach);
        }

        public static void Append(this Element parent, object source)
        {
            Manager.Default.Append(source, parent);
        }

        public static void Append(this Element parent, object source, string elementName)
        {
            Manager.Default.Append(source, parent, elementName);
        }

        public static void Append(this Element parent, object source, string elementName, bool attach)
        {
            Manager.Default.Append(source, parent, elementName, attach);
        }

        public static void Detach(this object obj)
        {
            Manager.Default.Detach(obj);
        }

        public static void Fetch(this object target, Element element)
        {
            Manager.Default.Fetch(target, element);
        }

        public static void Fetch(this Element element, object target)
        {
            Manager.Default.Fetch(target, element);
        }

        public static void Fetch(this object target)
        {
            Manager.Default.Fetch(target);
        }

        public static void Store(this object source, Element element)
        {
            Manager.Default.Store(source, element);
        }

        public static void Store(this Element element, object source)
        {
            Manager.Default.Store(source, element);
        }

        public static void Store(this object source)
        {
            Manager.Default.Store(source);
        }
    }
}
