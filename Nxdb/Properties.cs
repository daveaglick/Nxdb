using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using org.basex.core;

namespace Nxdb
{
    public static class Properties
    {
        public static BoolProperty ChopWhitespace = new BoolProperty(Database.Context.prop, Prop.CHOP);
        public static BoolProperty UsePathIndex = new BoolProperty(Database.Context.prop, Prop.PATHINDEX);
        public static BoolProperty UseTextIndex = new BoolProperty(Database.Context.prop, Prop.TEXTINDEX);
        public static BoolProperty UseAttributeIndex = new BoolProperty(Database.Context.prop, Prop.ATTRINDEX);
        public static BoolProperty UseFullTextIndex = new BoolProperty(Database.Context.prop, Prop.FTINDEX);
        public static BoolProperty UpdateIndexes = new BoolProperty(Database.Context.prop, Prop.UPDINDEX);
        public static BoolProperty DropOnDispose = new BoolProperty(Database.Properties, NxdbProp.DROPONDISPOSE);
        public static BoolProperty OptimizeAfterUpdates = new BoolProperty(Database.Properties, NxdbProp.OPTIMIZEAFTERUPDATES);
    }

    public class NxdbProp : AProp
    {
        public static readonly object[] DROPONDISPOSE = { "DROPONDISPOSE", new java.lang.Boolean(false) };
        public static readonly object[] OPTIMIZEAFTERUPDATES = { "OPTIMIZEAFTERUPDATES", new java.lang.Boolean(true) };

        internal NxdbProp() : base(".nxdb")
        {
        }
    }

    public abstract class Property<T>
    {

        private readonly AProp _prop;
        private readonly object[] _key;

        protected Property(AProp prop, object[] key)
        {
            _prop = prop;
            _key = key;
        }

        protected AProp Prop
        {
            get { return _prop; }
        }

        protected object[] Key
        {
            get { return _key; }
        }

        public abstract void Set(T value);
        public abstract T Get();
    }

    public class BoolProperty : Property<bool>
    {
        internal BoolProperty(AProp prop, object[] key)
            : base(prop, key)
        {
        }

        public override void Set(bool value)
        {
            Prop.set(Key, value);
            Prop.write();
        }

        public override bool Get()
        {
            return Prop.@is(Key);
        }
    }

    public class StringProperty : Property<string>
    {
        internal StringProperty(AProp prop, object[] key)
            : base(prop, key)
        {
        }

        public override void Set(string value)
        {
            Prop.set(Key, value);
            Prop.write();
        }

        public override string Get()
        {
            return Prop.get(Key);
        }
    }
}
