using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Nxdb;

namespace NxdbTests
{
    class Program
    {
        private static int _indent = 0;

        static void Main(string[] args)
        {
            //Set the home path (and clear any previous test data)
            string path = Path.Combine(Environment.CurrentDirectory, "NxdbData");
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }
            NxDatabase.SetHome(path);

            //Create a new database
            Test(() => CreateTests("DB1"), "Create Database DB1");

            //Reopen the database
            Test(() => ReopenTests("DB1"), "Reopen Database DB1");

            //Test nodes
            Test(() => NodeTests("DB1"), "Node Tests Database DB1");

            //Drop the database
            Test(() => NxDatabase.Drop("DB1"), "Drop Database DB1");

            //Wait
            Console.WriteLine("All tests complete, press any key...");
            Console.ReadKey();
        }

        //This batch of tests is run the first time the database is opened
        static bool CreateTests(string name)
        {
            //Create the database
            using (NxDatabase database = new NxDatabase(name))
            {
                Test(() => database.Add("TestA", "<TestA><Root>abcd</Root></TestA>"), "Add TestA");
            }

            return true;
        }

        //This batch of tests is run on reopen
        static bool ReopenTests(string name)
        {
            //Reopen the database
            using (NxDatabase database = new NxDatabase(name))
            {
                Test(() => database.Add("Nested/TestB", "<TestB><Root>abcd</Root></TestB>"), "Add TestB");
                Test(() => database.Replace("TestA", "<TestC><Root>abcd</Root></TestC>"), "Replace TestA with TestC");
                Test(() => database.Rename("TestC", "TestD"), "Rename TestC -> TestD");
            }
            
            return true;
        }

        //Test node fetching and traversal
        static bool NodeTests(string name)
        {
            using (NxDatabase database = new NxDatabase(name))
            {
                NxNode docNode = database.Get("TestC");
                NxNode childNode = database.Get("TestC/Root");
            }
            return true;
        }

        static void Test(Func<bool> func, string name)
        {
            Indent();
            Console.WriteLine(name + ":");
            _indent++;
            try
            {
                if (!func())
                {
                    Indent();
                    Console.WriteLine("Unknown failure");
                    Indent();
                    Console.WriteLine("Press any key...");
                    Console.ReadKey();
                    Environment.Exit(0);
                }
                else
                {
                    Indent();
                    Console.WriteLine("Success!");
                }
            }
            catch (Exception ex)
            {
                Indent();
                Console.WriteLine("Exception failure: " + ex.Message);
                Indent();
                Console.WriteLine("Press any key...");
                Console.ReadKey();
                Environment.Exit(0);
            }
            _indent--;
        }

        static void Indent()
        {
            for(int c = 0 ; c < _indent ; c++)
            {
                Console.Write("  ");
            }
        }
    }
}
