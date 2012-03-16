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
            get { return Is(Database.Context.prop, Prop.CHOP); }
            set { Set(Database.Context.prop, Prop.CHOP, value); }
        }

        /// <summary>
        /// Creates a path index whenever a new database is created.
        /// A path index helps to optimize location paths.
        /// </summary>
        public static bool CreatePathIndex
        {
            get { return Is(Database.Context.prop, Prop.PATHINDEX); }
            set { Set(Database.Context.prop, Prop.PATHINDEX, value); }
        }

        /// <summary>
        /// Creates a text index whenever a new database is created.
        /// A text index speeds up queries with equality comparisons on text nodes.
        /// </summary>
        public static bool CreateTextIndex
        {
            get { return Is(Database.Context.prop, Prop.TEXTINDEX); }
            set { Set(Database.Context.prop, Prop.TEXTINDEX, value); }
        }

        /// <summary>
        /// Creates an attribute index whenever a new database is created.
        /// An attribute index speeds up queries with equality comparisons on attribute values.
        /// </summary>
        public static bool CreateAttributeIndex
        {
            get { return Is(Database.Context.prop, Prop.ATTRINDEX); }
            set { Set(Database.Context.prop, Prop.ATTRINDEX, value); }
        }

        /// <summary>
        /// Creates a full-text index whenever a new database is created.
        /// A full-text index speeds up queries with full-text expressions.
        /// </summary>
        public static bool CreateFullTextIndex
        {
            get { return Is(Database.Context.prop, Prop.FTINDEX); }
            set { Set(Database.Context.prop, Prop.FTINDEX, value); }
        }

        /// <summary>
        /// Specifies the maximum length of strings that are to be indexed by
        /// the name, path, value, and full-text index structures. The value
        /// of this option will be assigned once to a new database, and cannot
        /// be changed after that. 
        /// </summary>
        public static int MaximumStringIndexLength
        {
            get { return Num(Database.Context.prop, Prop.MAXLEN); }
            set { Set(Database.Context.prop, Prop.MAXLEN, value); }
        }

        /// <summary>
        /// Specifies the maximum number of distinct values (categories) that
        /// will be remembered for a particular tag/attribute name or unique
        /// path. The value of this option will be assigned once to a new
        /// database, and cannot be changed after that. 
        /// </summary>
        public static int MaximumIndexCategories
        {
            get { return Num(Database.Context.prop, Prop.MAXCATS); }
            set { Set(Database.Context.prop, Prop.MAXCATS, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether indexes should be incrementally updated.
        /// The value of this option will be assigned once to a new database, and cannot be
        /// changed after that. The advantage of incremental indexes is that the value index
        /// structures will always be up-to-date. The downside is that updates will take a
        /// little bit longer. 
        /// </summary>
        public static bool UpdateIndexes
        {
            get { return Is(Database.Context.prop, Prop.UPDINDEX); }
            set { Set(Database.Context.prop, Prop.UPDINDEX, value); }
        }

        /// <summary>
        /// Flushes database buffers to disk after each update. If this option is set to false,
        /// bulk operations (multiple single updates) will be evaluated faster. As a drawback,
        /// the chance of data loss increases if the database is not explicitly flushed. 
        /// </summary>
        public static bool AutoFlush
        {
            get { return Is(Database.Context.prop, Prop.AUTOFLUSH); }
            set { Set(Database.Context.prop, Prop.AUTOFLUSH, value); }
        }

        /// <summary>
        /// Specifies whether databases should be dropped (deleted from disk) when disposed.
        /// </summary>
        public static bool DropOnDispose
        {
            get { return Is(Database.Properties, NxdbProp.DROPONDISPOSE); }
            set { Set(Database.Properties, NxdbProp.DROPONDISPOSE, value); }
        }

        /// <summary>
        /// Specifies whether databases should be optimized after update operations. If this
        /// is true and an Updates instance exists, optimizations will be performed when the
        /// outer-most Updates instance is disposed. Otherwise, optimizations will be
        /// performed immediately after each update operation. If this is false, the database
        /// will not be implicitly optimized and Database.Optimize() must be called.
        /// </summary>
        public static bool OptimizeAfterUpdates
        {
            get { return Is(Database.Properties, NxdbProp.OPTIMIZEAFTERUPDATES); }
            set { Set(Database.Properties, NxdbProp.OPTIMIZEAFTERUPDATES, value); }
        }

        /// <summary>
        /// Specifies whether databases should be flushed after update operations. This is only
        /// used if AutoFlush is false. If this is true and an Updates instance exists,
        /// a flush will be performed when the outer-most Updates instance is disposed. Otherwise,
        /// a flush will be performed immediately after each update operation. If this is false,
        /// the database will not be implicitly flushed (if AutoFlush is false) and Database.Flush()
        /// must be called.
        /// </summary>
        public static bool FlushAfterUpdates
        {
            get { return Is(Database.Properties, NxdbProp.FLUSHAFTERUPDATES); }
            set { Set(Database.Properties, NxdbProp.FLUSHAFTERUPDATES, value); }
        }

        private static bool Is(AProp prop, object[] key)
        {
            return prop.@is(key);
        }

        private static string Get(AProp prop, object[] key)
        {
            return prop.get(key);
        }

        private static int Num(AProp prop, object[] key)
        {
            return prop.num(key);
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

        private static void Set(AProp prop, object[] key, int value)
        {
            prop.set(key, value);
            prop.write();
        }
    }

    /// <summary>
    /// Properties container for Nxdb-specific properties. Intended for BaseX - not to be used directly by Nxdb users.
    /// </summary>
    public class NxdbProp : AProp
    {
        public static readonly object[] DROPONDISPOSE = { "DROPONDISPOSE", new java.lang.Boolean(false) };
        public static readonly object[] OPTIMIZEAFTERUPDATES = { "OPTIMIZEAFTERUPDATES", new java.lang.Boolean(true) };
        public static readonly object[] FLUSHAFTERUPDATES = { "FLUSHAFTERUPDATES", new java.lang.Boolean(true) };

        internal NxdbProp() : base(".nxdb")
        {
        }
    }
}
