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
using org.basex.core;

namespace Nxdb
{
    /// <summary>
    /// Static class used to get and set global properties.
    /// </summary>
    public static class Properties
    {
        /// <summary>
        /// Gets or sets a value indicating whether whitespace should be chopped (removed).
        /// </summary>
        public static bool ChopWhitespace
        {
            get { using(Database.GlobalReadLock()) return Is(Database.Context.prop, Prop.CHOP); }
            set { using (Database.GlobalWriteLock()) Set(Database.Context.prop, Prop.CHOP, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether a path index should be used.
        /// </summary>
        public static bool UsePathIndex
        {
            get { using(Database.GlobalReadLock()) return Is(Database.Context.prop, Prop.PATHINDEX); }
            set { using (Database.GlobalWriteLock()) Set(Database.Context.prop, Prop.PATHINDEX, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether a text index should be used.
        /// </summary>
        public static bool UseTextIndex
        {
            get { using(Database.GlobalReadLock()) return Is(Database.Context.prop, Prop.TEXTINDEX); }
            set { using (Database.GlobalWriteLock()) Set(Database.Context.prop, Prop.TEXTINDEX, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether an attribute index should be used.
        /// </summary>
        public static bool UseAttributeIndex
        {
            get { using(Database.GlobalReadLock()) return Is(Database.Context.prop, Prop.ATTRINDEX); }
            set { using (Database.GlobalWriteLock()) Set(Database.Context.prop, Prop.ATTRINDEX, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether a full text index should be used.
        /// </summary>
        public static bool UseFullTextIndex
        {
            get { using(Database.GlobalReadLock()) return Is(Database.Context.prop, Prop.FTINDEX); }
            set { using (Database.GlobalWriteLock()) Set(Database.Context.prop, Prop.FTINDEX, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether indexes should be incrementally updated.
        /// </summary>
        public static bool UpdateIndexes
        {
            get { using(Database.GlobalReadLock()) return Is(Database.Context.prop, Prop.UPDINDEX); }
            set { using (Database.GlobalWriteLock()) Set(Database.Context.prop, Prop.UPDINDEX, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether databases should be dropped (deleted from disk) when disposed.
        /// </summary>
        public static bool DropOnDispose
        {
            get { using(Database.GlobalReadLock()) return Is(Database.Properties, NxdbProp.DROPONDISPOSE); }
            set { using (Database.GlobalWriteLock()) Set(Database.Properties, NxdbProp.DROPONDISPOSE, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether databases should be optimized after update operations.
        /// </summary>
        public static bool OptimizeAfterUpdates
        {
            get { using(Database.GlobalReadLock()) return Is(Database.Properties, NxdbProp.OPTIMIZEAFTERUPDATES); }
            set { using (Database.GlobalWriteLock()) Set(Database.Properties, NxdbProp.OPTIMIZEAFTERUPDATES, value); }
        }

        private static bool Is(AProp prop, object[] key)
        {
            return prop.@is(key);
        }

        private static string Get(AProp prop, object[] key)
        {
            return prop.get(key);
        }

        private static void Set(AProp prop, object[] key, bool value)
        {
            prop.set(key, value);
            prop.write();
        }

        private static void Set(AProp prop, object[] key, string value)
        {
            prop.set(key, value);
            prop.write();
        }
    }

    internal class NxdbProp : AProp
    {
        public static readonly object[] DROPONDISPOSE = { "DROPONDISPOSE", new java.lang.Boolean(false) };
        public static readonly object[] OPTIMIZEAFTERUPDATES = { "OPTIMIZEAFTERUPDATES", new java.lang.Boolean(true) };

        internal NxdbProp() : base(".nxdb")
        {
        }
    }
}
