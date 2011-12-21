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
    internal static class ConversionExtensions
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
                return Node.Get(node);
            }

            // Get the Java object
            object obj = value.toJava();

            // Clean up non-.NET values
            if (obj is BigInteger)
            {
                BigInteger bigInteger = (BigInteger)obj;
                obj = Convert.ToDecimal(bigInteger.toString());
            }
            else if (obj is BigDecimal)
            {
                BigDecimal bigDecimal = (BigDecimal)obj;
                obj = Convert.ToDecimal(bigDecimal.toString());
            }
            else if (obj is XMLGregorianCalendar)
            {
                XMLGregorianCalendar date = (XMLGregorianCalendar)obj;
                date.normalize();   // Normalizes the date to UTC
                obj = XmlConvert.ToDateTime(date.toXMLFormat(), XmlDateTimeSerializationMode.Utc);
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
            //Is it already a Value?
            if (obj is Value)
            {
                return (Value)obj;
            }

            //Is it enumerable (in other words, a sequence)?
            //We only need to support one-dimensional arrays since that's all that XQuery supports
            IEnumerable enumerable = obj as IEnumerable;
            if (enumerable != null)
            {
                Item[] array = enumerable.Cast<object>().Select(ToItem).ToArray();
                return Seq.get(array, array.Length);
            }

            //Get it as an item
            Item item = ToItem(obj);
            if (item == null)
            {
                return Empty.SEQ;
            }
            return item;
        }

        private static Item ToItem(object obj)
        {
            //It it a Node?
            Node node = obj as Node;
            if (node != null)
            {
                return node.ANode;
            }

            // Clean up non-.NET values
            if (obj is Decimal)
            {
                obj = new BigDecimal(obj.ToString());
            }
            else if (obj is DateTime)
            {
                obj = DatatypeFactory.newInstance().newXMLGregorianCalendar(
                    ((DateTime)obj).ToString("yyyy-MM-ddTHH:mm:ss.fffffffzzz"));
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
            return JavaFunc.type(obj).e(obj, null);
        }
    }
}
