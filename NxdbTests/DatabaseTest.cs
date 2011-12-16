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
    public class DatabaseTest
    {
        [Test]
        public void Drop()
        {
            Common.Reset();
            using (Database database = new Database(Common.DatabaseName))
            {
                Common.Populate(database, "A", "B", "C", "D");
            }
            Assert.IsTrue(Database.Drop(Common.DatabaseName));
            using (Database database = new Database(Common.DatabaseName))
            {
                CollectionAssert.AreEqual(new []{Common.DatabaseName}, database.DocumentNames); //Initially has an empty document with the database name
            }
        }

        [Test]
        public void Add()
        {
            Common.Reset();
            using (Database database = new Database(Common.DatabaseName))
            {
                Documents docs = Common.Populate(database, "A", "B", "C", "D");

                //TODO: Test adding with XmlDocument and XDocument

                docs.Verify(database);
            }
        }

        [Test]
        public void Delete()
        {
            Common.Reset();
            using (Database database = new Database(Common.DatabaseName))
            {
                Documents docs = Common.Populate(database, "A", "B", "C", "D");
                database.Delete(docs.Names[1]);
                docs.Names.RemoveAt(1);
                docs.Content.RemoveAt(1);
                docs.Verify(database);
            }
        }

        [Test]
        public void Rename()
        {
            Common.Reset();
            using (Database database = new Database(Common.DatabaseName))
            {
                Documents docs = Common.Populate(database, "A", "B", "C", "D");
                database.Rename(docs.Names[2], "E");
                docs.Names[2] = "E";
                docs.Verify(database);
            }
        }

        [Test]
        public void Replace()
        {
            Common.Reset();
            using (Database database = new Database(Common.DatabaseName))
            {
                Documents docs = Common.Populate(database, "A", "B", "C", "D");
                string content = Common.GenerateXmlContent("E");
                database.Replace(docs.Names[1], content);
                docs.Content[1] = content;
                docs.Verify(database);

                //Test replacing a document that doesn't exist (shouldn't do anything)
                content = Common.GenerateXmlContent("F");
                database.Replace("X", content);
                docs.Verify(database);
            }
        }

        [Test]
        public void GetDocument()
        {
            Common.Reset();
            using (Database database = new Database(Common.DatabaseName))
            {
                Documents docs = Common.Populate(database, "A", "B", "C", "D");
                Node node = database.GetDocument(docs.Names[1]);
                Assert.IsNotNull(node);
                Assert.AreEqual(docs.Names[1], node.Name);
            }
        }
    }
}
