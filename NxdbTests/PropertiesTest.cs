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

            // No drop on dispose
            Assert.IsFalse(Properties.DropOnDispose);
            using (Database database = Database.Get(Common.DatabaseName))
            {
                Common.Populate(database, "A", "B", "C", "D").Verify(database);
            }
            using (Database database = Database.Get(Common.DatabaseName))
            {
                CollectionAssert.AreEqual(new[] { "A", "B", "C", "D" }, database.DocumentNames);
            }
            Database.Drop(Common.DatabaseName);

            // Drop on dispose
            Properties.DropOnDispose = true;
            using (Database database = Database.Get(Common.DatabaseName))
            {
                Common.Populate(database, "A", "B", "C", "D").Verify(database);
            }
            using (Database database = Database.Get(Common.DatabaseName))
            {
                CollectionAssert.AreEqual(new[] { Common.DatabaseName }, database.DocumentNames); //Initially has an empty document with the database name
            }
        }
    }
}
