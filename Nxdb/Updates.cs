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
        private static readonly Stack<Updates> UpdateStack = new Stack<Updates>();
        private static readonly Queue<org.basex.query.up.Updates> PendingQueue
            = new Queue<org.basex.query.up.Updates>();

        private QueryContext _queryContext = GetQueryContext();
        private readonly bool _applyOnDispose = false;

        private static QueryContext GetQueryContext()
        {
            QueryContext queryContext = new QueryContext(Database.Context);
            queryContext.updating(true);    // Need to explicitly indicate the context can update
            return queryContext;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Updates"/> class. Make sure to dispose the
        /// instance when finished.
        /// </summary>
        public Updates()
        {
            UpdateStack.Push(this);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Updates"/> class. Make sure to dispose the
        /// instance when finished.
        /// </summary>
        /// <param name="applyOnDispose">If set to <c>true</c>, updates for this instance are
        /// immediately applied when the instance is disposed.</param>
        public Updates(bool applyOnDispose) : this()
        {
            _applyOnDispose = applyOnDispose;
        }

        internal QueryContext QueryContext
        {
            get
            {
                return _queryContext;
            }
        }

        // Used by Query when evaluating a query which updates the query context
        internal org.basex.query.up.Updates QueryUpdates
        {
            get
            {
                if(_queryContext == null) throw new ObjectDisposedException("Updates");
                return _queryContext.updates;
            }
        }

        /// <summary>
        /// Disposes this Updates instance. If this is the outer-most Updates instance,
        /// all pending updates will be applied.
        /// </summary>
        public void Dispose()
        {
            if (_queryContext == null) return;
            if(_applyOnDispose) Apply();
            while(UpdateStack.Count > 0)
            {
                Updates updates = UpdateStack.Peek();
                if (updates == this)
                {
                    UpdateStack.Pop();
                    break;
                }
                updates.Dispose();
            }
            PendingQueue.Enqueue(QueryUpdates);
            _queryContext = null;
            ApplyPendingUpdates();
        }

        private void ResetQueryContext()
        {
            _queryContext = GetQueryContext();
        }

        // Either gets the outer Updates instance or null
        internal static Updates GetOuterUpdates()
        {
            return UpdateStack.Count > 0 ? UpdateStack.Peek() : null;
        }

        //Adds an update primitive to the sequence of operations
        internal static void Add(UpdatePrimitive update)
        {
            if(UpdateStack.Count > 0)
            {
                // Add to the open operation if there is one
                Updates updates = UpdateStack.Peek();
                updates.QueryUpdates.add(update, updates.QueryContext);
            }
            else
            {
                // Otherwise, just push the new update
                QueryContext queryContext = GetQueryContext();
                queryContext.updates.add(update, queryContext);
                PendingQueue.Enqueue(queryContext.updates);
            }
            ApplyPendingUpdates();
        }

        internal static void Add(Expr expr)
        {
            if (UpdateStack.Count > 0)
            {
                // Add to the open operation if there is one
                Updates updates = UpdateStack.Peek();
                expr.item(updates.QueryContext, null);
            }
            else
            {
                // Otherwise, just push the new update
                QueryContext queryContext = GetQueryContext();
                expr.item(queryContext, null);
                PendingQueue.Enqueue(queryContext.updates);
            }
            ApplyPendingUpdates();
        }

        /// <summary>
        /// Forces all pending updates to be applied, regardless of Updates nesting level.
        /// </summary>
        public static void ApplyAll()
        {
            foreach(Updates updates in UpdateStack.Where(u => u.QueryContext != null))
            {
                PendingQueue.Enqueue(updates.QueryUpdates);
                updates.ResetQueryContext();
            }
            ApplyPendingUpdates();
        }

        /// <summary>
        /// Immediately applies updates for this (and only this) Updates instance.
        /// </summary>
        public void Apply()
        {
            if (_queryContext == null) throw new ObjectDisposedException("Updates");
            HashSet<Database> databases = new HashSet<Database>();
            ApplyUpdates(QueryUpdates, databases);
            ResetQueryContext();
            NotifyDatabases(databases);
        }

        // Applies pending updates, but only if there are no more Updates on the stack
        private static void ApplyPendingUpdates()
        {
            if (UpdateStack.Count != 0) return;
            HashSet<Database> databases = new HashSet<Database>();
            while (PendingQueue.Count > 0)
            {
                ApplyUpdates(PendingQueue.Dequeue(), databases);
            }
            NotifyDatabases(databases);
        }

        private static void ApplyUpdates(org.basex.query.up.Updates updates, HashSet<Database> databases)
        {                    
            // Check if there are any updates to perform (if not, updates.mod will be null)
            if (updates != null && updates.mod != null)
            {
                // Store all updating databases
                foreach (Database database in updates.mod.datas().Select(Database.Get))
                {
                    databases.Add(database);
                }

                // Apply the updates
                updates.applyUpdates();
            }
        }

        private static void NotifyDatabases(IEnumerable<Database> databases)
        {
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

        /// <summary>
        /// Resets the update stack and forgets all uncommitted updates.
        /// </summary>
        public static void ForgetAll()
        {
            // Forget updates in each update on the stack
            foreach (Updates updates in UpdateStack.Where(u => u.QueryContext != null))
            {
                updates.Forget();
            }

            // ...and any pending updates in the queue
            PendingQueue.Clear();
        }

        /// <summary>
        /// Forgets all pending updates for this Updates instance.
        /// </summary>
        public void Forget()
        {
            if (_queryContext == null) throw new ObjectDisposedException("Updates");
            ResetQueryContext();
        }
    }
}
