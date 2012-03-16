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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Xml.Serialization;
using NUnit.Framework;
using Nxdb;
using Nxdb.Node;
using Nxdb.Persistence;
using Nxdb.Persistence.Attributes;

namespace NxdbTests
{
    [TestFixture]
    public class PersistenceTest
    {
        [Test]
        public void XmlSerializerBehavior()
        {
            RunTests<XmlSerializerPersistentClass>();
        }

        [Test]
        public void DefaultBehavior()
        {
            RunTests<DefaultPersistentClass>();
        }

        private void RunTests<T>() where T : IPersistentClass, new()
        {
            Common.Reset();
            using (Database database = Database.Get(Common.DatabaseName))
            {
                database.Add("Doc", "<Doc/>");
                Element elem = (Element)database.GetDocument("Doc").Child(0);
                Assert.IsNotNull(elem);

                // Initial append two items
                T foo = new T {Num = 5, Str = "abc", Bl = false};
                T bar = new T {Num = 456, Str = "abc", Bl = true};
                elem.Append(foo);
                elem.Append(bar, "Bar");
                string barContent = bar.ToString("Bar");
                Assert.AreEqual(foo.ToString() + barContent, elem.InnerXml);

                // Change and then store only one item
                foo.Num = 10;
                foo.Str = "xyz";
                bar.Bl = false;
                bar.Str = "qwerty";
                foo.Store();
                Assert.AreEqual(foo.ToString() + barContent, elem.InnerXml);

                // Store items
                foo.Store();
                bar.Store();
                barContent = bar.ToString("Bar");
                string fooContent = foo.ToString();
                Assert.AreEqual(fooContent + barContent, elem.InnerXml);

                // Change attached element for one item
                bar.Num = 9876;
                elem.Append(bar, "Baz");
                Assert.AreEqual(fooContent + barContent + bar.ToString("Baz"), elem.InnerXml);

                // Fetch the first instance into a new object
                T copy = new T {Num = 567, Str = "zxcvb", Bl = true};
                Element fooElem = (Element)elem.Child(0);
                Assert.AreNotEqual(copy.ToString(), fooElem.OuterXml);
                copy.Fetch(fooElem);
                Assert.AreEqual(3, copy.NumArr.Length);
                Assert.AreEqual(fooContent, copy.ToString());
                Assert.AreEqual(fooElem.OuterXml, copy.ToString());

                // Change the old instance and attach the new one
                foo.Str = "sprgj";
                foo.Store();
                bar.Store();
                copy.Attach(fooElem);
                fooContent = foo.ToString();
                Assert.AreEqual(copy.NumArr.Length, 3);
                Assert.AreEqual(copy.ToString(), fooElem.OuterXml);
                Assert.AreEqual(copy.ToString(), fooContent);

                // Detach the old instance and change the new instance
                foo.Detach();
                copy.Str = "yuiop";
                bar.Store();
                copy.Store();
                Assert.Throws(Is.InstanceOf<Exception>(), () => foo.Store());
                Assert.AreEqual(copy.ToString(), fooElem.OuterXml);
                Assert.AreEqual(fooContent, foo.ToString());
                Assert.AreNotEqual(foo.ToString(), fooElem.OuterXml);

                // Test fetch failure
                Assert.Throws(Is.InstanceOf<Exception>(), () => foo.Fetch((Element)fooElem.Child(0)));
            }
        }
    }

    public interface IPersistentClass
    {
        int Num { get; set; }
        string Str { get; set; }
        bool Bl { get; set; }
        int[] NumArr { get; set; }
        string ToString(string elementName);
    }

public class Vehicle
{
    [PersistentAttribute]
    private int _numberWheels;

    [PersistentAttribute]
    private bool _isCar;

    public int NumberWheels
    {
        get { return _numberWheels; }
        set
        {
            _numberWheels = value;
            _isCar = value == 4;
        }
    }

    public bool IsCar
    {
        get { return _isCar; }
    }

    [PersistentElement]
    public string Model { get; set; }
}

    [XmlSerializerPersister(Indent = false)]
    public class XmlSerializerPersistentClass : IPersistentClass
    {
        private int _num;
        public int Num
        {
            get { return _num; }
            set
            {
                NumArr = new []{value+1, value+2, value+3};
                _num = value;
            }
        }
        public string Str { get; set; }

        [XmlAttribute("bool")]
        public bool Bl { get; set; }

        public int[] NumArr { get; set; }

        public string ToString(string elementName)
        {
            return String.Format(
                "<{0} bool=\"{1}\"><Num>{2}</Num><Str>{3}</Str><NumArr><int>{4}</int><int>{5}</int><int>{6}</int></NumArr></{0}>",
                elementName, Bl.ToString().ToLower(), Num, Str, Num+1, Num+2, Num+3);
        }

        public override string ToString()
        {
            return ToString("XmlSerializerPersistentClass");
        }
    }

    public class DefaultPersistentClass : IPersistentClass
    {
        [PersistentElement(Order = 2)]
        private int _num;

        public int Num
        {
            get { return _num; }
            set
            {
                NumArr = new[] { value + 1, value + 2, value + 3 };
                _classes.Add(new DefaultPersistentClass());
                _num = value;
                _dictionary.Clear();
                foreach (int key in NumArr)
                {
                    _dictionary.Add(key, "abc" + key + 5);
                }
            }
        }

        [PersistentElement(Order = 1, Required = true)]
        public string Str { get; set; }

        [PersistentAttribute(Name = "bool")]
        public bool Bl { get; set; }

        [PersistentCollection(Order = 4)]
        public int[] NumArr { get; set; }

        [PersistentElement(IsPersistentObject = true, Order = 3)]
        public DefaultPersistentClass Inner { get; set; }

        [PersistentCollection(Order = 5, ItemsArePersistentObjects = true, ItemName = "PersistentItem")]
        private readonly List<DefaultPersistentClass> _classes
            = new List<DefaultPersistentClass>();

        [PersistentKvpCollection(Order = 6, KeyAttributeName = "k")]
        private readonly Dictionary<int, string> _dictionary
            = new Dictionary<int, string>(); 

        public string ToString(string elementName)
        {
            string numArrString = "<NumArr />";
            if(NumArr != null && NumArr.Length > 0)
            {
                numArrString = "<NumArr>";
                foreach(int num in NumArr)
                {
                    numArrString += "<Item>" + num + "</Item>";
                }
                numArrString += "</NumArr>";
            }

            string classesString = "<_classes />";
            if(_classes.Count > 0)
            {
                classesString = "<_classes>";
                foreach(DefaultPersistentClass cls in _classes)
                {
                    classesString += cls.ToString("PersistentItem");
                }
                classesString += "</_classes>";
            }

            string dictionaryString = "<_dictionary />";
            if(_dictionary.Count > 0)
            {
                dictionaryString = "<_dictionary>";
                foreach(KeyValuePair<int, string> kvp in _dictionary)
                {
                    dictionaryString += String.Format("<Item k=\"{0}\"><Value>{1}</Value></Item>", kvp.Key, kvp.Value);
                }
                dictionaryString += "</_dictionary>";
            }

            return String.Format(
                "<{0} bool=\"{1}\">{2}<_num>{3}</_num>{4}{5}{6}{7}</{0}>",
                elementName, Bl.ToString(), String.IsNullOrEmpty(Str) ? "<Str />" : "<Str>" + Str + "</Str>",
                Num, Inner == null ? String.Empty : Inner.ToString("Inner"),
                numArrString, classesString, dictionaryString);
        }

        public override string ToString()
        {
            return ToString("DefaultPersistentClass");
        }
    }
}
