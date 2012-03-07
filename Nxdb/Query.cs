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
using org.basex.query;
using org.basex.query.expr;
using org.basex.query.item;
using org.basex.query.iter;
using org.basex.util;
using Type = System.Type;

namespace Nxdb
{
    /// <summary>
    /// Encapsulates an XQuery.
    /// </summary>
    public class Query : IQuery
    {
        private readonly Dictionary<string, Value> _namedCollections
            = new Dictionary<string, Value>();
        private Value _defaultCollection = null;
        private Value _context = null;
        private readonly Dictionary<string, Value> _variables
            = new Dictionary<string, Value>();
        private readonly Dictionary<string, string> _externals
            = new Dictionary<string, string>();

        /// <summary>
        /// Initializes a new instance of the <see cref="Query"/> class.
        /// </summary>
        public Query()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Query"/> class.
        /// </summary>
        /// <param name="context">The context of the query. Can be a node type, a database, or any other simple type.</param>
        public Query(object context)
        {
            SetContext(context);
        }

        /// <summary>
        /// Sets the context.
        /// </summary>
        /// <param name="context">The context of the query. Can be a node type, a database, or any other simple type.</param>
        public void SetContext(object context)
        {
            _context = context.ToValue();
        }

        /// <summary>
        /// Sets a collection.
        /// </summary>
        /// <param name="name">The name of the collection. If null or String.Empty, sets the default collection.</param>
        /// <param name="value">The contents of the collection. If null, clears the specified collection.</param>
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

        /// <summary>
        /// Sets a variable.
        /// </summary>
        /// <param name="name">The name of the variable to set.</param>
        /// <param name="value">The value of the variable. If null, clears the named variable.</param>
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

        /// <inheritdoc />
        public IEnumerable<object> Eval(string expression)
        {
            if (expression == null) throw new ArgumentNullException("expression");
            if (expression == String.Empty) throw new ArgumentException("expression");

            // Create the query context
            QueryContext queryContext = new QueryContext(Database.Context);

            // Add variables
            foreach (KeyValuePair<string, Value> kvp in _variables)
            {
                queryContext.bind(kvp.Key, kvp.Value);
            }

            // Add default collection
            queryContext.resource.addCollection(_defaultCollection ?? Empty.SEQ, String.Empty);

            // Add named collections
            foreach (KeyValuePair<string, Value> kvp in _namedCollections)
            {
                queryContext.resource.addCollection(kvp.Value, kvp.Key);
            }

            // Set the initial context item
            queryContext.ctxItem = _context;

            // Add external namespaces
            foreach (KeyValuePair<string, string> kvp in _externals)
            {
                queryContext.sc.@namespace(kvp.Key, "java:" + kvp.Value);
            }

            using (new Updates())
            {
                // Reset the update collection to the common one in our update operation
                queryContext.updates = Updates.QueryUpdates;

                // Parse the expression
                queryContext.parse(expression);

                // Compile the query
                queryContext.compile();

                // Reset the updating flag (so they aren't applied here)
                queryContext.updating(false);

                // Get the iterator and return the results
                Iter iter = queryContext.iter();
                return new IterEnum(iter);
            }
        }

        /// <inheritdoc />
        public IEnumerable<T> Eval<T>(string expression)
        {
            return Eval(expression).OfType<T>();
        }

        /// <inheritdoc />
        public IList<object> EvalList(string expression)
        {
            return new List<object>(Eval(expression));
        }

        /// <inheritdoc />
        public IList<T> EvalList<T>(string expression)
        {
            return new List<T>(Eval(expression).OfType<T>());
        }

        /// <inheritdoc />
        public object EvalSingle(string expression)
        {
            return Eval(expression).FirstOrDefault();
        }

        /// <inheritdoc />
        public T EvalSingle<T>(string expression) where T : class
        {
            return Eval<T>(expression).FirstOrDefault();
        }

        /// <summary>
        /// Evaluates the specified expression and returns the result as a enumeration of objects.
        /// </summary>
        /// <param name="context">The context to use.</param>
        /// <param name="expression">The expression to evaluate.</param>
        /// <returns>
        /// An enumeration of the result objects.
        /// </returns>
        public static IEnumerable<object> Eval(object context, string expression)
        {
            return new Query(context).Eval(expression);
        }

        /// <summary>
        /// Evaluates the specified expression and returns an enumeration of all resultant
        /// objects matching the given type.
        /// </summary>
        /// <typeparam name="T">The type of result objects to return.</typeparam>
        /// <param name="context">The context to use.</param>
        /// <param name="expression">The expression to evaluate.</param>
        /// <returns>An enumeration of all result objects matching the specified type.</returns>
        public static IEnumerable<T> Eval<T>(object context, string expression)
        {
            return new Query(context).Eval<T>(expression);
        }

        /// <summary>
        /// Evaluates the specified expression and returns an IList of objects.
        /// </summary>
        /// <param name="context">The context to use.</param>
        /// <param name="expression">The expression to evaluate.</param>
        /// <returns>An IList of the result objects.</returns>
        public static IList<object> EvalList(object context, string expression)
        {
            return new Query(context).EvalList(expression);
        }

        /// <summary>
        /// Evaluates the specified expression and returns an IList of all resultant
        /// objects matching the given type.
        /// </summary>
        /// <typeparam name="T">The type of result objects to return.</typeparam>
        /// <param name="context">The context to use.</param>
        /// <param name="expression">The expression to evaluate.</param>
        /// <returns>An IList of all result objects matching the specified type.</returns>
        public static IList<T> EvalList<T>(object context, string expression)
        {
            return new Query(context).EvalList<T>(expression);
        }

        /// <summary>
        /// Evaluates the specified expression and returns a single result object (the first
        /// if the expression resulted in more than one result). If
        /// the expression does not evaluate to any results, null is returned.
        /// </summary>
        /// <param name="context">The context to use.</param>
        /// <param name="expression">The expression to evaluate.</param>
        /// <returns>The first result object (or null).</returns>
        public static object EvalSingle(object context, string expression)
        {
            return new Query(context).EvalSingle(expression);
        }

        /// <summary>
        /// Evaluates the specified expression and returns a single result object of the
        /// specified type. If the first resultant object is not of the given type or if
        /// the expression does not evaluate to any results, null is returned.
        /// </summary>
        /// <typeparam name="T">The type of result object to return.</typeparam>
        /// <param name="context">The context to use.</param>
        /// <param name="expression">The expression to evaluate.</param>
        /// <returns>The first result object as the specified type (or null).</returns>
        public static T EvalSingle<T>(object context, string expression) where T : class
        {
            return new Query(context).EvalSingle<T>(expression);
        }
    }
}
