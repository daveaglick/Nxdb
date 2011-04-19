using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using org.basex.core;
using org.basex.core.cmd;
using org.basex.data;
using org.basex.io;
using org.basex.util;
using org.basex.build;
using Array=org.basex.util.Array;

namespace Nxdb
{
    public class NxCollection : IDictionary<string, NxNode>, IDisposable
    {
        private readonly NxDatabase database;
        public NxDatabase Database
        {
            get { return database; }
        }

        private readonly string name;
        public string Name
        {
            get { return name; }
        }

        private Data data = null;
        internal Data Data
        { get { return data; } }

        private bool disposed = false;
        internal bool Disposed
        {
            get { return disposed; }
        }

        internal NxCollection(NxDatabase database, string name)
            : this(database, name,
            CreateDB.empty(name, database.Context))
        {}

        internal NxCollection(NxDatabase database, string name, Data data)
        {
            this.database = database;
            this.name = name;
            this.data = data;
            database.Context.openDB(data);
        }

        public int LastId
        {
            get
            {
                CheckDisposed();
                return data.meta.lastid;
            }
        }

        //The time of the last update
        public long Time
        {
            get
            {
                CheckDisposed();
                return data.meta.time;
            }
        }

        public NxNode Add(string documentName, TextReader textReader)
        {
            if (textReader == null)
            {
                throw new ArgumentNullException("textReader");
            }
            using(XmlReader xmlReader = XmlReader.Create(textReader, ReaderSettings) )
            {
                return Add(documentName, xmlReader);
            }
        }

        public NxNode Add(string documentName, XmlReader xmlReader)
        {
            CheckDisposed();

            if (ContainsKey(documentName) && !(data.size(0, Data.DOC) == 1))
            {
                throw new ArgumentException("Document " + documentName + " already exists");
            }

            //Insert the new data
            int pre = data.meta.size;
            DataInserter.Insert(xmlReader, data, pre, -1, documentName);
            data.flush();
            database.Context.update();

            //Reoptimize if needed
            if(database.OptimizeCollectionOnAdd)
            {
                Optimize();
            }

            return new NxNode(this, pre == 1 ? 0 : pre);    //If pre == 1 then there was a temporary document in place that has now been deleted
        }

        private bool Remove(string key, bool optimize)
        {
            CheckDisposed();

            if(!ContainsKey(key))
            {
                return false;
            }

            Delete delete = new Delete(key);
            bool ret = delete.run(database.Context);
            if( optimize && database.OptimizeCollectionOnRemove )
            {
                ret = Optimize() && ret;
            }
            return ret;
        }

        //Optimizes the collection (compacts and rebuilds indexes)
        public bool Optimize()
        {
            CheckDisposed();
            Optimize optimize = new Optimize();
            return optimize.run(database.Context);
        }

        //Closes (and possibly drops/deletes) the database
        public void Dispose()
        {
            //Checking disposed ensures we don't get into an infinite loop when disposing from NxDatabase.Remove()
            if (!disposed)
            {
                if (data != null)
                {
                    Close.close(database.Context, data);
                    data = null;
                }
                disposed = true;
                database.Remove(name);
                if( database.DropOnDispose )
                {
                    database.Drop(name);
                }
            }
        }

        private void CheckDisposed()
        {
            if (disposed)
            {
                throw new ObjectDisposedException("NxCollection");
            }
        }

        ~NxCollection()
        {
            if( data != null )
            {
                Dispose();
            }
        }

        //Static XmlReader and XmlWriter settings
        //Most permisive possible, don't want the writer/reader doing any post-processing
        private static XmlWriterSettings writerSettings;
        public static XmlWriterSettings WriterSettings
        {
            get
            {
                if (writerSettings == null)
                {
                    writerSettings = new XmlWriterSettings();
                    writerSettings.Indent = false;
                    writerSettings.OmitXmlDeclaration = true;
                    writerSettings.CheckCharacters = false;
                    writerSettings.NewLineHandling = NewLineHandling.None;
                    writerSettings.NewLineOnAttributes = false;
                    writerSettings.ConformanceLevel = ConformanceLevel.Auto;
                }
                return writerSettings;
            }
        }

        private static XmlReaderSettings readerSettings;
        public static XmlReaderSettings ReaderSettings
        {
            get
            {
                if (readerSettings == null)
                {
                    readerSettings = new XmlReaderSettings();
                    readerSettings.IgnoreComments = false;
                    readerSettings.IgnoreProcessingInstructions = false;
                    readerSettings.IgnoreWhitespace = false;
                    readerSettings.CheckCharacters = false;
                    readerSettings.ConformanceLevel = ConformanceLevel.Fragment;
                }
                return readerSettings;
            }
        }

        public NxQuery GetQuery(string expression)
        {
            CheckDisposed();
            NxQuery query = new NxQuery(database, expression);
            query.SetContext(Values);
            return query;
        }

        public IEnumerable<object> Query(string expression)
        {
            return GetQuery(expression).Evaluate();
        }

        public NxNode GetNodeAtIndex(int index)
        {
            CheckDisposed();
            if(index < data.meta.size)
            {
                return new NxNode(this, index);
            }
            return null;
        }

        public NxNode GetNodeWithId(int id)
        {
            CheckDisposed();
            int index = data.pre(id);
            if (index >= 0)
            {
                return new NxNode(this, index, id);
            }
            return null;
        }

        //IDictionary

        public NxNode this[string key]
        {
            get
            {
                CheckDisposed();
                NxNode value;
                return TryGetValue(key, out value) ? value : null;
            }
            set { throw new NotSupportedException("Use Add(string, ...) to add a new document"); }
        }

        public bool Remove(string key)
        {
            CheckDisposed();
            return Remove(key, true);
        }

        public IEnumerator<KeyValuePair<string, NxNode>> GetEnumerator()
        {
            CheckDisposed();
            if (data != null)
            {
                foreach (int pre in data.doc())
                {
                    yield return new KeyValuePair<string, NxNode>(
                        Token.@string(data.text(pre, true)), new NxNode(this, pre));
                }
            }
            yield break;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(KeyValuePair<string, NxNode> item)
        {
            throw new NotSupportedException("Use Add(string, ...) to add a new document");
        }

        public void Add(string key, NxNode value)
        {
            throw new NotSupportedException("Use Add(string, ...) to add a new document");
        }

        public void Clear()
        {
            CheckDisposed();
            foreach(string key in Keys)
            {
                Remove(key, false);
            }
            if( database.OptimizeCollectionOnRemove )
            {
                Optimize();
            }
        }

        public bool Contains(KeyValuePair<string, NxNode> item)
        {
            CheckDisposed();
            NxNode value;
            return TryGetValue(item.Key, out value) && value.Equals(item.Value);
        }

        public void CopyTo(KeyValuePair<string, NxNode>[] array, int arrayIndex)
        {
            CheckDisposed();
            List<KeyValuePair<string, NxNode>> source = new List<KeyValuePair<string, NxNode>>(this);
            source.CopyTo(array, arrayIndex);
        }

        public bool Remove(KeyValuePair<string, NxNode> item)
        {
            CheckDisposed();
            if( Contains(item) )
            {
                Remove(item.Key);
            }
            return false;
        }

        public int Count
        {
            get
            {
                CheckDisposed();
                if( data == null )
                {
                    return 0;
                }
                return data.doc().Length;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                CheckDisposed();
                return false;
            }
        }

        public bool ContainsKey(string key)
        {
            CheckDisposed();
            return Keys.Contains(key);
        }

        public bool TryGetValue(string key, out NxNode value)
        {
            CheckDisposed();
            if( data != null )
            {
                foreach (int pre in data.doc())
                {
                    if( Token.@string(data.text(pre, true)) == key )
                    {
                        value = new NxNode(this, pre);
                        return true;
                    }
                }
            }
            value = null;
            return false;
        }

        public ICollection<string> Keys
        {
            get
            {
                CheckDisposed();
                List<string> keys = new List<string>();
                if (data != null)
                {
                    foreach (int pre in data.doc())
                    {
                        keys.Add(Token.@string(data.text(pre, true)));
                    }
                }
                return keys;
            }
        }

        public ICollection<NxNode> Values
        {
            get
            {
                CheckDisposed();
                List<NxNode> values = new List<NxNode>();
                if (data != null)
                {
                    foreach (int pre in data.doc())
                    {
                        values.Add(new NxNode(this, pre));
                    }
                }
                return values;
            }
        }

        //A cache of all constructed DOM nodes for this collection
        //Needed because .NET XML DOM consumers probably expect one object per node
        //instead of the on the fly creation that Nxdb uses
        //Key = node Id, Value = WeakReference to XmlNode instance
        private readonly Dictionary<int, WeakReference> domCache = new Dictionary<int, WeakReference>();
        internal Dictionary<int, WeakReference> DomCache
        {
            get { return domCache; }
        }

        //Equals and GetHashCode rely on there being unique names per NxCollection as enforced by NxDatabase
        public override bool Equals(object obj)
        {
            NxCollection other = obj as NxCollection;
            if( other != null && name.Equals(other.name) && database.Equals(other.database))
            {
                return true;
            }
            return false;
        }

        public override int GetHashCode()
        {
            int result = 17;
            result = 37 * result + name.GetHashCode();
            result = 37 * result + database.GetHashCode();
            return result;
        }
    }
}
