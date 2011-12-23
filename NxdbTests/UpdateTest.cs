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
        public void Queries()
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

                //Aborted update
                using (new Updates())
                {
                    query.Eval("insert node 'test4' into .");
                    Assert.AreEqual(2, doc.Children.Count());
                    query.Eval("insert node 'test5' into .");
                    Assert.AreEqual(2, doc.Children.Count());
                    Assert.AreEqual("testtest2test3", query.EvalSingle("string(./text()[1])"));
                    Updates.Reset();
                }
                Assert.AreEqual(2, doc.Children.Count());
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
    }
}
