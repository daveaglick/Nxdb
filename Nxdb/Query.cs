using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using org.basex.query;
using org.basex.query.expr;
using org.basex.query.item;
using org.basex.query.iter;

namespace Nxdb
{
    public class Query
    {
        private string _expression;
        private readonly Dictionary<string, Value> _collections
            = new Dictionary<string, Value>();  //String.Empty = the default (first) collection
        private Value _initialContext = null;

        public Query(string expression)
        {
            Expression = expression;
        }

        public string Expression
        {
            get { return _expression; }
            set
            {
                if (value == null) throw new ArgumentNullException("value");
                if (value == String.Empty) throw new ArgumentException("value");
                _expression = value;
            }
        }

        //name == null -> set the initial context
        //name == String.Empty -> set the default (first) collection
        //name == text -> set a named collection
        //value == null -> clear the specified context/collection
        private void SetCollection(string name, Value value)
        {
            if(name == null)
            {
                _initialContext = value;
            }
            else
            {
                if(value == null)
                {
                    _collections.Remove(name);
                }
                else
                {
                    _collections[name] = value;
                }
            }
        }

        public IEnumerable<object> Evaluate()
        {
            //Create the query context
            QueryContext queryContext = new QueryContext(Database.Context);

            //TODO: Add variables

            //Parse the expression
            queryContext.parse(_expression);

            //Compile the query
            queryContext.compile();

            //Get the iterator and return the results
            Iter iter = queryContext.iter();
            return new IterEnum(iter);
        }
    }
}
