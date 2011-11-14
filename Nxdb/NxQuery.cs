using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using org.basex.data;
using org.basex.query;
using org.basex.query.expr;
using org.basex.query.item;
using org.basex.query.iter;
using org.basex.query.path;
using org.basex.query.util;
using org.basex.util;
using Type=org.basex.query.item.Type;

namespace Nxdb
{
    /*
    public class NxQuery
    {
        private readonly NxManager manager;
        private readonly List<NxNode> nodes = new List<NxNode>();
        private readonly Dictionary<string, List<NxNode>> collections = new Dictionary<string, List<NxNode>>();
        private readonly string expression;

        public string Expression
        { get { return expression; } }

        internal NxQuery(NxManager manager, string expression)
        {
            if (manager == null)
            {
                throw new ArgumentNullException("manager");
            }
            if (expression == null)
            {
                throw new ArgumentNullException("expression");
            }
            this.manager = manager;
            this.expression = expression;
        }

        public void SetContext()
        {
            nodes.Clear();
        }

        public void SetContext(NxDatabase database)
        {
            if (database == null)
            {
                throw new ArgumentNullException("database");
            }
            SetCollection(null, database);
        }

        public void SetContext(NxNode node)
        {
            if (node == null)
            {
                throw new ArgumentNullException("node");
            }
            SetCollection(null, node);
        }

        public void SetContext(IEnumerable<NxNode> contextNodes)
        {
            if (contextNodes == null)
            {
                throw new ArgumentNullException("contextNodes");
            }
            SetCollection(null, contextNodes);
        }

        //name == null -> set the context
        //name == String.Empty -> set the default (first) database
        //name == text -> set a named database
        public void SetCollection(string name, NxDatabase database)
        {
            if( database == null )
            {
                throw new ArgumentNullException("database");
            }
            SetCollection(name, database.Values);
        }

        public void SetCollection(string name, NxNode node)
        {
            if (node == null)
            {
                throw new ArgumentNullException("node");
            }
            if( !node.Database.Manager.Equals(manager) )
            {
                throw new ArgumentException("Specified node is not part of database for this query");
            }
            if (name == null)
            {
                nodes.Clear();
                nodes.Add(node);
            }
            else
            {
                collections[name] = new List<NxNode>(){node};
            }
        }

        public void SetCollection(string name, IEnumerable<NxNode> collectionNodes)
        {
            if (collectionNodes == null)
            {
                throw new ArgumentNullException("collectionNodes");
            }
            foreach(NxNode node in collectionNodes)
            {
                if (!node.Database.Manager.Equals(manager))
                {
                    throw new ArgumentException("Specified node is not part of database for this query");
                }
            }
            if (name == null)
            {
                nodes.Clear();
                nodes.AddRange(collectionNodes);
            }
            else
            {
                collections[name] = new List<NxNode>(collectionNodes);
            }
        }

        public bool RemoveCollection(string name)
        {
            return collections.Remove(name);
        }

        public void ClearCollections()
        {
            collections.Clear();
        }

        //Gets both the context and all database nodes
        public IEnumerable<NxNode> GetAllNodes()
        {
            HashSet<NxNode> allNodes = new HashSet<NxNode>();
            foreach(NxNode node in nodes)
            {
                if(!allNodes.Contains(node))
                {
                    allNodes.Add(node);
                }
            }
            foreach(List<NxNode> collectionNodes in collections.Values)
            {
                foreach(NxNode node in collectionNodes)
                {
                    if(!allNodes.Contains(node))
                    {
                        allNodes.Add(node);
                    }
                }
            }
            return allNodes.AsEnumerable();
        }

        private Iter GetResultIter()
        {
            //Create an initialize the QueryContext
            QueryContext queryContext = new QueryContext(manager.Context);
            queryContext.nodes = null;

            //Add the variables
            foreach (KeyValuePair<string, string> kvp in variables)
            {
                Var var = new Var(new QNm(Token.token(kvp.Key)));
                var._field_value = Str.get(Token.token(kvp.Value));
                queryContext.vars.add(var);
            }

            //Parse the expression
            queryContext.parse(expression);

            //Compile the query (very similar to context.compile(), but new routine needed to allow nodes from multiple data sources)
            Compile(queryContext);

            //Return the result iterator
            return queryContext.iter();
        }

        private static bool GenerateDocsList(IEnumerable<NxNode> nodeList, out List<DBNode> docslist)
        {
            bool onlyDocs = true;
            docslist = new List<DBNode>();
            foreach (NxNode node in nodeList)
            {
                NxNode docNode = node;
                while (docNode != null && docNode.Kind != Data.DOC)
                {
                    docNode = docNode.ParentNode;
                    onlyDocs = false;
                }
                if (docNode != null)
                {
                    //Add the document node, but only if it isn't already added
                    bool found = false;
                    foreach (DBNode sn in docslist)
                    {
                        if (sn.pre == docNode.Index && sn.data == docNode.Database.Data)
                        {
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                    {
                        docslist.Add(new DBNode(docNode.Database.Data, docNode.Index, docNode.Kind));
                    }
                }
            }
            return onlyDocs;
        }

        //Need to recreate most of QueryContext.compile() here because it doesn't set the context nodes correctly for multi-document collections
        private void Compile(QueryContext queryContext)
        {
            // cache the initial context nodes
            if (nodes.Count > 0)
            {
                // create document nodes
                List<DBNode> docslist;
                bool onlyDocs = GenerateDocsList(nodes, out docslist);
                queryContext._field_doc = docslist.ToArray();
                queryContext.docs = docslist.Count;
                queryContext.rootDocs = docslist.Count;

                //Set context item
                if (onlyDocs)
                {
                    // optimization: all items are documents

                    //Note: Need to make a copy of the _field_doc array because of a bug in QueryContext.data()
                    //that causes a single Data object to be returned, even if multiple data objects are used
                    //when the item sequence and the _field_doc sequence are reference equivalent.
                    //Returning the single Data reference then causes optimizations that look at the Data to
                    //fail if more than one database is being used such as NameText.comp()

                    queryContext.value = Seq.get((DBNode[])queryContext._field_doc.Clone(), queryContext.docs);
                }
                else
                {
                    // otherwise, add all context items
                    ItemIter si = new ItemIter(nodes.Count);
                    foreach (NxNode node in nodes)
                    {
                        si.add(new DBNode(node.Database.Data, node.Index, node.Kind));
                    }
                    queryContext.value = si.finish();
                }
            }

            //add collections
            List<NxNode> collectionNodes;
            if( collections.TryGetValue(String.Empty, out collectionNodes) && collectionNodes.Count > 0 )
            {
                AddCollection(queryContext, String.Empty, collectionNodes);
            }
            foreach(KeyValuePair<string, List<NxNode>> kvp in collections)
            {
                if (kvp.Key != String.Empty && kvp.Value.Count > 0)
                {
                    AddCollection(queryContext, kvp.Key, kvp.Value);
                }
            }

            // evaluates the query and returns the result
            bool empty = queryContext.value == null;
            if (empty) queryContext.value = Item.DUMMY;
            queryContext.funcs.comp(queryContext);
            queryContext.root = queryContext.root.comp(queryContext);
            if (empty) queryContext.value = null;
        }

        private static void AddCollection(QueryContext queryContext, string name, IEnumerable<NxNode> collectionNodes)
        {
            List<DBNode> collectionDocslist;
            GenerateDocsList(collectionNodes, out collectionDocslist);
            NodIter cni = new NodIter();
            foreach (DBNode d in collectionDocslist)
            {
                cni.add(d);
            }
            queryContext.addColl(cni, Token.token(name));
        }

        private readonly Dictionary<string, string> variables = new Dictionary<string, string>();

        public void SetVariable(string name, string value)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }
            variables[name] = value;
        }

        public IEnumerable<object> Evaluate()
        {
            Iter iter = GetResultIter();
            Item item;
            while ((item = iter.next()) != null)
            {
                yield return GetObjectForItem(item);
            }
            yield break;
        }
        
        private object GetObjectForItem(Item item)
        {
            //Check for a null item
            if (item == null)
            {
                return null;
            }

            //Check for a node
            DBNode node = item as DBNode;
            if (node != null)
            {
                Data data = node.data;
                foreach (NxDatabase collection in manager.Values)
                {
                    if (collection.Data == data)
                    {
                        return new NxNode(collection, node.pre);
                    }
                }
                throw new InvalidOperationException();  //A result node wasn't a part of any loaded collections
            }

            //Must be atomic...
            //TODO: Does this work with sequences of sequences? probably not...
            Type.__Enum type = (Type.__Enum)Enum.ToObject(typeof(Type.__Enum), item.type().type.ordinal());
            switch (type)
            {
                case (Type.__Enum.BLN):
                    return item.@bool(null);
                case (Type.__Enum.BYT):
                    return (byte)item.itr(null);
                case (Type.__Enum.INT):
                case (Type.__Enum.USH):
                    return (int)item.itr(null);
                case (Type.__Enum.SHR):
                case (Type.__Enum.UBY):
                    return (short)item.itr(null);
                case (Type.__Enum.LNG):
                case (Type.__Enum.UIN):
                    return item.itr(null);
                case (Type.__Enum.DBL):
                    return item.dbl(null);
                case (Type.__Enum.DEC):
                    return Convert.ToDecimal(item.dec(null).toString());
                case (Type.__Enum.FLT):
                    return item.flt(null);
                case (Type.__Enum.HEX):
                    return item.toJava();
                default:
                    try
                    {
                        return Token.@string(item.atom());
                    }
                    catch (Exception)
                    {
                        return ((java.lang.Object)item.toJava()).toString();
                    }
            }
        }
    }
     */
}