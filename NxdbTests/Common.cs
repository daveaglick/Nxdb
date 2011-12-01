using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using NUnit.Framework;
using Nxdb;

namespace NxdbTests
{
    [SetUpFixture]
    public class Common
    {
        public static readonly string Path = System.IO.Path.Combine(Environment.CurrentDirectory, "NxdbData");
        public const string DatabaseName = "TestDatabase";

        [SetUp]
        public void SetUp()
        {
            //Set the home path (and clear any previous test data)
            Reset();
            NxDatabase.SetHome(Path);
        }

        [TearDown]
        public void TearDown()
        {
        }

        //Remove the previous database - every test should stand in isolation
        public static void Reset()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, true);
            }
        }

        public static string GenerateXmlContent(string name)
        {
            return String.Format("<{0}><{0}A a='attr_{0}a'>{0}a</{0}A><{0}B><{0}BA>{0}ba</{0}BA><{0}BB b='attr_{0}bb'>{0}bb</{0}BB></{0}B></{0}>", name);
        }

        public static Documents Populate(NxDatabase database, params string[] names)
        {
            Documents documents = new Documents(new List<string>(names), new List<string>());
            foreach (string name in names)
            {
                string content = GenerateXmlContent(name);
                documents.Content.Add(content);
                Assert.IsTrue(database.Add(name, content));
            }
            return documents;
        }
    }
}
