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
using Nxdb.Node;

namespace Nxdb.Persistence
{
    public class DefaultPersistenceAttribute : PersistenceAttribute
    {
        private readonly DefaultBehavior _behavior = new DefaultBehavior();

        internal override PersistenceBehavior Behavior
        {
            get { return _behavior; }
        }
    }

    // This scans over all instance fields in the object and uses a TypeConverter to convert them from string
    // Values are obtained from the element by first looking for an element and then an attribute with the given field name
    // If any fields cannot be converted an exception is thrown because the entire state was not fetched
    public class DefaultBehavior : PersistenceBehavior
    {
        internal override void Fetch(Element element, object obj, TypeCache typeCache)
        {
            // TODO: If the element that was found for a given field does not contain simple text content, attempt to descend for complex fields

            // Do all conversions before actually setting any values so that any
            // conversion exceptions don't put the object in an inconsistent state
            //List<KeyValuePair<FieldInfo, object>> values = new List<KeyValuePair<FieldInfo, object>>();
            //foreach(FieldInfo fieldInfo in typeCache.Fields)
            //{
            //    object field = fieldInfo.GetValue(obj);
            //    TypeConverter converter = TypeDescriptor.GetConverter(field);
            //    if(!converter.CanConvertFrom(typeof(string))) throw new Exception(
            //        "Can not convert field " + fieldInfo.Name + " of type " + fieldInfo.FieldType.Name + " from string.");
            //    object value = converter.ConvertFromString(text);
            //    values.Add(new KeyValuePair<FieldInfo, object>(fieldInfo, value));
            //}

            //// Now that all conversions have been succesfully performed, set the values
            //foreach (KeyValuePair<FieldInfo, object> value in values)
            //{
            //    value.Key.SetValue(obj, value.Value);
            //}
        }

        // This scans over all instance fields in the object and uses a TypeConverter to convert them to string
        // Values are set in the element by first looking for an element and then an attribute with the given field name
        // If either and element or attribute with the field name is not found, a new element with the field name is created
        // If any fields cannot be converted an exception is thrown because the entire state was not stored
        internal override void Store(Element element, object obj, TypeCache typeCache)
        {
            // TODO: If a field cannot convert to string, attempt to descend and create a nested element

            throw new NotImplementedException();
        }
    }
}
