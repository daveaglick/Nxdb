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
    public class NxNodeTest
    {
        [Test]
        public void Validity()
        {
            Common.Reset();
            using (NxDatabase database = new NxDatabase(Common.DatabaseName))
            {
                Common.Populate(database, "A", "B", "C", "D");
                //TODO: convert to XPath
                NxNode invalid = database.GetDocument("B").ChildNodes.First().ChildNodes.ElementAt(1);
                NxNode valid = database.GetDocument("C").ChildNodes.First().ChildNodes.ElementAt(1);
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

                //TODO: convert to XPath

                NxNode document = database.GetDocument("B");
                Assert.AreEqual(XmlNodeType.Document, document.NodeType);

                NxNode element = database.GetDocument("C").ChildNodes.First().ChildNodes.ElementAt(1);
                Assert.AreEqual(XmlNodeType.Element, element.NodeType);

                NxNode attribute = database.GetDocument("A").ChildNodes.First().ChildNodes.First().Attributes.First();
                Assert.AreEqual(XmlNodeType.Attribute, attribute.NodeType);

                NxNode text = database.GetDocument("D").ChildNodes.First().ChildNodes.First().ChildNodes.First();
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
                //TODO: convert to XPath
                NxNode node = database.GetDocument("B").ChildNodes.First()
                    .ChildNodes.ElementAt(1)
                    .ChildNodes.ElementAt(1);
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
                //TODO: convert to XPath
                NxNode node = database.GetDocument("B").ChildNodes.First()
                    .ChildNodes.ElementAt(1)
                    .ChildNodes.ElementAt(1);
                Assert.AreEqual("Bbbb", node.GetAttribute("b"));
                Assert.IsEmpty(node.GetAttribute("invalid"));
            }
        }

        [Test]
        public void GetAttributeNode()
        {
            Common.Reset();
            using (NxDatabase database = new NxDatabase(Common.DatabaseName))
            {
                Common.Populate(database, "A", "B", "C", "D");
                //TODO: convert to XPath
                NxNode node = database.GetDocument("B").ChildNodes.First()
                    .ChildNodes.ElementAt(1)
                    .ChildNodes.ElementAt(1);
                Assert.AreEqual("Bbbc", node.GetAttributeNode("c").Value);
                Assert.IsNull(node.GetAttributeNode("invalid"));
            }
        }

        [Test]
        public void RemoveAllAttributes()
        {
            Common.Reset();
            using (NxDatabase database = new NxDatabase(Common.DatabaseName))
            {
                Common.Populate(database, "A", "B", "C", "D");
                //TODO: convert to XPath
                NxNode node = database.GetDocument("B").ChildNodes.First()
                    .ChildNodes.ElementAt(1)
                    .ChildNodes.ElementAt(1);
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
                //TODO: convert to XPath
                NxNode node = database.GetDocument("B").ChildNodes.First()
                    .ChildNodes.ElementAt(1)
                    .ChildNodes.ElementAt(1);
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
                //TODO: convert to XPath
                NxNode node = database.GetDocument("B").ChildNodes.First()
                    .ChildNodes.ElementAt(1)
                    .ChildNodes.ElementAt(1);
                node.InsertAttribute("appendName", "appendValue");
                CollectionAssert.AreEquivalent(new[] { "Bbba", "Bbbb", "Bbbc", "appendValue" }, node.Attributes.Select(n => n.Value));
            }
        }

        [Test]
        public void Name()
        {
            Common.Reset();
            using (NxDatabase database = new NxDatabase(Common.DatabaseName))
            {
                Common.Populate(database, "A", "B", "C", "D");

                //TODO: convert to XPath

                NxNode document = database.GetDocument("B");
                Assert.IsEmpty(document.Name);

                NxNode element = database.GetDocument("C").ChildNodes.First().ChildNodes.ElementAt(1);
                Assert.AreEqual("CB", element.Name);

                NxNode attribute = database.GetDocument("A").ChildNodes.First().ChildNodes.First().Attributes.First();
                Assert.AreEqual("a", attribute.Name);

                NxNode text = database.GetDocument("D").ChildNodes.First().ChildNodes.First().ChildNodes.First();
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

                //TODO: convert to XPath

                NxNode document = database.GetDocument("B");
                Assert.AreEqual("BaBbaBbb", document.Value);

                NxNode element = database.GetDocument("C").ChildNodes.First().ChildNodes.ElementAt(1);
                Assert.AreEqual("CbaCbb", element.Value);

                NxNode attribute = database.GetDocument("A").ChildNodes.First().ChildNodes.First().Attributes.First();
                Assert.AreEqual("Aaa", attribute.Value);

                NxNode text = database.GetDocument("D").ChildNodes.First().ChildNodes.First().ChildNodes.First();
                Assert.AreEqual("Da", text.Value);

                //TODO: processing instruction

                //TODO: comment
            }
        }
    }
}
