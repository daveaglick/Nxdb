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
    public static class PersistenceExtensions
    {
        public static T GetObject<T>(this Element element) where T : class
        {
            return PersistenceManager.Default.GetObject<T>(element);
        }

        public static T GetObject<T>(this Element element, bool attach, bool searchCache) where T : class
        {
            return PersistenceManager.Default.GetObject<T>(element, attach, searchCache);
        }

        public static IEnumerable<T> GetObjects<T>(this Element element, string expression) where T : class
        {
            return PersistenceManager.Default.GetObjects<T>(element, expression);
        }

        public static IEnumerable<T> GetObjects<T>(this Element element, string expression,
            bool attach, bool searchCache, bool attachResults) where T : class
        {
            return PersistenceManager.Default.GetObjects<T>(element, expression, attach, searchCache, attachResults);
        }

        public static void Attach(this object obj, Element element)
        {
            PersistenceManager.Default.Attach(obj, element);
        }

        public static void Attach(this Element element, object obj)
        {
            PersistenceManager.Default.Attach(obj, element);
        }

        public static void Append(this object obj, Element parent)
        {
            PersistenceManager.Default.Append(obj, parent);
        }

        public static void Append(this object obj, Element parent, string elementName)
        {
            PersistenceManager.Default.Append(obj, parent, elementName);
        }

        public static void Append(this object obj, Element parent, string elementName, bool attach)
        {
            PersistenceManager.Default.Append(obj, parent, elementName, attach);
        }

        public static void Append(this Element parent, object obj)
        {
            PersistenceManager.Default.Append(obj, parent);
        }

        public static void Append(this Element parent, object obj, string elementName)
        {
            PersistenceManager.Default.Append(obj, parent, elementName);
        }

        public static void Append(this Element parent, object obj, string elementName, bool attach)
        {
            PersistenceManager.Default.Append(obj, parent, elementName, attach);
        }

        public static void Detach(this object obj)
        {
            PersistenceManager.Default.Detach(obj);
        }

        public static void Fetch(this object obj, Element element)
        {
            PersistenceManager.Default.Fetch(obj, element);
        }

        public static void Fetch(this Element element, object obj)
        {
            PersistenceManager.Default.Fetch(obj, element);
        }

        public static void Fetch(this object obj)
        {
            PersistenceManager.Default.Fetch(obj);
        }

        public static void Store(this object obj, Element element)
        {
            PersistenceManager.Default.Store(obj, element);
        }

        public static void Store(this Element element, object obj)
        {
            PersistenceManager.Default.Store(obj, element);
        }

        public static void Store(this object obj)
        {
            PersistenceManager.Default.Store(obj);
        }
    }
}
