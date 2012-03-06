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
using System.Xml.Serialization;
using NUnit.Framework;
using Nxdb;
using Nxdb.Node;
using Nxdb.Persistence;

namespace NxdbTests
{
    [TestFixture]
    public class PersistenceTest
    {
        [Test]
        public void XmlSerializerBehavior()
        {
            Common.Reset();
            using (Database database = Database.Get(Common.DatabaseName))
            {
                database.Add("Doc", "<Doc/>");
                Element elem = database.GetDocument("Doc").Child(0) as Element;
                Assert.IsNotNull(elem);

                // Initial append two items
                XmlSerializerPersistentClass foo = new XmlSerializerPersistentClass(5, "abc", false);
                XmlSerializerPersistentClass bar = new XmlSerializerPersistentClass(456, "abc", true);
                elem.Append(foo);
                elem.Append(bar, "Bar");
                string barContent = bar.ToString("Bar");
                Assert.AreEqual(foo.ToString("XmlSerializerPersistentClass") + barContent, elem.InnerXml);

                // Change and then store only one item
                foo.Num = 10;
                foo.Str = "xyz";
                bar.Bl = false;
                bar.Str = "qwerty";
                foo.Store();
                Assert.AreEqual(foo.ToString("XmlSerializerPersistentClass") + barContent, elem.InnerXml);

                // Store all items
                PersistenceManager.Default.StoreAll();
                barContent = bar.ToString("Bar");
                Assert.AreEqual(foo.ToString("XmlSerializerPersistentClass") + barContent, elem.InnerXml);

                // Change attached element for one item
                bar.Num = 9876;
                elem.Append(bar, "Baz");
                Assert.AreEqual(foo.ToString("XmlSerializerPersistentClass") + barContent + bar.ToString("Baz"), elem.InnerXml);
            }
        }
    }

    [XmlSerializerPersistence(Indent = false)]
    public class XmlSerializerPersistentClass
    {
        public int Num { get; set; }
        public string Str { get; set; }

        [XmlAttribute("bool")]
        public bool Bl { get; set; }

        // The XmlSerializer requires a parameterless constructor
        public XmlSerializerPersistentClass()
        {
        }

        public XmlSerializerPersistentClass(int num, string str, bool bl) : this()
        {
            Num = num;
            Str = str;
            Bl = bl;
        }

        public string ToString(string elementName)
        {
            return String.Format(
                "<{0} bool=\"{1}\"><Num>{2}</Num><Str>{3}</Str></{0}>",
                elementName, Bl.ToString().ToLower(), Num, Str);
        }
    }
}
