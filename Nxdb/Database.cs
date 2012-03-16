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

#region Doxygen Comments

/** @mainpage Nxdb API Documentation
 * Nxdb is a high-performance embedded XML database for .NET with full
 * XQuery support and a powerful object persistence framework. This is the
 * API documentation for Nxdb. For more information, please see the Nxdb
 * portal at https://dracorp.assembla.com/spaces/nxdb.
 */

//! @namespace Nxdb The primary Nxdb namespace that contains classes for database access, querying, and other general capabilities.

//! @namespace Nxdb.Dom Contains classes that replicate the .NET DOM interface for compatibility with other .NET XML components.

//! @namespace Nxdb.Io Contains internal classes relevant for data input and output.

//! @namespace Nxdb.Node Contains classes that represent the internal Nxdb data model and that provide direct database access.

//! @namespace Nxdb.Persistence Contains the Nxdb persistence framework.

//! @namespace Nxdb.Persistence.Attributes Contains all of the custom attributes used in the Nxdb persistence framework.

#endregion

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using Nxdb.Node;
using org.basex.core;
using org.basex.core.cmd;
using org.basex.data;
using org.basex.query;
using org.basex.query.item;
using org.basex.query.iter;
using org.basex.query.up.primitives;
using org.basex.build;
using org.basex.util.list;

namespace Nxdb
{
    /// <summary>
    /// Represents a single database into which all documents should be stored.
    /// The documents in the database can be grouped using paths in the document name,
    /// i.e.: "folderA/docA.xml", "folderA/docB.xml", "folderB/docC.xml"
    /// </summary>
    public class Database : IDisposable, IEquatable<Database>, IQuery
    {
        #region Static
        
        /// <summary>
        /// This runs some one-time initialization and needs to be called once (and only once) before use.
        /// </summary>
        /// <param name="path">The path in which to store the database(s).</param>
        //Useful because several Java classes use reflection on their first construction
        //which hurts performance when the first construction is done during an operation
        public static void Initialize(string path)
        {
            if (_context == null)
            {
                // Make sure we have a valid path
                if(String.IsNullOrEmpty(path))
                {
                    path = Path.Combine(Environment.CurrentDirectory, "Nxdb");
                }

                // Create the home path
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                // Set the home path so the BaseX preference file will go there (rather than user path)
                java.lang.System.setProperty("user.home", path);
                java.lang.System.setProperty("org.basex.path", path);

                // Now we can create the context since the path for preferences has been set
                Context context = new Context();
                _properties = new NxdbProp();

                // Now set the database path
                context.mprop.set(MainProp.DBPATH, path);

                // Do some Java initialization by instantiating objects
                new QueryContext(context);
                new FElem(new QNm("init".Token()));

                // Set the context at the very end
                _context = context;
            }
        }

        private static void Initialize()
        {
            Initialize(null);
        }

        //Use one static Context object - all opened databases will get added ("pinned") to the context
        //The primary Context.data() reference should remain null
        private static Context _context = null;

        internal static Context Context
        {
            get
            {
                if(_context == null)
                {
                    Initialize();
                }
                return _context;
            }
        }

        private static NxdbProp _properties = null;

        internal static NxdbProp Properties
        {
            get
            {
                Initialize();    // Need to make sure we're initialized before getting the properties object
                return _properties;
            }
        }

        // A cache of all databases - the same instance should be returned for the same Data
        private static readonly Dictionary<Data, Database> DatabasesCache = new Dictionary<Data, Database>();

        /// <summary>
        /// Drops the specified database.
        /// </summary>
        /// <param name="name">The name of the database to drop.</param>
        /// <returns>True if the database was found and successfully dropped, false otherwise.</returns>
        public static bool Drop(string name)
        {
            if (name == null) throw new ArgumentNullException("name");
            if (name == String.Empty) throw new ArgumentException("name");

            //Only drop if not currently pinned
            if (Context.pinned(name))
            {
                throw new ArgumentException("Database is currently open, please dispose all references and try again.");
            }

            //Attempt to drop
            return DropDB.drop(name, Context);
        }
        
        /// <summary>
        /// Gets an existing database or creates a new one.
        /// </summary>
        /// <param name="name">The name of the database to get or create.</param>
        /// <param name="database">The database.</param>
        /// <returns>true if the database was opened, false if it was created.</returns>
        public static bool Get(string name, out Database database)
        {
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
                    database = Get(CreateDB.create(name, Parser.emptyParser(Context.prop), Context));
                    return false;
                }
                catch (Exception ex)
                {
                    throw new ArgumentException("Could not create database.", ex);
                }
            }
        }

        internal static Database Get(Data data)
        {
            if (data == null) throw new ArgumentNullException("data");
            Database database;
            if (!DatabasesCache.TryGetValue(data, out database))
            {
                database = new Database(data);
                DatabasesCache.Add(data, database);
            }
            return database;
        }

        /// <summary>
        /// Gets an existing database or creates a new one.
        /// </summary>
        /// <param name="name">The name of the database to get or create.</param>
        /// <returns>The opened or newly created database.</returns>
        public static Database Get(string name)
        {
            Database database;
            Get(name, out database);
            return database;
        }

        /// <summary>
        /// Gets a new memory database. This database has no name and is not added to
        /// the database cache. It does not need to be dropped.
        /// </summary>
        /// <returns>A new in-memory database.</returns>
        public static Database Get()
        {
            MemData data = CreateDB.mainMem(Parser.emptyParser(Context.prop), Context);
            return new Database(data);
        }

        /// <summary>
        /// Gets all open databases.
        /// </summary>
        public static IEnumerable<Database> Databases
        {
            get
            {
                List<Database> databases = new List<Database>(DatabasesCache.Values);
                return databases;
            }
        }

        #endregion
        
        // A cache of all nodes for this database indexed by pre value
        private WeakReference[] _nodes = null;

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

        /// <summary>
        /// Closes the database and optionally drops it (if Nxdb.Properties.DropOnDispose == true).
        /// </summary>
        public void Dispose()
        {
            string dropName = null;
            if (_data == null) return;
            dropName = Data.meta.name;
            if(Context.unpin(_data))
            {
                DatabasesCache.Remove(_data);
                _data.close();
                _data = null;
                _nodes = null;
            }
            if (dropName != null && Nxdb.Properties.DropOnDispose)
            {
                Drop(dropName);
            }
        }

        // Only called from the Node.Get() static methods - use those to get new nodes
        internal Node.Node GetNode(int pre)
        {
            if (_nodes == null) throw new ObjectDisposedException("Database");
            if (_nodes[pre] != null)
            {
                Node.Node node = (Node.Node) _nodes[pre].Target;
                if (node != null)
                {
                    return node;
                }
            }
            return null;
        }

        internal void SetNode(int pre, Node.Node node)
        {
            if (_nodes == null) throw new ObjectDisposedException("Database");
            _nodes[pre] = new WeakReference(node);
        }

        // Called by Updates.Apply()
        internal void Update()
        {
            if (_nodes == null) throw new ObjectDisposedException("Database");

            // Grow the nodes cache if needed (but never shrink it)
            if (_data.meta.size > _nodes.Length)
            {
                Array.Resize(ref _nodes, _data.meta.size);
            }

            // Check validity and reposition nodes
            LinkedList<Node.Node> reposition = new LinkedList<Node.Node>();
            for (int c = 0; c < _nodes.Length; c++)
            {
                if (_nodes[c] != null)
                {
                    Node.Node node = (Node.Node)_nodes[c].Target;
                    if (node != null)
                    {
                        if (!node.Validate())
                        {
                            // The node is now invalid, remove it from the cache
                            _nodes[c] = null;
                        }
                        else
                        {
                            // The node is still valid, but if it moved add it to the reposition list
                            if (node.Index != c)
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
            foreach (Node.Node node in reposition)
            {
                _nodes[node.Index] = new WeakReference(node);
            }

            //Raise the Updated event
            EventHandler<EventArgs> handler = Updated;
            if (handler != null) handler(this, EventArgs.Empty);
        }

        /// <summary>
        /// Occurs when the database is updated.
        /// </summary>
        public event EventHandler<EventArgs> Updated;

        /// <summary>
        /// Gets the name of the database.
        /// </summary>
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

        /// <summary>
        /// Deletes the document at the specified path.
        /// </summary>
        /// <param name="path">The path of the document to delete.</param>
        public void Delete(string path)
        {   
            if (_data == null) throw new ObjectDisposedException("Database");
            IntList docs = Data.resources.docs(path);
            using (new Updates())
            {
                for (int i = docs.size() - 1; i >= 0; i--)
                {
                    Updates.Do(new DeleteNode(docs.get(i), Data, null));
                }
            }
        }

        /// <summary>
        /// Renames the document at the specified path.
        /// </summary>
        /// <param name="path">The path of the document to rename.</param>
        /// <param name="newName">The new name of the document.</param>
        public void Rename(string path, string newName)
        {
            if (_data == null) throw new ObjectDisposedException("Database");
            IntList docs = Data.resources.docs(path);
            using (new Updates())
            {
                for (int i = 0, s = docs.size(); i < s; i++)
                {
                    int pre = docs.get(i);
                    string target = org.basex.core.cmd.Rename.target(Data, pre, path, newName);
                    if (!String.IsNullOrEmpty(target))
                    {
                        Updates.Do(new ReplaceValue(pre, Data, null, target.Token()));
                    }
                }
            }
        }

        /// <summary>
        /// Optimizes database structures and updates indexes.
        /// </summary>
        public void Optimize()
        {
            if (_data == null) throw new ObjectDisposedException("Database");
            Updates.Do(new DBOptimize(Data, Context, false, null));
        }

        /// <summary>
        /// Optimizes database structures and updates indexes. The internal
        /// database structures are completely rebuilt which often leads to
        /// a reduction of the total database size.
        /// </summary>
        public void OptimizeAll()
        {
            if (_data == null) throw new ObjectDisposedException("Database");
            Updates.Do(new DBOptimize(Data, Context, true, null));
        }

        /// <summary>
        /// Flushes this database to disk. This is needed if Properties.AutoFlush
        /// is set to false.
        /// </summary>
        public void Flush()
        {
            if (_data == null) throw new ObjectDisposedException("Database");
            bool autoFlush = Nxdb.Properties.AutoFlush;
            Nxdb.Properties.AutoFlush = true;
            _data.flush();
            Nxdb.Properties.AutoFlush = autoFlush;
        }

        /// <summary>
        /// Adds a new document at the specified path.
        /// </summary>
        /// <param name="path">The path at which to add the document.</param>
        /// <param name="xmlReader">The XML reader containing the new content.</param>
        public virtual void Add(string path, XmlReader xmlReader)
        {
            if (xmlReader == null) throw new ArgumentNullException("xmlReader");
            Add(path, Helper.GetNodeCache(xmlReader));
        }

        /// <summary>
        /// Adds a new document at the specified path.
        /// </summary>
        /// <param name="path">The path at which to add the document.</param>
        /// <param name="content">The XML content to add.</param>
        public virtual void Add(string path, string content)
        {
            Helper.CallWithString(content, path, Add);
        }

        /// <summary>
        /// Adds new document(s) at the specified path.
        /// </summary>
        /// <param name="path">The path at which to add the document(s).</param>
        /// <param name="nodes">The document nodes to add.</param>
        public void Add(string path, params Document[] nodes)
        {
            if (nodes == null) throw new ArgumentNullException("nodes");
            Add(path, Helper.GetNodeCache(nodes));
        }

        /// <summary>
        /// Adds new document(s) at the specified path.
        /// </summary>
        /// <param name="path">The path at which to add the document(s).</param>
        /// <param name="nodes">The document nodes to add.</param>
        public void Add(string path, IEnumerable<Document> nodes)
        {
            if (nodes == null) throw new ArgumentNullException("nodes");
            Add(path, Helper.GetNodeCache(nodes.Cast<Node.Node>()));
        }

        private void Add(string path, NodeCache nodeCache)
        {
            if (_data == null) throw new ObjectDisposedException("Database");
            if (nodeCache != null)
            {
                FDoc doc = new FDoc(nodeCache, path.Token());
                Updates.Do(new DBAdd(Data, null, doc, path, Context));
            }
        }

        /// <summary>
        /// Replaces a document at the specified path.
        /// </summary>
        /// <param name="path">The path of the document to replace.</param>
        /// <param name="xmlReader">The XML reader containing the replacing content.</param>
        public virtual void Replace(string path, XmlReader xmlReader)
        {
            if (xmlReader == null) throw new ArgumentNullException("xmlReader");
            Replace(path, Helper.GetNodeCache(xmlReader));
        }

        /// <summary>
        /// Replaces a document at the specified path.
        /// </summary>
        /// <param name="path">The path of the document to replace.</param>
        /// <param name="content">The replacing XML content.</param>
        public virtual void Replace(string path, string content)
        {
            Helper.CallWithString(content, path, Replace);
        }

        /// <summary>
        /// Replaces document(s) at the specified path.
        /// </summary>
        /// <param name="path">The path of the document(s) to replace.</param>
        /// <param name="nodes">The replacing document nodes.</param>
        public void Replace(string path, params Document[] nodes)
        {
            if (nodes == null) throw new ArgumentNullException("nodes");
            Replace(path, Helper.GetNodeCache(nodes));
        }

        /// <summary>
        /// Replaces document(s) at the specified path.
        /// </summary>
        /// <param name="path">The path of the document(s) to replace.</param>
        /// <param name="nodes">The replacing document nodes.</param>
        public void Replace(string path, IEnumerable<Document> nodes)
        {
            if (nodes == null) throw new ArgumentNullException("nodes");
            Replace(path, Helper.GetNodeCache(nodes.Cast<Node.Node>()));
        }

        private void Replace(string path, NodeCache nodeCache)
        {
            if (_data == null) throw new ObjectDisposedException("Database");
            int pre = Data.resources.doc(path);
            if (pre != -1)
            {
                if (Data.resources.docs(path).size() != 1) throw new ArgumentException("Simple document expected as replacement target");
                using (new Updates())
                {
                    Updates.Do(new DeleteNode(pre, Data, null));
                    Add(path, nodeCache);
                }
            }
        }

        /// <summary>
        /// Gets the document with the specified name, or null if a document with the specified name is not found.
        /// </summary>
        /// <param name="name">The name of the document to get.</param>
        /// <returns></returns>
        public Document GetDocument(string name)
        {
            if (_data == null) throw new ObjectDisposedException("Database");
            int pre = Data.resources.doc(name);
            return pre == -1 ? null : (Document)Node.Node.Get(pre, Data);    
        }

        /// <summary>
        /// Gets all documents at the specified path.
        /// </summary>
        /// <param name="path">The path at which to get documents.</param>
        /// <returns></returns>
        public IEnumerable<Document> GetDocuments(string path)
        {
            List<Document> documents = new List<Document>();
            if (_data == null) throw new ObjectDisposedException("Database");
            IntList docs = Data.resources.docs(path);
            for (int i = 0, s = docs.size(); i < s; i++)
            {
                int pre = docs.get(i);
                documents.Add((Document) Node.Node.Get(pre, Data));
            }
            return documents;
        }

        /// <summary>
        /// Gets all documents in the database.
        /// </summary>
        public IEnumerable<Document> Documents
        {
            get
            {
                List<Document> documents = new List<Document>();
                if (_data == null) throw new ObjectDisposedException("Database");
                IntList il = Data.resources.docs();
                for (int c = 0; c < il.size(); c++)
                {
                    int pre = il.get(c);
                    documents.Add((Document)Node.Node.Get(pre, Data));
                }
                return documents;
            }
        }

        /// <summary>
        /// Gets all document names in the database.
        /// </summary>
        public IEnumerable<string> DocumentNames
        {
            get
            {
                List<string> names = new List<string>();
                if (_data == null) throw new ObjectDisposedException("Database");
                IntList il = Data.resources.docs();
                for (int c = 0; c < il.size(); c++)
                {
                    int pre = il.get(c);
                    names.Add(Data.text(pre, true).Token());
                }
                return names;
            }
        }

        public IEnumerable<object> Eval(string expression)
        {
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
            return Eval<T>(expression).FirstOrDefault();
        }
    }
}
