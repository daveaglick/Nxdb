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
