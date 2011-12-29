using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using org.basex.core;

namespace Nxdb
{
    public static class Properties
    {
        public static bool ChopWhitespace
        {
            get { return Is(Database.Context.prop, Prop.CHOP); }
            set { Set(Database.Context.prop, Prop.CHOP, value); }
        }

        public static bool UsePathIndex
        {
            get { return Is(Database.Context.prop, Prop.PATHINDEX); }
            set { Set(Database.Context.prop, Prop.PATHINDEX, value); }
        }

        public static bool UseTextIndex
        {
            get { return Is(Database.Context.prop, Prop.TEXTINDEX); }
            set { Set(Database.Context.prop, Prop.TEXTINDEX, value); }
        }

        public static bool UseAttributeIndex
        {
            get { return Is(Database.Context.prop, Prop.ATTRINDEX); }
            set { Set(Database.Context.prop, Prop.ATTRINDEX, value); }
        }

        public static bool UseFullTextIndex
        {
            get { return Is(Database.Context.prop, Prop.FTINDEX); }
            set { Set(Database.Context.prop, Prop.FTINDEX, value); }
        }

        public static bool UpdateIndexes
        {
            get { return Is(Database.Context.prop, Prop.UPDINDEX); }
            set { Set(Database.Context.prop, Prop.UPDINDEX, value); }
        }

        public static bool DropOnDispose
        {
            get { return Is(Database.Properties, NxdbProp.DROPONDISPOSE); }
            set { Set(Database.Properties, NxdbProp.DROPONDISPOSE, value); }
        }

        public static bool OptimizeAfterUpdates
        {
            get { return Is(Database.Properties, NxdbProp.OPTIMIZEAFTERUPDATES); }
            set { Set(Database.Properties, NxdbProp.OPTIMIZEAFTERUPDATES, value); }
        }

        private static bool Is(AProp prop, object[] key)
        {
            return prop.@is(key);
        }

        private static string Get(AProp prop, object[] key)
        {
            return prop.get(key);
        }

        private static void Set(AProp prop, object[] key, bool value)
        {
            prop.set(key, value);
            prop.write();
        }

        private static void Set(AProp prop, object[] key, string value)
        {
            prop.set(key, value);
            prop.write();
        }
    }

    public class NxdbProp : AProp
    {
        public static readonly object[] DROPONDISPOSE = { "DROPONDISPOSE", new java.lang.Boolean(false) };
        public static readonly object[] OPTIMIZEAFTERUPDATES = { "OPTIMIZEAFTERUPDATES", new java.lang.Boolean(true) };

        internal NxdbProp() : base(".nxdb")
        {
        }
    }
}
