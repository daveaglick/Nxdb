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
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Nxdb.Node;

namespace Nxdb.Persistence
{
    // Storing with this persister is destructive. That is, a store operation will delete all the current
    // content of the node and replace it with new content. This is invalidate all child nodes on store.
    // If any other objects are attached to child nodes, they will be automatically detached when this
    // object is stored due to the child node invalidation.
    // TODO: Once the matching algorithm has been reimplemented, use that for store operations instead of total replacement
    public class XmlSerializerPersister : Persister
    {
        private readonly XmlWriterSettings _writerSettings;

        public XmlSerializerPersister(XmlWriterSettings writerSettings)
        {
            _writerSettings = writerSettings;
        }

        public XmlSerializerPersister()
        {
            _writerSettings = Helper.WriterSettings.Clone();
        }

        internal override void Fetch(Element element, object target, TypeCache typeCache, Cache cache)
        {
            if (target == null) return;

            // Deserialize the object
            XmlSerializer serializer = new XmlSerializer(typeCache.Type);
            object deserialized;
            using(TextReader reader = new StringReader(element.OuterXml))
            {
                deserialized = serializer.Deserialize(reader);
            }

            // Deep copy the deserialized object to the target object
            foreach(FieldInfo field in typeCache.Fields)
            {
                object value = field.GetValue(deserialized);
                field.SetValue(target, value);
            }
        }

        internal override object Serialize(object source, TypeCache typeCache, Cache cache)
        {
            if (source == null) return null;

            // Serialize the object
            // Use an empty namespace object to prevent default namespace declarations
            XmlSerializer serializer = new XmlSerializer(typeCache.Type);
            XmlSerializerNamespaces namespaces = new XmlSerializerNamespaces();
            namespaces.Add(String.Empty, String.Empty);
            StringBuilder content = new StringBuilder();
            using (XmlWriter writer = XmlWriter.Create(content, _writerSettings))
            {
                serializer.Serialize(writer, source, namespaces);
            }
            return content.ToString();
        }

        internal override void Store(Element element, object serialized, object source, TypeCache typeCache, Cache cache)
        {
            if (source == null || serialized == null) return;

            // Replace the element content in the database with the new content
            using (TextReader textReader = new StringReader((string)serialized))
            {
                using(XmlReader reader = XmlReader.Create(textReader))
                {
                    // Move to the root element
                    reader.MoveToContent();

                    // Replace all attributes
                    element.RemoveAllAttributes();
                    if(reader.HasAttributes)
                    {
                        while(reader.MoveToNextAttribute())
                        {
                            element.InsertAttribute(reader.Name, reader.Value);
                        }
                        reader.MoveToElement();
                    }
                        
                    // Replace the child content
                    // Need to use an intermediate string since there is no way to
                    // get the "inner XML" of an XmlReader without getting the
                    // parent element too
                    element.InnerXml = reader.ReadInnerXml();
                }
            }
        }
    }
}
