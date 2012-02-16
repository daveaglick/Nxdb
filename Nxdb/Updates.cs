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
using System.Threading;
using NiceThreads;
using org.basex.core;
using org.basex.core.cmd;
using org.basex.data;
using org.basex.query;
using org.basex.query.expr;
using org.basex.query.func;
using org.basex.query.up;
using org.basex.query.up.primitives;

namespace Nxdb
{
    public class Updates : IDisposable
    {
        private static int _counter = 0;
        private static readonly SyncObject<QueryContext> _queryContext
            = new SyncObject<QueryContext>(new QueryContext(Database.Context));
        
        public Updates()
        {
            Begin();
        }

        // Used by Query when evaluating a query which updates the query context
        internal static IDisposable WriteLock()
        {
            return _queryContext.WriteLock();
        }

        // Used by Query when evaluating a query which updates the query context
        internal static org.basex.query.up.Updates QueryUpdates
        {
            get { return _queryContext.Unsync.updates; }
        }

        public static void Begin()
        {
            _queryContext.DoWrite(q => _counter++);
        }

        public static void End()
        {
            using (_queryContext.WriteLock())
            {
                _counter--;
                Apply(false);
            }
        }

        public void Dispose()
        {
            End();
        }
        
        //Adds an update primitive to the sequence of operations
        internal static void Add(UpdatePrimitive update)
        {
            using (_queryContext.WriteLock())
            {
                //Add the update to the query context
                _queryContext.Unsync.updates.add(update, _queryContext.Unsync);

                //If a context isn't open, apply the update immediatly
                Apply(false);
            }
        }

        internal static void Add(Expr expr)
        {
            using (_queryContext.WriteLock())
            {
                //Execute the function (which may implicity add to the query context)
                expr.item(_queryContext.Unsync, null);

                // If a context isn't open, apply the update immediatly
                Apply(false);
            }
        }

        // Forces update application regardless of update nesting level, resets updates
        public static void Apply()
        {
            _queryContext.DoWrite(q => Apply(true));
        }

        // This method not thread-safe - relies on callers for locking
        private static void Apply(bool force)
        {
            if (force || _counter <= 0)
            {
                // Check if there are any updates to perform (if not, updates.mod will be null)
                if (_queryContext.Unsync.updates.mod != null)
                {
                    //Need to get the optimize property before locking
                    bool optimize = Properties.OptimizeAfterUpdates;

                    // Get and lock all updating databases
                    List<KeyValuePair<Database, WriteLock>> databases
                        = new List<KeyValuePair<Database, WriteLock>>();
                    foreach (Data data in _queryContext.Unsync.updates.mod.datas())
                    {
                        Database database = Database.Get(data);
                        databases.Add(new KeyValuePair<Database, WriteLock>(
                            database, database.WriteLock()));
                    }

                    // Apply the updates
                    _queryContext.Unsync.updates.applyUpdates();

                    // Update databases
                    foreach (KeyValuePair<Database, WriteLock> kvp in databases)
                    {
                        // Update database node cache
                        kvp.Key.Update();

                        // Optimize database(s)
                        if (optimize)
                        {
                            Optimize.optimize(kvp.Key.Data);
                        }

                        // we're done, unlock the database
                        kvp.Value.Dispose();
                    }
                }

                // Reset
                UnsyncReset();
            }
        }

        /// <summary>
        /// Resets the update stack and forgets all uncommitted updates.
        /// </summary>
        public static void Reset()
        {
            _queryContext.DoWrite(q => UnsyncReset());
        }

        // This method not thread-safe - relies on callers for locking
        private static void UnsyncReset()
        {
            _queryContext.Unsync = new QueryContext(Database.Context);
            _counter = 0;
        }
    }
}
