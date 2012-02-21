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

namespace Nxdb
{
    /// <summary>
    /// Defines the interface for objects that can be queried.
    /// </summary>
    public interface IQuery
    {
        /// <summary>
        /// Evaluates the specified expression and returns the result as a enumeration of objects.
        /// </summary>
        /// <param name="expression">The expression to evaluate.</param>
        /// <returns>An enumeration of the result objects.</returns>
        IEnumerable<object> Eval(string expression);

        /// <summary>
        /// Evaluates the specified expression and returns an enumeration of all resultant
        /// objects matching the given type.
        /// </summary>
        /// <typeparam name="T">The type of result objects to return.</typeparam>
        /// <param name="expression">The expression to evaluate.</param>
        /// <returns>An enumeration of all result objects matching the specified type.</returns>
        IEnumerable<T> Eval<T>(string expression);

        /// <summary>
        /// Evaluates the specified expression and returns an IList of objects.
        /// </summary>
        /// <param name="expression">The expression to evaluate.</param>
        /// <returns>An IList of the result objects.</returns>
        IList<object> EvalList(string expression);

        /// <summary>
        /// Evaluates the specified expression and returns an IList of all resultant
        /// objects matching the given type.
        /// </summary>
        /// <typeparam name="T">The type of result objects to return.</typeparam>
        /// <param name="expression">The expression to evaluate.</param>
        /// <returns>An IList of all result objects matching the specified type.</returns>
        IList<T> EvalList<T>(string expression);

        /// <summary>
        /// Evaluates the specified expression and returns a single result object (the first
        /// if the expression resulted in more than one result). If
        /// the expression does not evaluate to any results, null is returned.
        /// </summary>
        /// <param name="expression">The expression to evaluate.</param>
        /// <returns>The first result object (or null).</returns>
        object EvalSingle(string expression);

        /// <summary>
        /// Evaluates the specified expression and returns a single result object of the
        /// specified type. If the first resultant object is not of the given type or if
        /// the expression does not evaluate to any results, null is returned.
        /// </summary>
        /// <typeparam name="T">The type of result object to return.</typeparam>
        /// <param name="expression">The expression to evaluate.</param>
        /// <returns>The first result object as the specified type (or null).</returns>
        T EvalSingle<T>(string expression) where T : class;
    }
}
