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

                //TODO: Some tests for variable binding
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
        }

        private void TypeConversion(object value)
        {
            Query query = new Query("$var");
            query.SetVariable("var", value);
            IList<object> results = query.GetList();
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(value, results[0]);
        }
    }
}
