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
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Nxdb.Node;

namespace Nxdb.Persistence
{
    public class XmlSerializerPersistenceAttribute : PersistenceAttribute
    {
        private XmlSerializerBehavior _behavior = null;

        public bool Indent { get; set; }

        internal override PersistenceBehavior Behavior
        {
            get 
            {
                if(_behavior == null)
                {
                    XmlWriterSettings writerSettings = Helper.WriterSettings.Clone();
                    writerSettings.Indent = Indent;
                    _behavior = new XmlSerializerBehavior(writerSettings);
                }
                return _behavior;
            }
        }
    }

    // Storing with this behavior is destructive. That is, a store operation will delete all the current
    // content of the node and replace it with new content. This is invalidate all child nodes on store.
    // If any other objects are attached to child nodes, they will be automatically detached when this
    // object is stored due to the child node invalidation.
    // TODO: Once the matching algorithm has been reimplemented, use that for store operations instead of total replacement
    public class XmlSerializerBehavior : PersistenceBehavior
    {
        private readonly XmlWriterSettings _writerSettings;

        public XmlSerializerBehavior(XmlWriterSettings writerSettings)
        {
            _writerSettings = writerSettings;
        }

        public XmlSerializerBehavior()
        {
            _writerSettings = Helper.WriterSettings.Clone();
        }

        internal override void Fetch(Element element, object obj, TypeCache typeCache)
        {
        }

        internal override void Store(Element element, object obj, TypeCache typeCache)
        {
            // Serialize the object
            // Use an empty namespace object to prevent default namespace declarations
            XmlSerializer serializer = new XmlSerializer(typeCache.Type);
            XmlSerializerNamespaces namespaces = new XmlSerializerNamespaces();
            namespaces.Add(String.Empty, String.Empty);
            StringBuilder content = new StringBuilder();
            using (XmlWriter writer = XmlWriter.Create(content, _writerSettings))
            {   
                serializer.Serialize(writer, obj, namespaces);
            }

            // Replace the element content in the database with the new content
            using(new Updates())
            {
                using(TextReader textReader = new StringReader(content.ToString()))
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
}
