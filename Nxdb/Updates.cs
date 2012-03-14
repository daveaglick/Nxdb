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
using org.basex.core.cmd;
using org.basex.data;
using org.basex.query;
using org.basex.query.expr;
using org.basex.query.func;
using org.basex.query.up;
using org.basex.query.up.primitives;

namespace Nxdb
{
    /// <summary>
    /// Instances of this class should be placed around combined update operations. It's primary
    /// purpose is to prevent intermediate and premature database optimization which will greatly
    /// impact performance. Instances of this class may be nested. If an update operation (either
    /// directly or through a query) is applied while one or more instances of this class are
    /// active, database optimizations and reindexing will not be performed until the final instance
    /// of this class is disposed. Note that instances of this class do not affect the actual
    /// application of updates operations which are still applied immediatly regardless.
    /// </summary>
    public class Updates : IDisposable
    {
        private static readonly Stack<Updates> UpdatesStack = new Stack<Updates>();
        private static readonly HashSet<Database> NeedsOptimize = new HashSet<Database>(); 
        private static QueryContext _queryContext = GetQueryContext();

        private bool _disposed = false;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="Updates"/> class.
        /// </summary>
        public Updates()
        {
            UpdatesStack.Push(this);
        }

        public void Dispose()
        {
            if (_disposed) return;
            while(UpdatesStack.Count > 0 && UpdatesStack.Pop() != this) {}
            Optimize(null);
            _disposed = true;
        }
        
        private static QueryContext GetQueryContext()
        {
            QueryContext queryContext = new QueryContext(Database.Context);
            queryContext.updating(true);    // Need to explicitly indicate the context can update
            return queryContext;
        }

        internal static QueryContext QueryContext
        {
            get { return _queryContext; }
        }

        // Used by Query when evaluating a query which updates the query context
        internal static org.basex.query.up.Updates QueryUpdates
        {
            get { return _queryContext.updates; }
        }

        internal static void Do(UpdatePrimitive update)
        {
            QueryUpdates.add(update, QueryContext);
            Apply();
        }

        internal static void Do(Expr expr)
        {
            expr.item(QueryContext, null);
            Apply();
        }

        // Applies pending updates - used from the query class after a query
        internal static void Apply()
        {
            // Check if there are any updates to perform (if not, updates.mod will be null)
            if (QueryContext.updates != null && QueryContext.updates.mod != null)
            {
                // Get all updating databases
                List<Database> databases = QueryUpdates.mod.datas().Select(Database.Get).ToList();

                // Apply the updates
                QueryContext.updates.applyUpdates();

                // Optimize databases
                Optimize(databases);

                // Update databases
                foreach (Database database in databases)
                {
                    database.Update();
                }
            }

            // Reset
            _queryContext = GetQueryContext();
        }

        // Optimizes all databases that need it, but only if no Updates instances exist
        private static void Optimize(IEnumerable<Database> databases)
        {
            if (UpdatesStack.Count == 0)
            {   
                // Not in an Updates instance
                if (Properties.OptimizeAfterUpdates)
                {
                    // Optimization after updates is enabled
                    if( NeedsOptimize.Count == 0 )
                    {
                        // No other databases in the optimization queue
                        if (databases != null)
                        {
                            // Currently have databases to optimize
                            foreach (Database database in databases)
                            {
                                // Optimize the databases
                                org.basex.core.cmd.Optimize.optimize(database.Data);
                            }
                        }
                    }
                    else
                    {
                        // Other databases in the optimization queue
                        if (databases != null)
                        {
                            // And current databases - combine them with the queue first
                            // (so we don't get duplicates and optimize a database twice)
                            foreach (Database database in databases)
                            {
                                NeedsOptimize.Add(database);
                            }
                        }

                        // Optimize pending and current databases
                        foreach (Database database in NeedsOptimize.Where(d => d.Data != null))
                        {
                            org.basex.core.cmd.Optimize.optimize(database.Data);
                        }
                    }
                }

                // We've optimized all pending and current databases, so clear the queue
                NeedsOptimize.Clear();
            }
            else if(databases != null)
            {
                // In an Updates instance, just add the new databases to the optimization queue
                foreach (Database database in databases)
                {
                    NeedsOptimize.Add(database);
                }
            }
        }

        /// <summary>
        /// Forgets all uncommitted updates. Used from the Query class
        /// when updates are not allowed (directly added updates are always
        /// immediatly applied, so it is impossible to forget them).
        /// </summary>
        internal static void Forget()
        {
            _queryContext = GetQueryContext();
        }
    }
}
