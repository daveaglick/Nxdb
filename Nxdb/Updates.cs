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
    public class Updates : IDisposable
    {
        private static int _counter = 0;
        private static QueryContext _queryContext = new QueryContext(Database.Context);
        
        public Updates()
        {
            Begin();
        }

        internal static org.basex.query.up.Updates QueryUpdates
        {
            get { return _queryContext.updates; }
        }

        public static void Begin()
        {
            _counter++;
        }

        public static void End()
        {
            _counter--;
            Apply(false);
        }

        public void Dispose()
        {
            End();
        }
        
        //Adds an update primitive to the sequence of operations
        internal static void Add(UpdatePrimitive update)
        {
            //Add the update to the query context
            _queryContext.updates.add(update, _queryContext);

            //If a context isn't open, apply the update immediatly
            Apply(false);
        }

        internal static void Add(Expr expr)
        {
            //Execute the function (which may implicity add to the query context)
            expr.item(_queryContext, null);

            // If a context isn't open, apply the update immediatly
            Apply(false);
        }

        // Forces update application regardless of update nesting level, resets updates
        public static void Apply()
        {
            Apply(true);
        }

        private static void Apply(bool force)
        {
            if (force || _counter <= 0)
            {
                // Check if there are any updates to perform (if not, updates.mod will be null)
                if (_queryContext.updates.mod != null)
                {
                    // Apply the updates
                    _queryContext.updates.applyUpdates();

                    // Update databases
                    bool optimize = Properties.OptimizeAfterUpdates.Get();
                    foreach (Data data in _queryContext.updates.mod.datas())
                    {
                        // Update database node cache
                        Database.Get(data).Update();

                        // Optimize database(s)
                        if (optimize)
                        {
                            Optimize.optimize(data);
                        }
                    }
                }

                // Reset
                Reset();
            }
        }

        /// <summary>
        /// Resets the update stack and forgets all uncommitted updates.
        /// </summary>
        public static void Reset()
        {
            _queryContext = new QueryContext(Database.Context);
            _counter = 0;
        }
    }
}
