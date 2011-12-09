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
    public class NxNodeTest
    {
        [Test]
        public void Validity()
        {
            Common.Reset();
            using (NxDatabase database = new NxDatabase(Common.DatabaseName))
            {
                Common.Populate(database, "A", "B", "C", "D");

                NxNode invalid = database.GetDocument("B").Child(0).Child(1);
                NxNode valid = database.GetDocument("C").Child(0).Child(1);
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
            using (NxDatabase database = new NxDatabase(Common.DatabaseName))
            {
                Common.Populate(database, "A", "B", "C", "D");
                
                NxNode document = database.GetDocument("B");
                Assert.AreEqual(XmlNodeType.Document, document.NodeType);

                NxNode element = database.GetDocument("C").Child(0).Child(1);
                Assert.AreEqual(XmlNodeType.Element, element.NodeType);

                NxNode attribute = database.GetDocument("A").Child(0).Child(0).Attributes.First();
                Assert.AreEqual(XmlNodeType.Attribute, attribute.NodeType);

                NxNode text = database.GetDocument("D").Child(0).Child(0).Child(0);
                Assert.AreEqual(XmlNodeType.Text, text.NodeType);

                //TODO: processing instruction

                //TODO: comment
            }
        }

        [Test]
        public void Attributes()
        {
            Common.Reset();
            using (NxDatabase database = new NxDatabase(Common.DatabaseName))
            {
                Common.Populate(database, "A", "B", "C", "D");

                NxNode node = database.GetDocument("B").Child(0).Child(1).Child(1);
                CollectionAssert.AreEquivalent(new []{"Bbba", "Bbbb", "Bbbc"}, node.Attributes.Select(n => n.Value));
            }
        }

        [Test]
        public void GetAttribute()
        {
            Common.Reset();
            using (NxDatabase database = new NxDatabase(Common.DatabaseName))
            {
                Common.Populate(database, "A", "B", "C", "D");

                NxNode node = database.GetDocument("B").Child(0).Child(1).Child(1);
                Assert.AreEqual("Bbbb", node.AttributeValue("b"));
                Assert.IsEmpty(node.AttributeValue("invalid"));
            }
        }

        [Test]
        public void GetAttributeNode()
        {
            Common.Reset();
            using (NxDatabase database = new NxDatabase(Common.DatabaseName))
            {
                Common.Populate(database, "A", "B", "C", "D");

                NxNode node = database.GetDocument("B").Child(0).Child(1).Child(1);
                Assert.AreEqual("Bbbc", node.Attribute("c").Value);
                Assert.IsNull(node.Attribute("invalid"));
            }
        }

        [Test]
        public void RemoveAllAttributes()
        {
            Common.Reset();
            using (NxDatabase database = new NxDatabase(Common.DatabaseName))
            {
                Common.Populate(database, "A", "B", "C", "D");

                NxNode node = database.GetDocument("B").Child(0).Child(1).Child(1);
                Assert.AreEqual(3, node.Attributes.Count());
                node.RemoveAllAttributes();
                Assert.AreEqual(0, node.Attributes.Count());
            }
        }

        [Test]
        public void RemoveAttribute()
        {
            Common.Reset();
            using (NxDatabase database = new NxDatabase(Common.DatabaseName))
            {
                Common.Populate(database, "A", "B", "C", "D");

                NxNode node = database.GetDocument("B").Child(0).Child(1).Child(1);
                node.RemoveAttribute("b");
                CollectionAssert.AreEquivalent(new[] { "Bbba", "Bbbc" }, node.Attributes.Select(n => n.Value));
            }
        }

        [Test]
        public void InsertAttribute()
        {
            Common.Reset();
            using (NxDatabase database = new NxDatabase(Common.DatabaseName))
            {
                Common.Populate(database, "A", "B", "C", "D");

                NxNode node = database.GetDocument("B").Child(0).Child(1).Child(1);
                node.InsertAttribute("appendName", "appendValue");
                CollectionAssert.AreEquivalent(new[] { "Bbba", "Bbbb", "Bbbc", "appendValue" }, node.Attributes.Select(n => n.Value));
            }
        }

        [Test]
        public void RemoveAll()
        {
            Common.Reset();
            using (NxDatabase database = new NxDatabase(Common.DatabaseName))
            {
                Common.Populate(database, "A", "B", "C", "D");

                NxNode node = database.GetDocument("B").Child(0).Child(1);
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
            using (NxDatabase database = new NxDatabase(Common.DatabaseName))
            {
                Common.Populate(database, "A", "B", "C", "D");

                NxNode node = database.GetDocument("B").Child(0).Child(1);
                
                NxNode child = database.GetDocument("B").Child(0).Child(1).Child(1);
                child.Remove();
                Assert.IsFalse(child.Valid);
                Assert.IsNull(database.GetDocument("B").Child(0).Child(1).Child(1));
                CollectionAssert.AreEqual(new []{ "BBA" }, node.Children.Select(n => n.Name));

                NxNode attribute = database.GetDocument("B").Child(0).Child(1).Attribute("e");
                attribute.Remove();
                Assert.IsFalse(attribute.Valid);
                Assert.IsNull(database.GetDocument("B").Child(0).Child(1).Attribute("e"));
                CollectionAssert.AreEquivalent(new []{ "d" }, node.Attributes.Select(n => n.Name));

                //Text merge node removal
                database.Add("E", "<E>abcd<F>efgh</F>ijkl</E>");
                NxNode root = database.GetDocument("E").Child(0);
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
            using (NxDatabase database = new NxDatabase(Common.DatabaseName))
            {
                Common.Populate(database, "A", "B", "C", "D");

                //Valid node insertion with text merge
                NxNode node = database.GetDocument("B").Child(0).Child(1).Child(1);
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

                //Insertion on a non-element should cause exception
                NxNode attribute = database.GetDocument("B").Child(0).Child(1).Attribute("e");
                using (TextReader text = new StringReader("abc<def>ghi</def>jkl"))
                {
                    XmlReaderSettings settings = new XmlReaderSettings();
                    settings.ConformanceLevel = ConformanceLevel.Auto;
                    using (XmlReader reader = XmlReader.Create(text, settings))
                    {
                        Assert.Throws<InvalidOperationException>(() => attribute.Append(reader));
                    }
                }
            }
        }

        [Test]
        public void Prepend()
        {
            Common.Reset();
            using (NxDatabase database = new NxDatabase(Common.DatabaseName))
            {
                Common.Populate(database, "A", "B", "C", "D");

                //Valid node insertion with text merge
                NxNode node = database.GetDocument("B").Child(0).Child(1).Child(1);
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

                //Insertion on a non-element should cause exception
                NxNode attribute = database.GetDocument("B").Child(0).Child(1).Attribute("e");
                using (TextReader text = new StringReader("abc<def>ghi</def>jkl"))
                {
                    XmlReaderSettings settings = new XmlReaderSettings();
                    settings.ConformanceLevel = ConformanceLevel.Auto;
                    using (XmlReader reader = XmlReader.Create(text, settings))
                    {
                        Assert.Throws<InvalidOperationException>(() => attribute.Prepend(reader));
                    }
                }
            }
        }

        [Test]
        public void InsertAfter()
        {
            Common.Reset();
            using (NxDatabase database = new NxDatabase(Common.DatabaseName))
            {
                Common.Populate(database, "A", "B", "C", "D");

                //Valid node insertion with text merge
                NxNode node = database.GetDocument("B").Child(0).Child(1).Child(1).Child(0);
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
            using (NxDatabase database = new NxDatabase(Common.DatabaseName))
            {
                Common.Populate(database, "A", "B", "C", "D");

                //Valid node insertion with text merge
                NxNode node = database.GetDocument("B").Child(0).Child(1).Child(1).Child(0);
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
        public void Name()
        {
            Common.Reset();
            using (NxDatabase database = new NxDatabase(Common.DatabaseName))
            {
                Common.Populate(database, "A", "B", "C", "D");
                
                NxNode document = database.GetDocument("B");
                Assert.IsEmpty(document.Name);

                NxNode element = database.GetDocument("C").Child(0).Child(1);
                Assert.AreEqual("CB", element.Name);

                NxNode attribute = database.GetDocument("A").Child(0).Child(0).Attributes.First();
                Assert.AreEqual("a", attribute.Name);

                NxNode text = database.GetDocument("D").Child(0).Child(0).Child(0);
                Assert.IsEmpty(text.Name);

                //TODO: processing instruction

                //TODO: comment
            }
        }

        [Test]
        public void Value()
        {
            Common.Reset();
            using (NxDatabase database = new NxDatabase(Common.DatabaseName))
            {
                Common.Populate(database, "A", "B", "C", "D");
                
                NxNode document = database.GetDocument("B");
                Assert.AreEqual("BaBbaBbb", document.Value);

                NxNode element = database.GetDocument("C").Child(0).Child(1);
                Assert.AreEqual("CbaCbb", element.Value);

                NxNode attribute = database.GetDocument("A").Child(0).Child(0).Attributes.First();
                Assert.AreEqual("Aaa", attribute.Value);

                NxNode text = database.GetDocument("D").Child(0).Child(0).Child(0);
                Assert.AreEqual("Da", text.Value);

                //TODO: processing instruction

                //TODO: comment
            }
        }
    }
}
