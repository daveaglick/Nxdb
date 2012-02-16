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

namespace NxdbTests
{
    [TestFixture]
    public class DatabaseTest
    {
        [Test]
        public void Drop()
        {
            Common.Reset();
            using (Database database = Database.Get(Common.DatabaseName))
            {
                Common.Populate(database, "A", "B", "C", "D");
            }
            Database.Drop(Common.DatabaseName);
            using (Database database = Database.Get(Common.DatabaseName))
            {
                CollectionAssert.AreEqual(new []{Common.DatabaseName}, database.DocumentNames); // Initially has an empty document with the database name
            }
        }

        [Test]
        public void Add()
        {
            Common.Reset();
            using (Database database = Database.Get(Common.DatabaseName))
            {
                Documents docs = Common.Populate(database, "A", "B", "C", "D");
                docs.Verify(database);

                //Add an empty document
                Document e = database.GetDocument("E");
                Assert.IsNull(e);
                database.Add("E", String.Empty);
                e = database.GetDocument("E");
                Assert.IsNotNull(e);

                // TODO: Test adding with XmlDocument and XDocument
            }
        }

        [Test]
        public void Paths()
        {
            Common.Reset();
            using (Database database = Database.Get(Common.DatabaseName))
            {
                Documents docs = Common.Populate(database, "A", "B", "path/A", "path/B", "path/C", "path2/D", "path2/E");

                docs.Verify(database);

                //Delete some documents on a specific path
                database.Delete("path");
                docs.Names.RemoveAt(2);
                docs.Names.RemoveAt(2);
                docs.Names.RemoveAt(2);
                docs.Content.RemoveAt(2);
                docs.Content.RemoveAt(2);
                docs.Content.RemoveAt(2);
                docs.Verify(database);

                //Try a global rename
                database.Rename("path2", "path3");
                docs.Names[2] = "path3/D";
                docs.Names[3] = "path3/E";
                docs.Verify(database);
            }
        }

        [Test]
        public void Delete()
        {
            Common.Reset();
            using (Database database = Database.Get(Common.DatabaseName))
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
            using (Database database = Database.Get(Common.DatabaseName))
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
            using (Database database = Database.Get(Common.DatabaseName))
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
            using (Database database = Database.Get(Common.DatabaseName))
            {
                Documents docs = Common.Populate(database, "A", "B", "C", "D");
                Node node = database.GetDocument(docs.Names[1]);
                Assert.IsNotNull(node);
                Assert.AreEqual(docs.Names[1], node.Name);
            }
        }
    }
}
