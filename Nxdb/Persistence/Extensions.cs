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
    /// Provides extension methods for the persistence framework.
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

        public static IEnumerable<T> GetObjects<T>(this Element element, string expression) where T : class
        {
            return Manager.Default.GetObjects<T>(element, expression);
        }

        public static IEnumerable<T> GetObjects<T>(this Element element, string expression,
            bool attach, bool attachItems) where T : class
        {
            return Manager.Default.GetObjects<T>(element, expression, attach, attachItems);
        }

        public static void Attach(this object obj, Element element)
        {
            Manager.Default.Attach(obj, element);
        }

        public static void Attach(this Element element, object obj)
        {
            Manager.Default.Attach(obj, element);
        }

        public static void Append(this object obj, Element parent)
        {
            Manager.Default.Append(obj, parent);
        }

        public static void Append(this object obj, Element parent, string elementName)
        {
            Manager.Default.Append(obj, parent, elementName);
        }

        public static void Append(this object obj, Element parent, string elementName, bool attach)
        {
            Manager.Default.Append(obj, parent, elementName, attach);
        }

        public static void Append(this Element parent, object obj)
        {
            Manager.Default.Append(obj, parent);
        }

        public static void Append(this Element parent, object obj, string elementName)
        {
            Manager.Default.Append(obj, parent, elementName);
        }

        public static void Append(this Element parent, object obj, string elementName, bool attach)
        {
            Manager.Default.Append(obj, parent, elementName, attach);
        }

        public static void Detach(this object obj)
        {
            Manager.Default.Detach(obj);
        }

        public static void Fetch(this object obj, Element element)
        {
            Manager.Default.Fetch(obj, element);
        }

        public static void Fetch(this Element element, object obj)
        {
            Manager.Default.Fetch(obj, element);
        }

        public static void Fetch(this object obj)
        {
            Manager.Default.Fetch(obj);
        }

        public static void Store(this object obj, Element element)
        {
            Manager.Default.Store(obj, element);
        }

        public static void Store(this Element element, object obj)
        {
            Manager.Default.Store(obj, element);
        }

        public static void Store(this object obj)
        {
            Manager.Default.Store(obj);
        }
    }
}
