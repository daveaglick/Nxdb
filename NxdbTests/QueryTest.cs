using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
    }
}
