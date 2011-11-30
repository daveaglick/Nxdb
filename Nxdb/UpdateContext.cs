using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using org.basex.core;
using org.basex.core.cmd;
using org.basex.query;
using org.basex.query.up;
using org.basex.query.up.primitives;

namespace Nxdb
{
    public class UpdateContext : IDisposable
    {
        private static int _counter = 0;
        private static Updates _updates = new Updates();

        //This caches all contexts used in this update as well as coorisponding
        //QueryContexts, which at the moment only appear to be used for permissions
        private static readonly Dictionary<Context, QueryContext> Contexts
            = new Dictionary<Context, QueryContext>(); 

        public UpdateContext()
        {
            _counter++;
        }

        //Adds an update primitive to the sequence of operations
        internal static void AddUpdate(UpdatePrimitive update, Context context)
        {
            //Add the update to the current context
            QueryContext queryContext;
            if(!Contexts.TryGetValue(context, out queryContext))
            {
                queryContext = new QueryContext(context);
                Contexts.Add(context, queryContext);
            }
            _updates.add(update, queryContext);

            //If a context isn't open, apply the update immediatly
            ApplyUpdates();
        }

        private static void ApplyUpdates()
        {
            if (_counter <= 0)
            {
                //Apply the updates
                _updates.applyUpdates();

                //Optimize database(s)
                //TODO: Make this an option or a flag
                if (true)
                {
                    //Loop through each context used and optimize
                    foreach (Context context in Contexts.Keys)
                    {
                        NxDatabase.Run(new Optimize(), context);
                    }
                }

                //Reset
                Contexts.Clear();
                _updates = new Updates();
                _counter = 0;
            }
        }

        public void Dispose()
        {
            _counter--;
            ApplyUpdates();
        }
    }
}
