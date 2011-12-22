using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using NUnit.Framework;
using Nxdb;

namespace NxdbTests
{
    [TestFixture]
    public class QueryTest
    {
        [Test]
        public void SimpleQueries()
        {
            Common.Reset();

            Query query = new Query("1 + 2");
            IList<object> results = query.GetList();
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(3, results[0]);

            query = new Query("(3, 'test', 2 + 5)");
            results = query.GetList();
            Assert.AreEqual(3, results.Count);
            Assert.AreEqual(3, results[0]);
            Assert.AreEqual("test", results[1]);
            Assert.AreEqual(7, results[2]);
        }

        [Test]
        public void MultipleDatabases()
        {
            Common.Reset();
            using (Database database = new Database(Common.DatabaseName))
            {
                using (Database database2 = new Database(Common.DatabaseName + "2"))
                {
                    Documents docs = Common.Populate(database, "A", "B", "C", "D");
                    Documents docs2 = Common.Populate(database2, "A", "B", "E", "F");

                    Query query = new Query("/A");
                    query.SetInitialContext(new Database[]{database, database2});
                    IList<object> results = query.GetList();
                    Assert.AreEqual(2, results.Count);
                    CollectionAssert.AreEquivalent(new[] { Common.DatabaseName, Common.DatabaseName + "2"},
                        results.OfType<Node>().Select(n => n.Database.Name));

                    query.Expression = "/*";
                    results = query.GetList();
                    Assert.AreEqual(8, results.Count);
                    CollectionAssert.AreEquivalent(docs.Names.Concat(docs2.Names),
                        results.OfType<Node>().Select(n => n.Name));
                }
            }
        }

        [Test]
        public void Variables()
        {
            Common.Reset();
            using (Database database = new Database(Common.DatabaseName))
            {
                Documents docs = Common.Populate(database, "A", "B", "C", "D");

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
            Query query = new Query("$var");
            object[] arr = new object[] {1, "2", 3.4, true};
            query.SetVariable("var", arr);
            IList<object> results = query.GetList();
            Assert.AreEqual(4, results.Count);
            CollectionAssert.AreEqual(arr, results);

            // Multi dimensional array (test flattening)
            query = new Query("$var");
            arr = new object[] { 1, "2", new object[]{3.4, 5, 6}, true };
            query.SetVariable("var", arr);
            results = query.GetList();
            Assert.AreEqual(6, results.Count);
            CollectionAssert.AreEqual(new object[] { 1, "2", 3.4, 5, 6, true }, results);
        }

        private void TypeConversion(object value)
        {
            Query query = new Query("$var");
            query.SetVariable("var", value);
            IList<object> results = query.GetList();
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(value, results[0]);
        }

        [Test]
        public void ExternalFunctions()
        {
            Common.Reset();

            // External static method call
            Query query = new Query("QueryTest:FuncTest(xs:int(5))");
            query.SetExternal(GetType());
            IList<object> results = query.GetList();
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(FuncTest(5), results[0]);

            // External class construction and member call
            query = new Query("let $cls := ExtTest:new('testing') return ExtTest:Count($cls, xs:int(5))");
            query.SetExternal(typeof(ExtTest));
            results = query.GetList();
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(12, results[0]);

            // Passing an object via variable binding
            query = new Query("QueryTest:BindingTest($var, xs:int(5))");
            query.SetVariable("var", new ExtTest("testing"));
            query.SetExternal(GetType());
            results = query.GetList();
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
