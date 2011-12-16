using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using NUnit.Framework;
using Nxdb;

namespace NxdbTests
{
    [TestFixture]
    public class NodeTest
    {
        [Test]
        public void Validity()
        {
            Common.Reset();
            using (Database database = new Database(Common.DatabaseName))
            {
                Common.Populate(database, "A", "B", "C", "D");

                Node invalid = database.GetDocument("B").Child(0).Child(1);
                Node valid = database.GetDocument("C").Child(0).Child(1);
                int validId = valid.Id;
                database.Delete("B");
                Assert.IsFalse(invalid.Valid);
                Assert.IsTrue(valid.Valid);
                Assert.AreEqual(validId, valid.Id);
            }
        }

        [Test]
        public void NodeType()
        {
            Common.Reset();
            using (Database database = new Database(Common.DatabaseName))
            {
                Common.Populate(database, "A", "B", "C", "D");
                
                Node document = database.GetDocument("B");
                Assert.AreEqual(XmlNodeType.Document, document.NodeType);

                Node element = database.GetDocument("C").Child(0).Child(1);
                Assert.AreEqual(XmlNodeType.Element, element.NodeType);

                Node attribute = database.GetDocument("A").Child(0).Child(0).Attributes.First();
                Assert.AreEqual(XmlNodeType.Attribute, attribute.NodeType);

                Node text = database.GetDocument("D").Child(0).Child(0).Child(0);
                Assert.AreEqual(XmlNodeType.Text, text.NodeType);

                //TODO: processing instruction

                //TODO: comment
            }
        }

        [Test]
        public void Attributes()
        {
            Common.Reset();
            using (Database database = new Database(Common.DatabaseName))
            {
                Common.Populate(database, "A", "B", "C", "D");

                Node node = database.GetDocument("B").Child(0).Child(1).Child(1);
                CollectionAssert.AreEquivalent(new []{"Bbba", "Bbbb", "Bbbc"}, node.Attributes.Select(n => n.Value));
            }
        }

        [Test]
        public void GetAttribute()
        {
            Common.Reset();
            using (Database database = new Database(Common.DatabaseName))
            {
                Common.Populate(database, "A", "B", "C", "D");

                Element node = (Element)database.GetDocument("B").Child(0).Child(1).Child(1);
                Assert.AreEqual("Bbbb", node.AttributeValue("b"));
                Assert.IsEmpty(node.AttributeValue("invalid"));
            }
        }

        [Test]
        public void GetAttributeNode()
        {
            Common.Reset();
            using (Database database = new Database(Common.DatabaseName))
            {
                Common.Populate(database, "A", "B", "C", "D");

                Element node = (Element)database.GetDocument("B").Child(0).Child(1).Child(1);
                Assert.AreEqual("Bbbc", node.Attribute("c").Value);
                Assert.IsNull(node.Attribute("invalid"));
            }
        }

        [Test]
        public void RemoveAllAttributes()
        {
            Common.Reset();
            using (Database database = new Database(Common.DatabaseName))
            {
                Common.Populate(database, "A", "B", "C", "D");

                Element node = (Element)database.GetDocument("B").Child(0).Child(1).Child(1);
                Assert.AreEqual(3, node.Attributes.Count());
                node.RemoveAllAttributes();
                Assert.AreEqual(0, node.Attributes.Count());
            }
        }

        [Test]
        public void RemoveAttribute()
        {
            Common.Reset();
            using (Database database = new Database(Common.DatabaseName))
            {
                Common.Populate(database, "A", "B", "C", "D");

                Element node = (Element)database.GetDocument("B").Child(0).Child(1).Child(1);
                node.RemoveAttribute("b");
                CollectionAssert.AreEquivalent(new[] { "Bbba", "Bbbc" }, node.Attributes.Select(n => n.Value));
            }
        }

        [Test]
        public void InsertAttribute()
        {
            Common.Reset();
            using (Database database = new Database(Common.DatabaseName))
            {
                Common.Populate(database, "A", "B", "C", "D");

                Element node = (Element)database.GetDocument("B").Child(0).Child(1).Child(1);
                node.InsertAttribute("appendName", "appendValue");
                CollectionAssert.AreEquivalent(new[] { "Bbba", "Bbbb", "Bbbc", "appendValue" }, node.Attributes.Select(n => n.Value));
            }
        }

        [Test]
        public void RemoveAll()
        {
            Common.Reset();
            using (Database database = new Database(Common.DatabaseName))
            {
                Common.Populate(database, "A", "B", "C", "D");

                Element node = (Element)database.GetDocument("B").Child(0).Child(1);
                Assert.AreEqual(2, node.Attributes.Count());
                Assert.AreEqual(2, node.Children.Count());
                CollectionAssert.AreEquivalent(new []{ "d", "e" }, node.Attributes.Select(n => n.Name));
                CollectionAssert.AreEqual(new []{ "BBA", "BBB" }, node.Children.Select(n => n.Name));
                node.RemoveAll();
                CollectionAssert.IsEmpty(node.Attributes);
                CollectionAssert.IsEmpty(node.Children);
            }
        }

        [Test]
        public void Remove()
        {
            Common.Reset();
            using (Database database = new Database(Common.DatabaseName))
            {
                Common.Populate(database, "A", "B", "C", "D");

                Node node = database.GetDocument("B").Child(0).Child(1);
                
                Node child = database.GetDocument("B").Child(0).Child(1).Child(1);
                child.Remove();
                Assert.IsFalse(child.Valid);
                Assert.IsNull(database.GetDocument("B").Child(0).Child(1).Child(1));
                CollectionAssert.AreEqual(new []{ "BBA" }, node.Children.Select(n => n.Name));

                Node attribute = ((Element)database.GetDocument("B").Child(0).Child(1)).Attribute("e");
                attribute.Remove();
                Assert.IsFalse(attribute.Valid);
                Assert.IsNull(((Element)database.GetDocument("B").Child(0).Child(1)).Attribute("e"));
                CollectionAssert.AreEquivalent(new []{ "d" }, node.Attributes.Select(n => n.Name));

                //Text merge node removal
                database.Add("E", "<E>abcd<F>efgh</F>ijkl</E>");
                Node root = database.GetDocument("E").Child(0);
                Assert.AreEqual(3, root.Children.Count());
                root.Child(1).Remove();
                Assert.AreEqual(1, root.Children.Count());
                Assert.AreEqual("abcdijkl", root.Child(0).Value);
            }
        }

        [Test]
        public void Append()
        {
            Common.Reset();
            using (Database database = new Database(Common.DatabaseName))
            {
                Common.Populate(database, "A", "B", "C", "D");

                //Valid node insertion with text merge
                Element node = (Element)database.GetDocument("B").Child(0).Child(1).Child(1);
                Assert.AreEqual(1, node.Children.Count());
                using (TextReader text = new StringReader("abc<def>ghi</def>jkl"))
                {
                    XmlReaderSettings settings = new XmlReaderSettings();
                    settings.ConformanceLevel = ConformanceLevel.Auto;
                    using (XmlReader reader = XmlReader.Create(text, settings))
                    {
                        node.Append(reader);
                    }
                }
                Assert.AreEqual(3, node.Children.Count());
                Assert.AreEqual("Bbbabc", node.Child(0).Value);
                Assert.AreEqual("def", node.Child(1).Name);
                Assert.AreEqual("jkl", node.Child(2).Value);
            }
        }

        [Test]
        public void Prepend()
        {
            Common.Reset();
            using (Database database = new Database(Common.DatabaseName))
            {
                Common.Populate(database, "A", "B", "C", "D");

                //Valid node insertion with text merge
                Element node = (Element)database.GetDocument("B").Child(0).Child(1).Child(1);
                Assert.AreEqual(1, node.Children.Count());
                using (TextReader text = new StringReader("abc<def>ghi</def>jkl"))
                {
                    XmlReaderSettings settings = new XmlReaderSettings();
                    settings.ConformanceLevel = ConformanceLevel.Auto;
                    using (XmlReader reader = XmlReader.Create(text, settings))
                    {
                        node.Prepend(reader);
                    }
                }
                Assert.AreEqual(3, node.Children.Count());
                Assert.AreEqual("abc", node.Child(0).Value);
                Assert.AreEqual("def", node.Child(1).Name);
                Assert.AreEqual("jklBbb", node.Child(2).Value);
            }
        }

        [Test]
        public void InsertAfter()
        {
            Common.Reset();
            using (Database database = new Database(Common.DatabaseName))
            {
                Common.Populate(database, "A", "B", "C", "D");

                //Valid node insertion with text merge
                TreeNode node = database.GetDocument("B").Child(0).Child(1).Child(1).Child(0);
                using (TextReader text = new StringReader("abc<def>ghi</def>jkl"))
                {
                    XmlReaderSettings settings = new XmlReaderSettings();
                    settings.ConformanceLevel = ConformanceLevel.Auto;
                    using (XmlReader reader = XmlReader.Create(text, settings))
                    {
                        node.InsertAfter(reader);
                    }
                }
                node = database.GetDocument("B").Child(0).Child(1).Child(1);
                Assert.AreEqual(3, node.Children.Count());
                Assert.AreEqual("Bbbabc", node.Child(0).Value);
                Assert.AreEqual("def", node.Child(1).Name);
                Assert.AreEqual("jkl", node.Child(2).Value);
            }
        }

        [Test]
        public void InsertBefore()
        {
            Common.Reset();
            using (Database database = new Database(Common.DatabaseName))
            {
                Common.Populate(database, "A", "B", "C", "D");

                //Valid node insertion with text merge
                TreeNode node = database.GetDocument("B").Child(0).Child(1).Child(1).Child(0);
                using (TextReader text = new StringReader("abc<def>ghi</def>jkl"))
                {
                    XmlReaderSettings settings = new XmlReaderSettings();
                    settings.ConformanceLevel = ConformanceLevel.Auto;
                    using (XmlReader reader = XmlReader.Create(text, settings))
                    {
                        node.InsertBefore(reader);
                    }
                }
                node = database.GetDocument("B").Child(0).Child(1).Child(1);
                Assert.AreEqual(3, node.Children.Count());
                Assert.AreEqual("abc", node.Child(0).Value);
                Assert.AreEqual("def", node.Child(1).Name);
                Assert.AreEqual("jklBbb", node.Child(2).Value);
            }
        }

        [Test]
        public void GetInnerXml()
        {
            Common.Reset();
            using (Database database = new Database(Common.DatabaseName))
            {
                Common.Populate(database, "A", "B", "C", "D");

                //Serialization process puts out all quotes as double-quotes
                //Need to convert single-quotes to double-quotes for comparison

                Document doc = database.GetDocument("B");
                Assert.AreEqual(Common.GenerateXmlContent("B").Replace('\'', '"'), doc.InnerXml);

                Element node = (Element)database.GetDocument("C").Child(0).Child(1);
                Assert.AreEqual("<CBA>Cba</CBA><CBB a=\"Cbba\" b=\"Cbbb\" c=\"Cbbc\">Cbb</CBB>", node.InnerXml);
            }
        }

        [Test]
        public void SetInnerXml()
        {
            Common.Reset();
            using (Database database = new Database(Common.DatabaseName))
            {
                Common.Populate(database, "A", "B", "C", "D");

                Element node = (Element)database.GetDocument("B").Child(0).Child(1).Child(1);
                Assert.AreEqual(1, node.Children.Count());
                node.InnerXml = "abc<def a='b'>ghi</def>jkl";
                Assert.AreEqual(3, node.Children.Count());
                Assert.AreEqual("abc<def a=\"b\">ghi</def>jkl", node.InnerXml);
            }
        }

        [Test]
        public void GetOuterXml()
        {
            Common.Reset();
            using (Database database = new Database(Common.DatabaseName))
            {
                Common.Populate(database, "A", "B", "C", "D");

                //Serialization process puts out all quotes as double-quotes
                //Need to convert single-quotes to double-quotes for comparison

                Document doc = database.GetDocument("B");
                Assert.AreEqual(Common.GenerateXmlContent("B").Replace('\'', '"'), doc.OuterXml);

                Element node = (Element)database.GetDocument("C").Child(0).Child(1);
                Assert.AreEqual("<CB d=\"Cbd\" e=\"Cbe\"><CBA>Cba</CBA><CBB a=\"Cbba\" b=\"Cbbb\" c=\"Cbbc\">Cbb</CBB></CB>", node.OuterXml);
            }
        }

        [Test]
        public void GetInnerText()
        {
            Common.Reset();
            using (Database database = new Database(Common.DatabaseName))
            {
                Common.Populate(database, "A", "B", "C", "D");

                Document doc = database.GetDocument("B");
                Assert.AreEqual("BaBbaBbb", doc.InnerText);

                using (TextReader reader = doc.InnerTextReader)
                {
                    Assert.AreEqual("BaBbaBbb", reader.ReadToEnd());
                }

                Element node = (Element)database.GetDocument("C").Child(0).Child(1);
                Assert.AreEqual("CbaCbb", node.InnerText);
            }
        }

        [Test]
        public void SetInnerText()
        {
            Common.Reset();
            using (Database database = new Database(Common.DatabaseName))
            {
                Common.Populate(database, "A", "B", "C", "D");

                Element node = (Element)database.GetDocument("C").Child(0).Child(1).Child(1);
                node.InnerText = "1234";
                Document doc = database.GetDocument("C");
                Assert.AreEqual(Common.GenerateXmlContent("C").Replace(">Cbb<", ">1234<").Replace('\'', '"'), doc.OuterXml);
            }
        }

        [Test]
        public void GetName()
        {
            Common.Reset();
            using (Database database = new Database(Common.DatabaseName))
            {
                Common.Populate(database, "A", "B", "C", "D");
                
                Node element = database.GetDocument("C").Child(0).Child(1);
                Assert.AreEqual("CB", element.Name);

                Node attribute = database.GetDocument("A").Child(0).Child(0).Attributes.First();
                Assert.AreEqual("a", attribute.Name);

                Node text = database.GetDocument("D").Child(0).Child(0).Child(0);
                Assert.IsEmpty(text.Name);

                Document doc = database.GetDocument("B");
                Assert.AreEqual("B", doc.Name);

                //TODO: processing instruction

                //TODO: comment
            }
        }

        [Test]
        public void SetName()
        {
            Common.Reset();
            using (Database database = new Database(Common.DatabaseName))
            {
                Common.Populate(database, "A", "B", "C", "D");
                
                Node element = database.GetDocument("C").Child(0).Child(1);
                Assert.AreEqual("CB", element.Name);
                element.Name = "XYZ";
                Assert.AreEqual("XYZ", element.Name);

                Node attribute = database.GetDocument("A").Child(0).Child(0).Attributes.First();
                Assert.AreEqual("a", attribute.Name);
                attribute.Name = "xyz";
                Assert.AreEqual("xyz", attribute.Name);

                Node text = database.GetDocument("D").Child(0).Child(0).Child(0);
                Assert.IsEmpty(text.Name);
                text.Name = "abc";
                Assert.IsEmpty(text.Name);

                Document doc = database.GetDocument("B");
                Assert.AreEqual("B", doc.Name);
                doc.Name = "X";
                Assert.AreEqual("X", doc.Name);
                CollectionAssert.AreEquivalent(new []{"A", "X", "C", "D"}, database.DocumentNames);
                doc = database.GetDocument("B");
                Assert.IsNull(doc);
                doc = database.GetDocument("X");
                Assert.IsNotNull(doc);

                //TODO: processing instruction

                //TODO: comment
            }
        }

        [Test]
        public void QName()
        {
            //TODO
        }

        [Test]
        public void GetValue()
        {
            Common.Reset();
            using (Database database = new Database(Common.DatabaseName))
            {
                Common.Populate(database, "A", "B", "C", "D");
                
                Node document = database.GetDocument("B");
                Assert.AreEqual("BaBbaBbb", document.Value);

                Node element = database.GetDocument("C").Child(0).Child(1);
                Assert.AreEqual("CbaCbb", element.Value);

                Node attribute = database.GetDocument("A").Child(0).Child(0).Attributes.First();
                Assert.AreEqual("Aaa", attribute.Value);

                Node text = database.GetDocument("D").Child(0).Child(0).Child(0);
                Assert.AreEqual("Da", text.Value);

                // TODO: processing instruction

                // TODO: comment
            }
        }

        [Test]
        public void SetValue()
        {
            Common.Reset();
            using (Database database = new Database(Common.DatabaseName))
            {
                Common.Populate(database, "A", "B", "C", "D");

                Document document = database.GetDocument("A");
                document.Value = "123";
                Assert.AreEqual(1, document.Children.Count());
                Assert.AreEqual("123", document.InnerXml);

                Element element = (Element)database.GetDocument("B").Child(0).Child(1);
                element.Value = "456";
                Assert.AreEqual("<BB d=\"Bbd\" e=\"Bbe\">456</BB>", element.OuterXml);

                Nxdb.Attribute attribute = database.GetDocument("C").Child(0).Child(0).Attributes.First();
                attribute.Value = "789";
                Assert.AreEqual("789", ((Element)database.GetDocument("C").Child(0).Child(0)).AttributeValue(attribute.Name));

                Node text = database.GetDocument("D").Child(0).Child(0).Child(0);
                text.Value = "54321";
                Assert.AreEqual(Common.GenerateXmlContent("D").Replace(">Da<", ">54321<").Replace('\'', '"'), database.GetDocument("D").OuterXml);

                // TODO: processing instruction

                // TODO: comment
            }
            
        }
    }
}
