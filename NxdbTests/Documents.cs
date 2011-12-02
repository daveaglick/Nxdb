using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Nxdb;

namespace NxdbTests
{
    public class Documents
    {
        public List<string> Names;
        public List<string> Content;

        public Documents(List<string> names, List<string> content)
        {
            Names = names;
            Content = content;
        }

        public void Verify(NxDatabase database)
        {
            CollectionAssert.AreEquivalent(Names, database.DocumentNames);  //Ordering doesn't matter - database reorders documents on certain operations

            //TODO: Verify content of all documents
        }
    }
}
