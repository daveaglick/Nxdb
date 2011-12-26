using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Nxdb;

namespace NxdbTests
{
    [TestFixture]
    public class UpdateTest
    {
        [Test]
        public void Simple()
        {
            Common.Reset();
            using (Database database = Database.Get(Common.DatabaseName))
            {
                Documents docs = Common.Populate(database, "A", "B", "C", "D");

                //Simple update with context
                Document doc = database.GetDocument("B");
                Query query = new Query(doc);
                Assert.AreEqual(1, doc.Children.Count());
                query.Eval("insert node 'test' into .");
                Assert.AreEqual(2, doc.Children.Count());
            }
        }

        [Test]
        public void Multiple()
        {
            Common.Reset();
            using (Database database = Database.Get(Common.DatabaseName))
            {
                Documents docs = Common.Populate(database, "A", "B", "C", "D");

                //Simple update with context
                Document doc = database.GetDocument("B");
                Query query = new Query(doc);
                Assert.AreEqual(1, doc.Children.Count());
                query.Eval("insert node 'test' into .");
                Assert.AreEqual(2, doc.Children.Count());

                //Multiple updates - make sure none are updated until Updates is closed
                using (new Updates())
                {
                    query.Eval("insert node 'test2' into .");
                    query.Eval("insert node 'test3' into .");
                    Assert.AreEqual("test", query.EvalSingle("string(./text()[1])"));
                }
                Assert.AreEqual(2, doc.Children.Count());   //The two insert texts should have been merged into the existing
                Assert.AreEqual("testtest2test3", query.EvalSingle("string(./text()[1])"));

                //Multiple updates with non-query updates
                using (new Updates())
                {
                    query.Eval("insert node 'test6' into .");
                    doc.Append("test7");
                    Assert.AreEqual("testtest2test3", query.EvalSingle("string(./text()[1])"));
                }
                Assert.AreEqual(2, doc.Children.Count());
                Assert.AreEqual("testtest2test3test6test7", query.EvalSingle("string(./text()[1])"));
            }
        }

        [Test]
        public void Aborted()
        {
            Common.Reset();
            using (Database database = Database.Get(Common.DatabaseName))
            {
                Documents docs = Common.Populate(database, "A", "B", "C", "D");
                Document doc = database.GetDocument("B");
                Query query = new Query(doc);
                query.Eval("insert node 'test' into .");

                //Aborted update
                using (new Updates())
                {
                    query.Eval("insert node 'test4' into .");
                    Assert.AreEqual(2, doc.Children.Count());
                    query.Eval("insert node 'test5' into .");
                    Assert.AreEqual(2, doc.Children.Count());
                    Assert.AreEqual("test", query.EvalSingle("string(./text()[1])"));
                    Updates.Reset();
                }
                Assert.AreEqual(2, doc.Children.Count());
                Assert.AreEqual("test", query.EvalSingle("string(./text()[1])"));
            }
        }

        [Test]
        public void Events()
        {
            Common.Reset();
            using (Database database = Database.Get(Common.DatabaseName))
            {
                Documents docs = Common.Populate(database, "A", "B", "C", "D");
                Document doc = database.GetDocument("B");

                //Set up events
                Element delElem = (Element)doc.Child(0).Child(1).Child(1);
                Element keepElem = (Element) delElem.PrecedingSiblings.First();
                bool delInvalid = false;
                bool keepInvalid = false;
                bool dbUpdate = false;
                delElem.Invalidated += (o, e) => delInvalid = true;
                keepElem.Invalidated += (o, e) => keepInvalid = true;
                database.Updated += (o, e) => dbUpdate = true;

                using (new Updates())
                {
                    delElem.Remove();
                    Assert.IsFalse(delInvalid);
                    Assert.IsFalse(keepInvalid);
                    Assert.IsFalse(dbUpdate);
                }
                Assert.IsTrue(delInvalid);
                Assert.IsFalse(keepInvalid);
                Assert.IsTrue(dbUpdate);
                Assert.AreEqual(1, doc.Child(0).Child(1).Children.Count());
            }
        }
    }
}
