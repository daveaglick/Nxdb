using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using org.basex.query;
using org.basex.query.expr;
using org.basex.query.item;
using org.basex.query.iter;
using org.basex.util;
using Type = System.Type;

namespace Nxdb
{
    public class Query
    {
        private string _expression;
        private readonly Dictionary<string, Value> _namedCollections
            = new Dictionary<string, Value>();
        private Value _defaultCollection = null;
        private Value _context = null;
        private readonly Dictionary<string, Value> _variables
            = new Dictionary<string, Value>();
        private readonly Dictionary<string, string> _externals
            = new Dictionary<string, string>();

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

        public void SetContext(object value)
        {
            _context = value.ToValue();
        }

        // name == null or String.Empty -> set the default (first) collection
        // name == text -> set a named collection
        // value == null -> clear the specified context/collection
        public void SetCollection(string name, object value)
        {
            Value v = value.ToValue();
            if(String.IsNullOrEmpty(name))
            {
                _defaultCollection = v;
            }
            else
            {
                if(v == null)
                {
                    _namedCollections.Remove(name);
                }
                else
                {
                    _namedCollections[name] = v;
                }
            }
        }

        // value == null -> clear variable
        public void SetVariable(string name, object value)
        {
            if (name == null) throw new ArgumentNullException("name");
            if (name == String.Empty) throw new ArgumentException("name");
            Value v = value.ToValue();
            if(v == null)
            {
                _variables.Remove(name);
            }
            else
            {
                _variables[name] = v;
            }
        }


        /// <summary>
        /// Sets an external class that can be used for XQuery external functions. The provided
        /// namespace will be used in the XQuery statement for all external functions that should
        /// call methods in the provided type. All external methods should be static. For exmaple,
        /// given a class Foo with a static method Foo.Bar() and a provided namespace of "xyz", the
        /// method could be called in XQuery as "xyz:Bar()".
        /// 
        /// In order for parameter types to match, some casting may need to be
        /// conducted. In the previous example, if the method took an int
        /// parameter, the XQuery call would need to cast to an xs:int as "xyz:Bar(xs:int(5))". Not
        /// providing the cast would result in a more broad xs:integer XQuery type which would not
        /// directly convert to an int type therefore causing parameter resolution problems.
        /// 
        /// Also keep in mind that an automatic translation between .NET and Java types is not yet
        /// supported so complex types such as dates, times, and QNames should be avoided at the
        /// moment.
        /// </summary>
        /// <param name="ns">The namespace of the external class functions.</param>
        /// <param name="type">The type that contains the static methods for this namespace.</param>
        public void SetExternal(string ns, Type type)
        {
            if (ns == null) throw new ArgumentNullException("ns");
            if (ns == String.Empty) throw new ArgumentException("ns");
            if(type == null)
            {
                _externals.Remove(ns);
            }
            else
            {
                _externals[ns] = type.AssemblyQualifiedName;
            }
        }

        /// <summary>
        /// Sets an external class that can be used for XQuery external functions. The name of
        /// the class will be registered as the namespace under which it's member static methods
        /// will be available.
        /// </summary>
        /// <param name="type">The type that contains the static methods you wish to call.</param>
        public void SetExternal(Type type)
        {
            if (type == null) throw new ArgumentNullException("type");
            SetExternal(type.Name, type);
        }

        // TODO: Return a structure with the result enumerable as well as other information such as what was modified
        public IEnumerable<object> Evaluate()
        {
            // Create the query context
            QueryContext queryContext = new QueryContext(Database.Context);

            // Add variables
            foreach(KeyValuePair<string, Value> kvp in _variables)
            {
                queryContext.bind(kvp.Key, kvp.Value);
            }

            // Add default collection
            queryContext.resource.addCollection(_defaultCollection ?? Empty.SEQ, String.Empty);

            // Add named collections
            foreach(KeyValuePair<string, Value> kvp in _namedCollections)
            {
                queryContext.resource.addCollection(kvp.Value, kvp.Key);
            }

            // Set the initial context item
            queryContext.ctxItem = _context;

            // Add external namespaces
            foreach(KeyValuePair<string, string> kvp in _externals)
            {
                queryContext.sc.@namespace(kvp.Key, "java:" + kvp.Value);
            }

            using (new Update())
            {
                // Reset the update collection to the common one in our update operation
                queryContext.updates = Update.Updates;

                // Parse the expression
                queryContext.parse(_expression);

                // Compile the query
                queryContext.compile();

                // Reset the updating flag (so they aren't applied here)
                queryContext.updating = false;

                // Get the iterator and return the results
                Iter iter = queryContext.iter();
                return new IterEnum(iter);
            }
        }

        public IEnumerable<object> GetResults()
        {
            return Evaluate();
        }

        public IList<object> GetList()
        {
            return new List<object>(GetResults());
        }

        public IList<T> GetList<T>()
        {
            return new List<T>(GetResults().OfType<T>());
        }

        public object GetSingle()
        {
            return GetResults().FirstOrDefault();
        }

        public T GetSingle<T>() where T : class
        {
            return GetSingle() as T;
        }
    }
}
