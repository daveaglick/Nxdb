using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Nxdb;

namespace NxdbTests
{
    [TestFixture]
    public class PropertiesTest
    {
        [Test]
        public void DropOnDispose()
        {
            Common.Reset();
            Properties.DropOnDispose.Set(true);
            using (Database database = Database.Get(Common.DatabaseName))
            {
                Common.Populate(database, "A", "B", "C", "D");
            }
            using (Database database = Database.Get(Common.DatabaseName))
            {
                CollectionAssert.AreEqual(new[] { Common.DatabaseName }, database.DocumentNames); //Initially has an empty document with the database name
            }
        }
    }
}
