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
using System.Xml;
using NUnit.Framework;
using Nxdb;
using Nxdb.Node;

namespace NxdbTests
{
    [TestFixture]
    public class QueryTest
    {
        [Test]
        public void SimpleQueries()
        {
            Common.Reset();

            IList<object> results = Query.EvalList(null, "1 + 2");
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(3, results[0]);

            results = Query.EvalList(null, "(3, 'test', 2 + 5)");
            Assert.AreEqual(3, results.Count);
            Assert.AreEqual(3, results[0]);
            Assert.AreEqual("test", results[1]);
            Assert.AreEqual(7, results[2]);
        }

        [Test]
        public void MultipleDatabases()
        {
            Common.Reset();
            using (Database database = Database.Get(Common.DatabaseName))
            {
                using (Database database2 = Database.Get(Common.DatabaseName + "2"))
                {
                    Documents docs = Common.Populate(database, "A", "B", "C", "D");
                    Documents docs2 = Common.Populate(database2, "A", "B", "E", "F");

                    Query query = new Query(new []{database, database2});
                    IList<object> results = query.EvalList("/A");
                    Assert.AreEqual(2, results.Count);
                    CollectionAssert.AreEquivalent(new[] { Common.DatabaseName, Common.DatabaseName + "2"},
                        results.OfType<Node>().Select(n => n.Database.Name));

                    results = query.EvalList("/*");
                    Assert.AreEqual(8, results.Count);
                    CollectionAssert.AreEquivalent(docs.Names.Concat(docs2.Names),
                        results.OfType<Node>().Select(n => n.Name));
                }
            }
        }

        [Test]
        public void Context()
        {
            Common.Reset();
            using (Database database = Database.Get(Common.DatabaseName))
            {
                Documents docs = Common.Populate(database, "A", "B", "C", "D");
                
                Query query = new Query(database.GetDocument("B"));
                IList<object> results = query.EvalList("/*[1]/*[2]");
                Assert.AreEqual("BB", ((Element)results[0]).Name);

                query.SetContext(results[0]);
                results = query.EvalList("./BBB");
                Assert.AreEqual(1, results.Count);
                Assert.AreEqual("BBB", ((Element)results[0]).Name);

                results = query.EvalList("./XYZ");
                Assert.AreEqual(0, results.Count);
            }
        }

        [Test]
        public void Variables()
        {
            Common.Reset();
            using (Database database = Database.Get(Common.DatabaseName))
            {
                Documents docs = Common.Populate(database, "A", "B", "C", "D");

                // TODO: Some complex variable binding tests
            }
        }

        [Test]
        public void TypeConversion()
        {
            Common.Reset();

            TypeConversion(1234);
            TypeConversion("string");
            TypeConversion(1234.567);
            TypeConversion(false);
            TypeConversion(DateTime.Now);
            TypeConversion(DateTime.UtcNow);
            TypeConversion(new TimeSpan(12300000));
            TypeConversion(new Decimal(123456789));
            TypeConversion(new XmlQualifiedName("foo", "bar"));

            // Single dimensional array
            Query query = new Query();
            object[] arr = new object[] {1, "2", 3.4, true};
            query.SetVariable("var", arr);
            IList<object> results = query.EvalList("$var");
            Assert.AreEqual(4, results.Count);
            CollectionAssert.AreEqual(arr, results);

            // Multi dimensional array (test flattening)
            query = new Query();
            arr = new object[] { 1, "2", new object[]{3.4, 5, 6}, true };
            query.SetVariable("var", arr);
            results = query.EvalList("$var");
            Assert.AreEqual(6, results.Count);
            CollectionAssert.AreEqual(new object[] { 1, "2", 3.4, 5, 6, true }, results);
        }

        private void TypeConversion(object value)
        {
            Query query = new Query();
            query.SetVariable("var", value);
            IList<object> results = query.EvalList("$var");
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(value, results[0]);
        }

        [Test]
        public void Externals()
        {
            Common.Reset();

            // External static method call
            Query query = new Query();
            query.SetExternal(GetType());
            IList<object> results = query.EvalList("QueryTest:FuncTest(xs:int(5))");
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(FuncTest(5), results[0]);

            // External class construction and member call
            query = new Query();
            query.SetExternal(typeof(ExtTest));
            results = query.EvalList("let $cls := ExtTest:new('testing') return ExtTest:Count($cls, xs:int(5))");
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(12, results[0]);

            // Passing an object via variable binding
            query = new Query();
            query.SetVariable("var", new ExtTest("testing"));
            query.SetExternal(GetType());
            results = query.EvalList("QueryTest:BindingTest($var, xs:int(5))");
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(12, results[0]);
        }

        public static int FuncTest(int i)
        {
            return i + 9;
        }

        public static int BindingTest(ExtTest obj, int i)
        {
            return obj.Count(i);
        }
    }

    public class ExtTest
    {
        private readonly string _test;

        public ExtTest(string test)
        {
            _test = test;
        }

        public int Count(int add)
        {
            return _test.Length + add;
        }
    }
}
