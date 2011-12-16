using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using ikvm.@internal;
using java.io;
using Nxdb.Io;
using java.lang;
using java.math;
using javax.xml.datatype;
using javax.xml.@namespace;
using org.basex.api.xmldb;
using org.basex.core;
using org.basex.core.cmd;
using org.basex.data;
using org.basex.io;
using org.basex.query;
using org.basex.query.item;
using org.basex.query.iter;
using org.basex.query.up.primitives;
using org.basex.util;
using org.basex.build;
using org.basex.util.list;
using org.xmldb.api;
using Exception = System.Exception;
using File = java.io.File;
using String = System.String;
using StringReader = java.io.StringReader;
using Type = org.basex.query.item.Type;

namespace Nxdb
{
    // Represents a single BaseX database into which all documents should be stored
    // Unlike the previous versions, it's recommended just one overall NxDatabase be used
    // and querying between them is not supported
    // The documents in the database can be grouped using paths in the document name,
    // i.e.: "folderA/docA.xml", "folderA/docB.xml", "folderB/docC.xml"
    public class Database : IDisposable, IEquatable<Database>
    {
        private static string _home = null;

        public static void SetHome(string path)
        {
            if (path == null) throw new ArgumentNullException("path");
            if (path == String.Empty) throw new ArgumentException("path");

            _home = path;

            // Create the home path
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            // Set the home path so the BaseX preference file will go there (rather than user path)
            Prop.HOME = Path.Combine(_home, "pref");
        }

        public static bool Drop(string name)
        {
            if (name == null) throw new ArgumentNullException("name");
            if (name == String.Empty) throw new ArgumentException("name");

            return Run(new DropDB(name), GetContext());
        }

        private static Context GetContext()
        {
            // Set a default home if one hasn't been provided yet
            if (_home == null)
            {
                SetHome(Path.Combine(Environment.CurrentDirectory, "Nxdb"));
            }

            // Now we can create the context since the path for preferences has been set
            Context context = new Context();

            // Now set the database path
            context.mprop.set(MainProp.DBPATH, _home);

            return context;
        }

        private static bool _initialized = false;

        //This runs some one-time initialization
        //Useful because several Java classes use reflection on their first construction
        //which hurts performance when the first construction is done during an operation
        private static void Initialize(Context context)
        {
            if (!_initialized)
            {
                new QueryContext(context);
                new FElem(new QNm("init".Token()));
                _initialized = true;
            }
        }

        private readonly Context _context;

        public Database(string name)
        {
            if (name == null) throw new ArgumentNullException("name");
            if (name == String.Empty) throw new ArgumentException("name");
            
            _context = GetContext();

            Initialize(_context);

            // Open/create the requested database
            if(!Run(new Open(name)))
            {
                // Unable to open it, try creating
                if (!Run(new CreateDB(name)))
                {
                    throw new ArgumentException();
                }
            }
        }

        public void Dispose()
        {
            Run(new Close());
        }

        /// <summary>
        /// Indicates whether the current database is equal to another database.
        /// </summary>
        /// <param name="other">A database to compare with this one.</param>
        /// <returns>
        /// true if the current database is equal to the <paramref name="other"/> parameter; otherwise, false.
        /// </returns>
        public bool Equals(Database other)
        {
            if(other == null)
            {
                return false;
            }
            return Data.Equals(other.Data);
        }

        /// <summary>
        /// Determines whether the specified <see cref="System.Object"/> is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="System.Object"/> to compare with this instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="System.Object"/> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        /// <exception cref="T:System.NullReferenceException">
        /// The <paramref name="obj"/> parameter is null.
        ///   </exception>
        public override bool Equals(object obj)
        {
            Database other = obj as Database;
            return other != null && Equals(other);
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode()
        {
            return Data.GetHashCode();
        }

        // BaseX commands can be run in one of two ways:
        // 1) An instance of the command class can be created and passed to the following Run() method
        // 2) When the above won't work, a Func or Action can be used to wrap a command and run it

        internal static bool Run(Command command, Context context)
        {
            bool ret = command.run(context);
            string info = command.info();
            Debug.WriteLine(info);  //TODO: Replace this with some kind of logging mechanism
            return ret;
        }

        internal static bool Run(Func<Context, string> func, Context context)
        {
            return Run(new FuncCommand(func), context);
        }

        internal static bool Run(Action<Context> action, Context context)
        {
            return Run(new FuncCommand(action), context);
        }

        internal bool Run(Command command)
        {
            return Run(command, _context);
        }

        internal bool Run(Func<Context, string> func)
        {
            return Run(new FuncCommand(func));
        }

        internal bool Run(Action<Context> action)
        {
            return Run(new FuncCommand(action));
        }

        // The following perform the same operation as their equivalent BaseX command:
        // http://docs.basex.org/wiki/Commands
        
        public void Delete(string name)
        {
            //TODO: Consider switching to the XQuery DBDelete update primitive (don't forget to check FNDb.java for full logic of function)
            Run(new Delete(name));
        }

        public void Rename(string name, string newName)
        {
            //TODO: Consider switching to the XQuery DBDelete update primitive (don't forget to check FNDb.java for full logic of function)
            Run(new Rename(name, newName));
        }

        public void Optimize()
        {
            //TODO: Consider switching to the XQuery DBOptimize update primitive (don't forget to check FNDb.java for full logic of function)

            Run(new Optimize());
        }

        public void OptimizeAll()
        {
            //TODO: Consider switching to the XQuery DBOptimize update primitive (don't forget to check FNDb.java for full logic of function)

            Run(new OptimizeAll());
        }

        // Non-command methods
        
        public virtual void Add(string name, XmlReader xmlReader)
        {
            if (xmlReader == null) throw new ArgumentNullException("xmlReader");
            Add(name, Helper.GetNodeCache(xmlReader));
        }

        public virtual void Add(string name, string content)
        {
            Helper.CallWithString(content, name, Add);
        }

        public void Add(string name, params Document[] nodes)
        {
            if (nodes == null) throw new ArgumentNullException("nodes");
            Add(name, Helper.GetNodeCache(nodes));
        }

        public void Add(string name, IEnumerable<Document> nodes)
        {
            if (nodes == null) throw new ArgumentNullException("nodes");
            Add(name, Helper.GetNodeCache(nodes.Cast<Node>()));
        }

        private void Add(string name, NodeCache nodeCache)
        {
            if (nodeCache != null)
            {
                FDoc doc = new FDoc(nodeCache, name.Token());
                using (new UpdateContext())
                {
                    UpdateContext.AddUpdate(new DBAdd(Data, null, doc, name, Context), Context);
                }
            }
        }

        public virtual void Replace(string name, XmlReader xmlReader)
        {
            if (xmlReader == null) throw new ArgumentNullException("xmlReader");
            Replace(name, Helper.GetNodeCache(xmlReader));
        }

        public virtual void Replace(string name, string content)
        {
            Helper.CallWithString(content, name, Replace);
        }

        public void Replace(string name, params Document[] nodes)
        {
            if (nodes == null) throw new ArgumentNullException("nodes");
            Replace(name, Helper.GetNodeCache(nodes));
        }

        public void Replace(string name, IEnumerable<Document> nodes)
        {
            if (nodes == null) throw new ArgumentNullException("nodes");
            Replace(name, Helper.GetNodeCache(nodes.Cast<Node>()));
        }

        private void Replace(string name, NodeCache nodeCache)
        {
            using(new UpdateContext())
            {
                //This is partially ported from FNDb.replace()
                int pre = Data.doc(name);
                if(pre != -1)
                {
                    if (Data.docs(name).size() != 1) throw new ArgumentException("Simple document expected as replacement target");
                    UpdateContext.AddUpdate(new DeleteNode(pre, Data, null), Context);  
                    Add(name, nodeCache);
                }
            }
        }
        
        internal Context Context
        {
            get { return _context; }
        }

        internal Data Data
        {
            get { return _context.data(); }
        }

        public Document GetDocument(string name)
        {
            int pre = Data.doc(name);
            return pre == -1 ? null : (Document)Node.Get(pre, this);
        }

        public IEnumerable<Document> Documents
        {
            get
            {
                IntList il = Data.docs();
                for(int c = 0 ; c < il.size() ; c++ )
                {
                    int pre = il.get(c);
                    yield return (Document)Node.Get(pre, this);
                }
            }
        }

        public IEnumerable<string> DocumentNames
        {
            get
            {
                IntList il = Data.docs();
                for (int c = 0; c < il.size(); c++)
                {
                    int pre = il.get(c);
                    yield return Data.text(pre, true).Token();
                }
            }
        }

        internal int GetId(int pre)
        {
            return Data.id(pre);
        }

        internal int GetPre(int id)
        {
            return Data.pre(id);
        }

        internal long GetTime()
        {
            return Data.meta.time;
        }

        internal int GetSize()
        {
            return Data.meta.size;
        }
        
        // Gets a Data instance for a given content stream
        
        // Not sure if this is needed - in every case so far, I go straight to a NodeCache

        //internal Data GetData(string name, string content)
        //{
        //    return Helper.CallWithString<string, Data>(content, name, GetData);
        //}

        //internal Data GetData(string name, XmlReader reader)
        //{
        //    if (name == null) throw new ArgumentNullException("name");
        //    if (name == String.Empty) throw new ArgumentException("name");
        //    if (reader == null) throw new ArgumentNullException("reader");
        //    Builder builder = new MemBuilder(name, new XmlReaderParser(name, reader), Context.prop);
        //    // TODO: Use DiskData for larger documents (but how to tell if a stream is going to be large?!)
        //    Data data = null;
        //    try
        //    {
        //        data = builder.build();
        //        if (data.meta.size > 1)
        //        {
        //            builder.close();
        //            return data;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Debug.WriteLine(ex.Message);  // TODO: Replace this with some kind of logging mechanism
        //    }
        //    finally
        //    {
        //        try { builder.close(); } catch { }
        //        if (data != null) try { data.close(); } catch { }
        //        // TODO: Drop the temp database if using DiskData
        //    }
        //    return null;
        //}

        // A cache of all constructed DOM nodes for this collection
        // Needed because .NET XML DOM consumers probably expect one object per node instead of the on the fly creation that Nxdb uses
        // This ensures reference equality for equivalent NxNodes
        // Key = node Id, Value = WeakReference to XmlNode instance
        private readonly Dictionary<int, WeakReference> _domCache = new Dictionary<int, WeakReference>();

        internal Dictionary<int, WeakReference> DomCache
        {
            get { return _domCache; }
        }
    }
}
