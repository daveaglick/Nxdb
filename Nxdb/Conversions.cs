﻿/*
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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using java.math;
using javax.xml.datatype;
using javax.xml.@namespace;
using org.basex.query.func;
using org.basex.query.item;

namespace Nxdb
{
    //Extensions to help convert between .NET, Java, and BaseX objects
    internal static class Conversions
    {
        //A couple extension methods to help with the tokenizing of strings
        internal static string Token(this byte[] bytes)
        {
            return org.basex.util.Token.@string(bytes);
        }

        internal static byte[] Token(this string str)
        {
            return org.basex.util.Token.token(str);
        }

        internal static object ToObject(this Value value)
        {
            //Check for null/empty
            if (value == null)
            {
                return null;
            }

            //Is it a sequence?
            Seq seq = value as Seq;
            if (seq != null)
            {
                return new IterEnum(seq.iter());
            }

            // Is it a node?
            ANode node = value as ANode;
            if (node != null)
            {
                return Node.Node.Get(node);
            }

            // Get the Java object
            object obj = value.toJava();

            // Clean up non-.NET values
            if (obj is java.lang.Number)
            {
                int i;
                double d;
                if(int.TryParse(obj.ToString(), out i))
                {
                    obj = i;
                }
                else if(double.TryParse(obj.ToString(), out d))
                {
                    obj = d;
                }
                else if (obj is BigInteger)
                {
                    BigInteger bigInteger = (BigInteger)obj;
                    obj = Convert.ToDecimal(bigInteger.toString());
                }
                else if (obj is BigDecimal)
                {
                    BigDecimal bigDecimal = (BigDecimal)obj;
                    obj = Convert.ToDecimal(bigDecimal.toString());
                }
            }
            else if (obj is XMLGregorianCalendar)
            {
                XMLGregorianCalendar date = (XMLGregorianCalendar)obj;
                obj = XmlConvert.ToDateTime(date.toXMLFormat(), XmlDateTimeSerializationMode.RoundtripKind);
            }
            else if (obj is Duration)
            {
                Duration duration = (Duration)obj;
                obj = XmlConvert.ToTimeSpan(duration.toString());
            }
            else if (obj is QName)
            {
                QName qname = (QName)obj;
                obj = new XmlQualifiedName(qname.getLocalPart(), qname.getNamespaceURI());
            }

            return obj;
        }

        internal static Value ToValue(this object obj)
        {
            //Is it null?
            if (obj == null)
            {
                return null;
            }

            //Is it already a Value?
            if (obj is Value)
            {
                return (Value)obj;
            }

            //Get the item(s)
            Item[] items = GetItems(obj).ToArray();
            if(items.Length == 0)
            {
                return Empty.SEQ;
            }
            return items.Length == 1 ? items[0] : Seq.get(items, items.Length);
        }

        //Converts potential sequences into item lists, while flattening to a single dimension
        private static IEnumerable<Item> GetItems(object obj)
        {
            //It it a Node?
            Node.Node node = obj as Node.Node;
            if (node != null)
            {
                return new[]{node.ANode};
            }

            //Is it a Database?
            Database database = obj as Database;
            if(database != null)
            {
                return database.Documents.Select(d => d.ANode).Cast<Item>();
            }

            //Is it enumerable (list, array, etc. - but not a string!)
            //This is recursive and results in flattening any nested sequences
            IEnumerable enumerable = obj as IEnumerable;
            if (!(obj is string) && enumerable != null)
            {
                return enumerable.Cast<object>().Select(GetItems).SelectMany(x => x);
            }

            // Clean up non-.NET values
            if (obj is Decimal)
            {
                obj = new BigDecimal(obj.ToString());
            }
            else if (obj is DateTime)
            {
                obj = DatatypeFactory.newInstance().newXMLGregorianCalendar(
                    ((DateTime)obj).ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ"));
            }
            else if (obj is TimeSpan)
            {
                obj = DatatypeFactory.newInstance().newDuration(
                    Convert.ToInt64(((TimeSpan)obj).TotalMilliseconds));
            }
            else if (obj is XmlQualifiedName)
            {
                XmlQualifiedName qname = (XmlQualifiedName)obj;
                obj = new QName(qname.Namespace, qname.Name);
            }

            //Get the item
            return new []{JavaMapping.type(obj).cast(obj, null)};
        }

    }
}
