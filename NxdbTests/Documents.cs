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
    public class Documents
    {
        public List<string> Names;
        public List<string> Content;

        public Documents(List<string> names, List<string> content)
        {
            Names = names;
            Content = content;
        }

        public void Verify(Database database)
        {
            CollectionAssert.AreEquivalent(Names, database.DocumentNames);  //Ordering doesn't matter - database reorders documents on certain operations
            for(int c = 0 ; c < Names.Count ; c++)
            {
                Document doc = database.GetDocument(Names[c]);

                //Serialization process puts out all quotes as double-quotes
                //Need to convert single-quotes to double-quotes for comparison

                Assert.AreEqual(Content[c].Replace('\'', '"'), doc.OuterXml);
            }
        }
    }
}
