using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using java.io;
using Nxdb.Io;
using org.basex.core;
using org.basex.core.cmd;
using org.basex.data;

//TODO - Should the database be disposable? If so, write out the the properties on dispose instead of on set and close/dispose all collections

namespace Nxdb
{
    //The new code - not sure if going to use
    //public class NxManager
    //{
    //    private readonly Context _context = new Context();

    //    public NxManager(string path)
    //    {
    //        if (path == null) throw new ArgumentNullException("path");
    //        if (path == String.Empty) throw new ArgumentException("path");

    //        //Create the directory
    //        if (!Directory.Exists(path))
    //        {
    //            Directory.CreateDirectory(path);
    //        }

    //        //Set the home path
    //        Prop.HOME = path;
    //    }

    //    //Based on org.basex.gui.GUI.execute() methods
    //    internal TextReader Execute(Command command)
    //    {
    //        LsbOutputStream outputStream = new LsbOutputStream();
    //        command.execute(_context, outputStream);
    //        return new LsbReader(outputStream.Buffer);
    //    }
    //}

    /*
    //The old code - for reference
    public class NxManager : IDictionary<string, NxDatabase>
    {
        private readonly string path;

        private readonly Context context;
        internal Context Context
        {
            get { return context; }
        }

        private readonly NxdbProp prop;

        private readonly Dictionary<string, NxDatabase> collections = new Dictionary<string, NxDatabase>();

        public NxManager(string path)
        {
            if (path == null)
            {
                throw new ArgumentNullException("path");
            }
            this.path = path;
            
            //Create the directory
            if(!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            //Create context, and use a hack to put properties files in the database path
            string oldHome = Prop.HOME;
            Prop.HOME = path;
            context = new Context();
            prop = new NxdbProp();
            Prop.HOME = oldHome;
            DatabasePath = path;
        }

        public NxDatabase Add(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }
            if( collections.ContainsKey(name) )
            {
                throw new ArgumentException(name + " already exists");
            }
            NxDatabase database = new NxDatabase(this, name);
            collections.Add(name, database);
            return database;
        }

        public NxDatabase Open(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }
            NxDatabase database;
            if (!collections.TryGetValue(name, out database))
            {
                Data data = org.basex.core.cmd.Open.open(name, context);
                database = new NxDatabase(this, name, data);
                collections.Add(name, database);
            }
            return database;
        }

        public bool Exists(string name)
        {
            if (name == null) throw new ArgumentNullException("name");
            return context.prop.dbexists(name);
        }

        public bool Drop(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }
            if( collections.ContainsKey(name) )
            {
                collections.Remove(name);
            }
            return DropDB.drop(name, context.prop);
        }

        public NxQuery GetQuery(string expression)
        {
            NxQuery query = new NxQuery(this, expression);
            List<NxNode> docNodes = new List<NxNode>();
            foreach(KeyValuePair<string, NxDatabase> kvp in collections)
            {
                query.SetCollection(kvp.Key, kvp.Value);
                docNodes.AddRange(kvp.Value.Values);
            }
            query.SetContext(docNodes);
            return query;
        }

        public IEnumerable<object> Query(string expression)
        {
            return GetQuery(expression).Evaluate();
        }

        public NxQuery GetQuery(string expression, IEnumerable<NxNode> queryContext)
        {
            NxQuery query = new NxQuery(this, expression);
            foreach (KeyValuePair<string, NxDatabase> kvp in collections)
            {
                query.SetCollection(kvp.Key, kvp.Value);
            }
            query.SetContext(queryContext);
            return query;
        }

        public IEnumerable<object> Query(string expression, IEnumerable<NxNode> queryContext)
        {
            return GetQuery(expression, queryContext).Evaluate();
        }

        //Get a query object without a preset context or collections
        public NxQuery GetEmptyQuery(string expression)
        {
            return new NxQuery(this, expression);
        }

        //IDictionary

        public NxDatabase this[string key]
        {
            get { return collections[key]; }
            set { throw new NotSupportedException(); }
        }

        public bool Remove(string key)
        {
            NxDatabase database;
            if( collections.TryGetValue(key, out database) )
            {
                database.Dispose();
            }
            return collections.Remove(key);
        }

        public IEnumerator<KeyValuePair<string, NxDatabase>> GetEnumerator()
        {
            return collections.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(KeyValuePair<string, NxDatabase> item)
        {
            throw new NotSupportedException();
        }

        public void Add(string key, NxDatabase value)
        {
            throw new NotSupportedException();
        }

        public void Clear()
        {
            List<NxDatabase> dispose = new List<NxDatabase>(collections.Values);
            collections.Clear();
            foreach (NxDatabase collection in dispose)
            {
                collection.Dispose();
            }
        }

        public bool Contains(KeyValuePair<string, NxDatabase> item)
        {
            NxDatabase value;
            return collections.TryGetValue(item.Key, out value) && value == item.Value;
        }

        public void CopyTo(KeyValuePair<string, NxDatabase>[] array, int arrayIndex)
        {
            List<KeyValuePair<string, NxDatabase>> source = new List<KeyValuePair<string, NxDatabase>>(this);
            source.CopyTo(array, arrayIndex);
        }

        public bool Remove(KeyValuePair<string, NxDatabase> item)
        {
            if (Contains(item))
            {
                return Remove(item.Key);
            }
            return false;
        }

        public int Count
        {
            get { return collections.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool ContainsKey(string key)
        {
            return collections.ContainsKey(key);
        }

        public bool TryGetValue(string key, out NxDatabase value)
        {
            return collections.TryGetValue(key, out value);
        }

        public ICollection<string> Keys
        {
            get { return collections.Keys; }
        }

        public ICollection<NxDatabase> Values
        {
            get { return collections.Values; }
        }

        public override bool Equals(object obj)
        {
            NxManager other = obj as NxManager;
            if( other != null && path.Equals(other.path))
            {
                return true;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return path.GetHashCode();
        }

        //Properties helpers
        
        private static void SetProperty(AProp aProp, object[] key, string value)
        {
            aProp.set(key, value);
            aProp.write();
        }

        private static string GetPropertyString(AProp aProp, object[] key)
        {
            return aProp.get(key);
        }

        private static void SetProperty(AProp aProp, object[] key, bool value)
        {
            aProp.set(key, value);
            aProp.write();
        }

        private static bool GetPropertyBool(AProp aProp, object[] key)
        {
            return aProp.@is(key);
        }

        //Nxdb properties
        //TODO - Convert this class to internal and expost using InternalsVisibleTo
        public class NxdbProp : AProp
        {
            public NxdbProp() : base(".nxdb") { }

            public static readonly object[] OPTIMIZEONADD = { "OPTIMIZEONADD", new java.lang.Boolean(true) };
            public static readonly object[] OPTIMIZEONREMOVE = { "OPTIMIZEONREMOVE", new java.lang.Boolean(true) };
            public static readonly object[] OPTIMIZEONUPDATE = { "OPTIMIZEONUPDATE", new java.lang.Boolean(true) };
            public static readonly object[] DROPONDISPOSE = { "DROPONDISPOSE", new java.lang.Boolean(true) };
        }

        //Whether collections should be optimized and indexes rebuilt when new documents are added
        //The first document added to a collection is always optimized
        public bool OptimizeCollectionOnAdd
        {
            get { return GetPropertyBool(prop, NxdbProp.OPTIMIZEONADD); }
            set { SetProperty(prop, NxdbProp.OPTIMIZEONADD, value); }
        }

        //Whether collections should be optimized and indexes rebuilt when documents are removed
        public bool OptimizeCollectionOnRemove
        {
            get { return GetPropertyBool(prop, NxdbProp.OPTIMIZEONREMOVE); }
            set { SetProperty(prop, NxdbProp.OPTIMIZEONREMOVE, value); }
        }

        //Whether collections should be optimized and indexes rebuilt when updates are applied
        //TODO - Check queries to see if updates are going to be applied and if so, check this flag to know whether the database needs to be optimized at the end
        public bool OptimizeCollectionOnUpdate
        {
            get { return GetPropertyBool(prop, NxdbProp.OPTIMIZEONUPDATE); }
            set { SetProperty(prop, NxdbProp.OPTIMIZEONUPDATE, value); }
        }

        //Indicates if the database should be deleted from disk when it gets disposed
        public bool DropOnDispose
        {
            get { return GetPropertyBool(prop, NxdbProp.DROPONDISPOSE); }
            set { SetProperty(prop, NxdbProp.DROPONDISPOSE, value); }
        }

        //BaseX properties (these can be found in the Prop class)

        public string DatabasePath
        {
            get { return GetPropertyString(context.prop, Prop.DBPATH); }
            private set { SetProperty(context.prop, Prop.DBPATH, value); }
        }

        public bool ChopWhitespace
        {
            get { return GetPropertyBool(context.prop, Prop.CHOP); }
            set { SetProperty(context.prop, Prop.CHOP, value); }
        }

        public bool UseAttributeIndex
        {
            get { return GetPropertyBool(context.prop, Prop.ATTRINDEX); }
            set { SetProperty(context.prop, Prop.ATTRINDEX, value); }
        }

        public bool UseTextIndex
        {
            get { return GetPropertyBool(context.prop, Prop.TEXTINDEX); }
            set { SetProperty(context.prop, Prop.TEXTINDEX, value); }
        }

        public bool UseFullTextIndex
        {
            get { return GetPropertyBool(context.prop, Prop.FTINDEX); }
            set { SetProperty(context.prop, Prop.FTINDEX, value); }
        }

        public bool UsePathIndex
        {
            get { return GetPropertyBool(context.prop, Prop.PATHINDEX); }
            set { SetProperty(context.prop, Prop.PATHINDEX, value); }
        }

        public bool DatabaseInMemory
        {
            get { return GetPropertyBool(context.prop, Prop.MAINMEM); }
            set { SetProperty(context.prop, Prop.MAINMEM, value); }
        }
    }
     */
}
