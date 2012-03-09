using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Nxdb.Node;

namespace Nxdb.Persistence.Attributes
{
    /// <summary>
    /// Base class for persistent attributes that fetch and store strings. Uses a TypeConverter to convert
    /// to/from string representations.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public abstract class StringBasedPersistentAttribute : PersistentMemberAttribute
    {
        /// <summary>
        /// Gets or sets a default value to use if the specified node isn't found during fetch.
        /// This value is passed to the type converter to create an instance of the target object.
        /// </summary>
        public string Default { get; set; }

        internal override object FetchValue(Element element, object target, TypeCache typeCache, Cache cache)
        {
            string value = FetchValue(element) ?? Default;
            TypeConverter typeConverter = target == null
                ? TypeDescriptor.GetConverter(typeCache.Type) : TypeDescriptor.GetConverter(target);
            if (typeConverter == null) throw new Exception("Could not get TypeConverter for member.");
            if (!typeConverter.CanConvertFrom(typeof(string))) throw new Exception("Can not convert member from string.");
            return typeConverter.ConvertFromString(value);
        }

        internal override object SerializeValue(object source, TypeCache typeCache, Cache cache)
        {
            TypeConverter typeConverter = source == null
                ? TypeDescriptor.GetConverter(typeCache.Type) : TypeDescriptor.GetConverter(source);
            if (typeConverter == null) throw new Exception("Could not get TypeConverter for member.");
            if (!typeConverter.CanConvertTo(typeof(string))) throw new Exception("Can not convert member to string.");
            return typeConverter.ConvertToString(source);
        }

        internal override void StoreValue(Element element, object serialized, object source, TypeCache typeCache, Cache cache)
        {
            StoreValue(element, (string)serialized);
        }

        protected virtual string FetchValue(Element element)
        {
            return null;
        }

        protected virtual void StoreValue(Element element, string value)
        {
        }
    }
}
