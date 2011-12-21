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
using org.basex.core;
using org.basex.core.cmd;
using org.basex.data;
using org.basex.io;
using org.basex.query;
using org.basex.query.func;
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

        //Use one static Context object - all opened databases will get added ("pinned") to the context
        //The primary Context.data() reference should remain null
        private static Context _context = null;

        internal static Context Context
        {
            get
            {
                if (_context == null)
                {
                    // Set a default home if one hasn't been provided yet
                    if (_home == null)
                    {
                        SetHome(Path.Combine(Environment.CurrentDirectory, "Nxdb"));
                    }

                    // Now we can create the context since the path for preferences has been set
                    _context = new Context();

                    // Now set the database path
                    _context.mprop.set(MainProp.DBPATH, _home);
                }
                return _context;
            }
        }

        
        public static void Drop(string name)
        {
            if (name == null) throw new ArgumentNullException("name");
            if (name == String.Empty) throw new ArgumentException("name");

            //Only drop if not currently pinned
            if (Context.pinned(name))
            {
                throw new ArgumentException("Database is currently open, please dispose all references and try again.");
            }

            //Attempt to drop
            if(!DropDB.drop(name, Context.mprop))
            {
                throw new Exception("Could not drop database.");
            }
        }

        private static bool _initialized = false;

        //This runs some one-time initialization
        //Useful because several Java classes use reflection on their first construction
        //which hurts performance when the first construction is done during an operation
        private static void Initialize()
        {
            if (!_initialized)
            {
                Context context = Context;
                new QueryContext(Context);
                new FElem(new QNm("init".Token()));
                _initialized = true;
            }
        }

        private Data _data;
        
        internal Data Data
        {
            get { return _data; }
        }

        public Database(string name)
        {
            if (name == null) throw new ArgumentNullException("name");
            if (name == String.Empty) throw new ArgumentException("name");
            Initialize();

            //Try to open or create the database
            try
            {
                _data = Open.open(name, Context);
            }
            catch (Exception)
            {
                try
                {
                    _data = CreateDB.create(name, Parser.emptyParser(), Context);
                }
                catch(Exception ex)
                {
                    throw new ArgumentException("Could not create database.", ex);
                }
            }
        }

        internal Database(Data data)
        {
            if (data == null) throw new ArgumentNullException("data");
            Initialize();
            _data = data;
        }

        public void Dispose()
        {
            Close.close(Data, Context);
            _data = null;
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
            return other != null && Data.Equals(other.Data);
        }

        /// <summary>
        /// Determines whether the specified <see cref="object"/> is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="object"/> to compare with this instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="object"/> is equal to this instance; otherwise, <c>false</c>.
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
        
        //Many of the following are ported from FNDb.java to eliminate the overhead of the XQuery function evaluation

        public void Delete(string path)
        {
            IntList docs = Data.docs(path);
            using(new Update())
            {
                for (int i = 0, s = docs.size(); i < s; i++)
                {
                    Update.Add(new DeleteNode(docs.get(i), Data, null));
                }
            }
        }

        public void Rename(string path, string newName)
        {
            IntList docs = Data.docs(path);
            using (new Update())
            {
                for (int i = 0, s = docs.size(); i < s; i++)
                {
                    int pre = docs.get(i);
                    string target = org.basex.core.cmd.Rename.target(Data, pre, path, newName);
                    if (!String.IsNullOrEmpty(target))
                    {
                        Update.Add(new ReplaceValue(pre, Data, null, target.Token()));
                    }
                }
            }
        }

        public void Optimize()
        {
            using(new Update())
            {
                Update.Add(new DBOptimize(Data, Context, false, null));
            }
        }

        public void OptimizeAll()
        {
            using (new Update())
            {
                Update.Add(new DBOptimize(Data, Context, true, null));
            }
        }
        
        public virtual void Add(string path, XmlReader xmlReader)
        {
            if (xmlReader == null) throw new ArgumentNullException("xmlReader");
            Add(path, Helper.GetNodeCache(xmlReader));
        }

        public virtual void Add(string path, string content)
        {
            Helper.CallWithString(content, path, Add);
        }

        public void Add(string path, params Document[] nodes)
        {
            if (nodes == null) throw new ArgumentNullException("nodes");
            Add(path, Helper.GetNodeCache(nodes));
        }

        public void Add(string path, IEnumerable<Document> nodes)
        {
            if (nodes == null) throw new ArgumentNullException("nodes");
            Add(path, Helper.GetNodeCache(nodes.Cast<Node>()));
        }

        private void Add(string path, NodeCache nodeCache)
        {
            if (nodeCache != null)
            {
                FDoc doc = new FDoc(nodeCache, path.Token());
                using (new Update())
                {
                    Update.Add(new DBAdd(Data, null, doc, path, Context));
                }
            }
        }

        public virtual void Replace(string path, XmlReader xmlReader)
        {
            if (xmlReader == null) throw new ArgumentNullException("xmlReader");
            Replace(path, Helper.GetNodeCache(xmlReader));
        }

        public virtual void Replace(string path, string content)
        {
            Helper.CallWithString(content, path, Replace);
        }

        public void Replace(string path, params Document[] nodes)
        {
            if (nodes == null) throw new ArgumentNullException("nodes");
            Replace(path, Helper.GetNodeCache(nodes));
        }

        public void Replace(string path, IEnumerable<Document> nodes)
        {
            if (nodes == null) throw new ArgumentNullException("nodes");
            Replace(path, Helper.GetNodeCache(nodes.Cast<Node>()));
        }

        private void Replace(string path, NodeCache nodeCache)
        {
            using (new Update())
            {
                int pre = Data.doc(path);
                if (pre != -1)
                {
                    if (Data.docs(path).size() != 1) throw new ArgumentException("Simple document expected as replacement target");
                    Update.Add(new DeleteNode(pre, Data, null));
                    Add(path, nodeCache);
                }
            }
        }
        
        public Document GetDocument(string name)
        {
            int pre = Data.doc(name);
            return pre == -1 ? null : (Document)Node.Get(pre, Data);
        }

        public IEnumerable<Document> GetDocuments(string path)
        {
            IntList docs = Data.docs(path);
            for (int i = 0, s = docs.size(); i < s; i++)
            {
                int pre = docs.get(i);
                yield return (Document)Node.Get(pre, Data);
            }
        }

        public IEnumerable<Document> Documents
        {
            get
            {
                IntList il = Data.docs();
                for(int c = 0 ; c < il.size() ; c++ )
                {
                    int pre = il.get(c);
                    yield return (Document)Node.Get(pre, Data);
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
