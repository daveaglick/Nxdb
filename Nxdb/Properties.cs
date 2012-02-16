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
    public static class Properties
    {
        public static bool ChopWhitespace
        {
            get { using(Database.GlobalReadLock()) return Is(Database.Context.prop, Prop.CHOP); }
            set { using (Database.GlobalWriteLock()) Set(Database.Context.prop, Prop.CHOP, value); }
        }

        public static bool UsePathIndex
        {
            get { using(Database.GlobalReadLock()) return Is(Database.Context.prop, Prop.PATHINDEX); }
            set { using (Database.GlobalWriteLock()) Set(Database.Context.prop, Prop.PATHINDEX, value); }
        }

        public static bool UseTextIndex
        {
            get { using(Database.GlobalReadLock()) return Is(Database.Context.prop, Prop.TEXTINDEX); }
            set { using (Database.GlobalWriteLock()) Set(Database.Context.prop, Prop.TEXTINDEX, value); }
        }

        public static bool UseAttributeIndex
        {
            get { using(Database.GlobalReadLock()) return Is(Database.Context.prop, Prop.ATTRINDEX); }
            set { using (Database.GlobalWriteLock()) Set(Database.Context.prop, Prop.ATTRINDEX, value); }
        }

        public static bool UseFullTextIndex
        {
            get { using(Database.GlobalReadLock()) return Is(Database.Context.prop, Prop.FTINDEX); }
            set { using (Database.GlobalWriteLock()) Set(Database.Context.prop, Prop.FTINDEX, value); }
        }

        public static bool UpdateIndexes
        {
            get { using(Database.GlobalReadLock()) return Is(Database.Context.prop, Prop.UPDINDEX); }
            set { using (Database.GlobalWriteLock()) Set(Database.Context.prop, Prop.UPDINDEX, value); }
        }

        public static bool DropOnDispose
        {
            get { using(Database.GlobalReadLock()) return Is(Database.Properties, NxdbProp.DROPONDISPOSE); }
            set { using (Database.GlobalWriteLock()) Set(Database.Properties, NxdbProp.DROPONDISPOSE, value); }
        }

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

    public class NxdbProp : AProp
    {
        public static readonly object[] DROPONDISPOSE = { "DROPONDISPOSE", new java.lang.Boolean(false) };
        public static readonly object[] OPTIMIZEAFTERUPDATES = { "OPTIMIZEAFTERUPDATES", new java.lang.Boolean(true) };

        internal NxdbProp() : base(".nxdb")
        {
        }
    }
}
