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
    //TODO: How should this interact with updating queries? How should queries trigger optimization?
    //TODO: Should Update cache/compare database time to see if update actually occured?
    public class Update : IDisposable
    {
        private static int _counter = 0;
        private static QueryContext _queryContext = new QueryContext(Database.Context);
        
        public Update()
        {
            _counter++;
        }
        
        //Adds an update primitive to the sequence of operations
        internal static void Add(UpdatePrimitive update)
        {
            //Add the update to the query context
            _queryContext.updates.add(update, _queryContext);

            //If a context isn't open, apply the update immediatly
            ApplyUpdates();
        }

        internal static void Add(Expr expr)
        {
            //Execute the function (which may implicity add to the query context)
            expr.item(_queryContext, null);

            //If a context isn't open, apply the update immediatly
            ApplyUpdates();
        }

        private static void ApplyUpdates()
        {
            if (_counter <= 0)
            {
                //Apply the updates
                _queryContext.updates.applyUpdates();

                //Optimize database(s)
                //TODO: Make this an option or a flag
                bool optimize = false;
                if (_queryContext.updates.mod != null && optimize)
                {
                    //Loop through each database used and optimize
                    foreach (Data data in _queryContext.updates.mod.datas())
                    {
                        Optimize.optimize(data);
                    }
                }

                //Reset
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

        public void Dispose()
        {
            _counter--;
            ApplyUpdates();
        }
    }
}
