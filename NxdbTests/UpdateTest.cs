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
            using (Database database = new Database(Common.DatabaseName))
            {
                Documents docs = Common.Populate(database, "A", "B", "C", "D");

                //Simple update with context
                Query query = new Query("insert node 'test' into .");
                Document doc = database.GetDocument("B");
                query.SetContext(doc);
                Assert.AreEqual(1, doc.Children.Count());
                query.Evaluate();
                Assert.AreEqual(2, doc.Children.Count());

                //Multiple updates - make sure none are updated until Update is closed
                using (new Update())
                {
                    query.Expression = "insert node 'test2' into .";
                    query.Evaluate();
                    query.Expression = "insert node 'test3' into .";
                    query.Evaluate();
                    query.Expression = "string(./text()[1])";
                    Assert.AreEqual("test", query.GetSingle());
                }
                Assert.AreEqual(2, doc.Children.Count());   //The two insert texts should have been merged into the existing
                query.Expression = "string(./text()[1])";
                Assert.AreEqual("testtest2test3", query.GetSingle());

                //Aborted update
                using (new Update())
                {
                    query.Expression = "insert node 'test4' into .";
                    query.Evaluate();
                    Assert.AreEqual(2, doc.Children.Count());
                    query.Expression = "insert node 'test5' into .";
                    query.Evaluate();
                    Assert.AreEqual(2, doc.Children.Count());
                    query.Expression = "string(./text()[1])";
                    Assert.AreEqual("testtest2test3", query.GetSingle());
                    Update.Reset();
                }
                Assert.AreEqual(2, doc.Children.Count());
                query.Expression = "string(./text()[1])";
                Assert.AreEqual("testtest2test3", query.GetSingle());

                //Multiple updates with non-query updates
                using (new Update())
                {
                    query.Expression = "insert node 'test6' into .";
                    query.Evaluate();
                    doc.Append("test7");
                    query.Expression = "string(./text()[1])";
                    Assert.AreEqual("testtest2test3", query.GetSingle());
                }
                Assert.AreEqual(2, doc.Children.Count());
                query.Expression = "string(./text()[1])";
                Assert.AreEqual("testtest2test3test6test7", query.GetSingle());
            }
        }
    }
}
