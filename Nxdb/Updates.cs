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
    /// Encapsulates all update operations, including those from direct node manipulation as well as
    /// XQuery Update evaluation. Instances of this class are nested to represent multiple
    /// update operations. When the outer-most Updates instance is disposed, all pending updates are
    /// automatically applied.
    /// </summary>
    public class Updates : IDisposable
    {
        private static int _counter = 0;
        private static QueryContext QueryContext = GetQueryContext();

        private static QueryContext GetQueryContext()
        {
            QueryContext queryContext = new QueryContext(Database.Context);
            queryContext.updating(true);    // Need to explicitly indicate the context can update
            return queryContext;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Updates"/> class. Make sure to dispose the
        /// instance when the encapsulated update operations are complete.
        /// </summary>
        public Updates()
        {
            Begin();
        }

        // Used by Query when evaluating a query which updates the query context
        internal static org.basex.query.up.Updates QueryUpdates
        {
            get { return QueryContext.updates; }
        }

        /// <summary>
        /// Begins a set of update operations.
        /// </summary>
        public static void Begin()
        {
            _counter++;
        }

        /// <summary>
        /// Ends a set of update operations.
        /// </summary>
        public static void End()
        {
            _counter--;
            Apply(false);
        }

        /// <summary>
        /// Disposes this Updates instance. If this is the outer-most Updates instance, all pending
        /// updates will be applied.
        /// </summary>
        public void Dispose()
        {
            End();
        }
        
        //Adds an update primitive to the sequence of operations
        internal static void Add(UpdatePrimitive update)
        {
            //Add the update to the query context
            QueryContext.updates.add(update, QueryContext);

            //If a context isn't open, apply the update immediatly
            Apply(false);
        }

        internal static void Add(Expr expr)
        {
            //Execute the function (which may implicity add to the query context)
            expr.item(QueryContext, null);

            // If a context isn't open, apply the update immediatly
            Apply(false);
        }

        /// <summary>
        /// Forces all pending updates to be applied, regardless of Updates nesting level.
        /// </summary>
        public static void Apply()
        {
            Apply(true);
        }

        private static void Apply(bool force)
        {
            if (force || _counter <= 0)
            {
                // Check if there are any updates to perform (if not, updates.mod will be null)
                if (QueryContext.updates != null && QueryContext.updates.mod != null)
                {
                    // Get all updating databases
                    List<Database> databases
                        = QueryContext.updates.mod.datas().Select(Database.Get).ToList();

                    // Apply the updates
                    QueryContext.updates.applyUpdates();

                    // Update databases
                    foreach (Database database in databases)
                    {
                        // Update database node cache
                        database.Update();

                        // Optimize database(s)
                        if (Properties.OptimizeAfterUpdates)
                        {
                            Optimize.optimize(database.Data);
                        }
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
            UnsyncReset();
        }

        private static void UnsyncReset()
        {
            QueryContext = GetQueryContext();
            _counter = 0;
        }
    }
}
