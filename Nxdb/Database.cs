using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using org.basex.core;
using org.basex.core.cmd;
using org.basex.data;
using org.basex.query;
using org.basex.query.item;
using org.basex.query.iter;
using org.basex.query.up.primitives;
using org.basex.build;
using org.basex.util.list;
using Exception = System.Exception;
using String = System.String;

namespace Nxdb
{
    // Represents a single BaseX database into which all documents should be stored
    // Unlike the previous versions, it's recommended just one overall NxDatabase be used
    // and querying between them is not supported
    // The documents in the database can be grouped using paths in the document name,
    // i.e.: "folderA/docA.xml", "folderA/docB.xml", "folderB/docC.xml"
    public class Database : IDisposable, IEquatable<Database>, IQuery
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
                    _properties = new NxdbProp();

                    // Now set the database path
                    _context.mprop.set(MainProp.DBPATH, _home);
                }
                return _context;
            }
        }

        private static NxdbProp _properties = null;

        internal static NxdbProp Properties
        {
            get
            {
                Context context = Context;
                return _properties;
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

        // Return value = opened (true = opened; false = created)
        public static bool Get(string name, out Database database)
        {
            Initialize();

            if (name == null) throw new ArgumentNullException("name");
            if (name == String.Empty) throw new ArgumentException("name");

            //Try to open or create the database
            database = null;
            try
            {
                database = Get(Open.open(name, Context));
                return true;
            }
            catch (Exception)
            {
                try
                {
                    database = Get(CreateDB.create(name, Parser.emptyParser(), Context));
                    return false;
                }
                catch (Exception ex)
                {
                    throw new ArgumentException("Could not create database.", ex);
                }
            }
        }

        public static Database Get(string name)
        {
            Database database;
            Get(name, out database);
            return database;
        }

        internal static Database Get(Data data)
        {
            if (data == null) throw new ArgumentNullException("data");
            Database database;
            if(!Databases.TryGetValue(data, out database))
            {
                database = new Database(data);
                Databases.Add(data, database);
            }
            return database;
        }

        // A cache of all databases - the same instance should be returned for the same Data
        private static readonly Dictionary<Data, Database> Databases
            = new Dictionary<Data, Database>(); 

        // A cache of all nodes for this database indexed by pre value
        private WeakReference[] _nodes;

        private Data _data;
        
        internal Data Data
        {
            get { return _data; }
        }

        private Database(Data data)
        {
            _data = data;
            _nodes = new WeakReference[data.meta.size];
        }

        public void Dispose()
        {
            if(_data == null) throw new ObjectDisposedException("Database");
            if(Context.unpin(_data))
            {
                string name = Name;
                Databases.Remove(_data);
                _data.close();  
                _data = null;   
                _nodes = null;
                if(Nxdb.Properties.DropOnDispose.Get())
                {
                    Drop(name);
                }
            }
        }

        // Only called from the Node.Get() static methods - use those to get new nodes
        internal Node GetNode(int pre)
        {
            if (_data == null) throw new ObjectDisposedException("Database");
            if (_nodes[pre] != null)
            {
                Node node = (Node)_nodes[pre].Target;
                if (node != null)
                {
                    return node;
                }
            }
            return null;
        }

        internal void SetNode(int pre, Node node)
        {
            if (_data == null) throw new ObjectDisposedException("Database");
            _nodes[pre] = new WeakReference(node);
        }

        internal void Update()
        {
            if (_data == null) throw new ObjectDisposedException("Database");

            //Raise the Updated event
            EventHandler<EventArgs> handler = Updated;
            if (handler != null) handler(this, EventArgs.Empty);

            // Grow the nodes cache if needed (but never shrink it)
            if(_data.meta.size > _nodes.Length)
            {
                Array.Resize(ref _nodes, _data.meta.size);
            }

            // Check validity and reposition nodes
            LinkedList<Node> reposition = new LinkedList<Node>();
            for(int c = 0; c < _nodes.Length ; c++)
            {
                if (_nodes[c] != null)
                {
                    Node node = (Node)_nodes[c].Target;
                    if (node != null)
                    {
                        if(!node.Validate())
                        {
                            // The node is now invalid, remove it from the cache
                            _nodes[c] = null;
                        }
                        else
                        {
                            // The node is still valid, but if it moved add it to the reposition list
                            if(node.Index != c)
                            {
                                reposition.AddLast(node);
                                _nodes[c] = null;
                            }
                        }
                    }
                    else
                    {
                        _nodes[c] = null;
                    }
                }
            }

            // Reposition any nodes that moved
            foreach(Node node in reposition)
            {
                _nodes[node.Index] = new WeakReference(node);
            }
        }

        public event EventHandler<EventArgs> Updated;

        public string Name
        {
            get
            {
                if (_data == null) throw new ObjectDisposedException("Database");
                return Data.meta.name;
            }
        }

        /// <summary>
        /// Gets a timestamp representing the last database modification. This can be valuable
        /// when you need to check if the database has been modified.
        /// </summary>
        public long Time
        {
            get
            {
                if (_data == null) throw new ObjectDisposedException("Database");
                return Data.meta.time;
            }
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
            return other != null && this == other;
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
            return Data == null ? 0 : Data.GetHashCode();
        }
        
        //Many of the following are ported from FNDb.java to eliminate the overhead of the XQuery function evaluation

        public void Delete(string path)
        {
            if (_data == null) throw new ObjectDisposedException("Database");
            IntList docs = Data.docs(path);
            using(new Updates())
            {
                for (int i = 0, s = docs.size(); i < s; i++)
                {
                    Updates.Add(new DeleteNode(docs.get(i), Data, null));
                }
            }
        }

        public void Rename(string path, string newName)
        {
            if (_data == null) throw new ObjectDisposedException("Database");
            IntList docs = Data.docs(path);
            using (new Updates())
            {
                for (int i = 0, s = docs.size(); i < s; i++)
                {
                    int pre = docs.get(i);
                    string target = org.basex.core.cmd.Rename.target(Data, pre, path, newName);
                    if (!String.IsNullOrEmpty(target))
                    {
                        Updates.Add(new ReplaceValue(pre, Data, null, target.Token()));
                    }
                }
            }
        }

        public void Optimize()
        {
            if (_data == null) throw new ObjectDisposedException("Database");
            Updates.Add(new DBOptimize(Data, Context, false, null));
        }

        public void OptimizeAll()
        {
            if (_data == null) throw new ObjectDisposedException("Database");
            Updates.Add(new DBOptimize(Data, Context, true, null));
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
            if (_data == null) throw new ObjectDisposedException("Database");
            if (nodeCache != null)
            {
                FDoc doc = new FDoc(nodeCache, path.Token());
                Updates.Add(new DBAdd(Data, null, doc, path, Context));
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
            if (_data == null) throw new ObjectDisposedException("Database");
            using (new Updates())
            {
                int pre = Data.doc(path);
                if (pre != -1)
                {
                    if (Data.docs(path).size() != 1) throw new ArgumentException("Simple document expected as replacement target");
                    Updates.Add(new DeleteNode(pre, Data, null));
                    Add(path, nodeCache);
                }
            }
        }
        
        public Document GetDocument(string name)
        {
            if (_data == null) throw new ObjectDisposedException("Database");
            int pre = Data.doc(name);
            return pre == -1 ? null : (Document)Node.Get(pre, Data);
        }

        public IEnumerable<Document> GetDocuments(string path)
        {
            if (_data == null) throw new ObjectDisposedException("Database");
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
                if (_data == null) throw new ObjectDisposedException("Database");
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
                if (_data == null) throw new ObjectDisposedException("Database");
                IntList il = Data.docs();
                for (int c = 0; c < il.size(); c++)
                {
                    int pre = il.get(c);
                    yield return Data.text(pre, true).Token();
                }
            }
        }

        public IEnumerable<object> Eval(string expression)
        {
            if (_data == null) throw new ObjectDisposedException("Database");
            return new Query(this).Eval(expression);
        }

        public IEnumerable<T> Eval<T>(string expression)
        {
            return Eval(expression).OfType<T>();
        }

        public IList<object> EvalList(string expression)
        {
            return new List<object>(Eval(expression));
        }

        public IList<T> EvalList<T>(string expression)
        {
            return new List<T>(Eval(expression).OfType<T>());
        }

        public object EvalSingle(string expression)
        {
            return Eval(expression).FirstOrDefault();
        }

        public T EvalSingle<T>(string expression) where T : class
        {
            return EvalSingle(expression) as T;
        }
    }
}
