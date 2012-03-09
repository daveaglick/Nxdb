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
    internal static class Updates
    {
        private static QueryContext _qContext = GetQueryContext();

        private static QueryContext GetQueryContext()
        {
            QueryContext queryContext = new QueryContext(Database.Context);
            queryContext.updating(true);    // Need to explicitly indicate the context can update
            return queryContext;
        }

        internal static QueryContext QueryContext
        {
            get
            {
                return _qContext;
            }
        }

        // Used by Query when evaluating a query which updates the query context
        internal static org.basex.query.up.Updates QueryUpdates
        {
            get
            {
                if (_qContext == null) throw new ObjectDisposedException("Updates");
                return _qContext.updates;
            }
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
            _qContext = GetQueryContext();
        }

        /// <summary>
        /// Forgets all uncommitted updates.
        /// </summary>
        public static void Forget()
        {
            _qContext = GetQueryContext();
        }
    }
}
