using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using Nxdb.Dom;
using org.basex.build;
using org.basex.data;
using org.basex.query.item;
using org.basex.util;

namespace Nxdb
{
    public class NxNode : IEquatable<NxNode>
    {
        private readonly NxCollection collection;
        public NxCollection Collection
        {
            get { return collection; }
        }

        private readonly int id;
        public int Id
        {
            get { return id; }
        }

        private int pre;    //This should be updated before every use by calling Valid.get or CheckValid()
        public int Index
        {
            get
            {
                CheckValid();
                return pre;
            }
        }

        //Cache the node kind since it should never change and is used frequently
        private readonly int kind;
        internal int Kind
        {
            get
            {
                return kind;
            }
        }

        //Cache the last modified time to avoid checking pre against id on every operation
        private long time;

        internal NxNode(NxCollection collection, int pre)
            : this(collection, pre, collection.Data.id(pre))
        {}

        internal NxNode(NxCollection collection, int pre, int id)
        {
            this.collection = collection;
            this.pre = pre;
            this.id = id;
            kind = GetKind(collection.Data, pre);
            time = collection.Data.meta.time;
        }

        internal static int GetKind(Data data, int pre)
        {
            return data.kind(pre);
        }

        #region Dom

        public XmlNodeType NodeType
        {
            get
            {
                CheckValid();
                switch (kind)
                {
                    case (Data.DOC):
                        return XmlNodeType.Document;
                    case (Data.ELEM):
                        return XmlNodeType.Element;
                    case (Data.TEXT):
                        return XmlNodeType.Text;
                    case (Data.ATTR):
                        return XmlNodeType.Attribute;
                    case (Data.COMM):
                        return XmlNodeType.Comment;
                    case (Data.PI):
                        return XmlNodeType.ProcessingInstruction;
                    default:
                        throw new InvalidOperationException("Unexpected node type");
                }
            }
        }

        //Get the System.XmlNode representation and cache it for future reference
        public XmlNode XmlNode
        {
            get
            {
                CheckValid();

                //Has it already been cached?
                WeakReference weakNode;
                XmlNode xmlNode = null;
                if(collection.DomCache.TryGetValue(id, out weakNode))
                {
                    xmlNode = (XmlNode)weakNode.Target;
                }

                //If not found in the cache
                if (xmlNode == null)
                {
                    //Create the appropriate node type
                    switch (kind)
                    {
                        case (Data.DOC):
                            xmlNode = new NxDocument(this);
                            break;
                        case (Data.ELEM):
                            xmlNode = new NxElement(this);
                            break;
                        case (Data.TEXT):
                            xmlNode = new NxText(this);
                            break;
                        case (Data.ATTR):
                            xmlNode = new NxAttribute(this);
                            break;
                        case (Data.COMM):
                            xmlNode = new NxComment(this);
                            break;
                        case (Data.PI):
                            xmlNode = new NxProcessingInstruction(this);
                            break;
                        default:
                            throw new InvalidOperationException("Unexpected node type");
                    }

                    //Cache for later
                    collection.DomCache[id] = new WeakReference(xmlNode);
                }

                return xmlNode;
            }
        }

        //Checks if the input NxNode is null and if so, returns null - used from Dom implementations
        internal static XmlNode GetXmlNode(NxNode node)
        {
            if (node == null)
            {
                return null;
            }
            return node.XmlNode;
        }

        #endregion

        #region Validity

        //Verify the id and pre values still match, and if they don't update the pre
        //Should be checked before every operation (and if invalid, don't perform the operation)
        //Need to check because nodes may have been modified explicilty (through methods) or implicitly (XQuery Update)
        //Returns true if this NxNode is valid (it exists in the database), false otherwise
        public bool Valid
        {
            get
            {
                if( pre == -1 )
                {
                    return false;
                }
                if( collection.Disposed )
                {
                    pre = -1;
                    return false;
                }
                if (time != collection.Data.meta.time)
                {
                    time = collection.Data.meta.time;
                    if( pre >= collection.Data.meta.size || id != collection.Data.id(pre) )
                    {
                        pre = collection.Data.pre(id);
                        return pre != -1;
                    }
                }
                return true;
            }
        }

        //Internal convinience method for checking validity and then throwing an exception if no longer valid
        //Also checks if this node matches one of a set of kinds
        private bool CheckValid(params int[] kinds)
        {
            return CheckValid(false, kinds);
        }

        private bool CheckValid(bool throwKindException, params int[] kinds)
        {
            if( !Valid )
            {
                throw new InvalidOperationException("Node no longer valid");
            }
            if( kinds.Length > 0 )
            {
                foreach(int testKind in kinds)
                {
                    if( testKind == kind )
                    {
                        return true;
                    }
                }
                if (throwKindException)
                {
                    throw new InvalidOperationException("Invalid node type for this operation");
                }
                return false;
            }
            return true;
        }

        #endregion

        #region Queries
        
        public NxQuery GetQuery(string expression)
        {
            NxQuery query = new NxQuery(collection.Database, expression);
            query.SetContext(this);
            return query;
        }

        //If you are executing an update operation, you may need to enumerate the result for the
        //update to take effect (I.e., use .Count() after the query)
        public IEnumerable<object> Query(string expression)
        {
            return GetQuery(expression).Evaluate();
        }

        #endregion

        #region Children

        //Helper for the various child axis enumerables and acessors so we don't create unused NxNode objects
        internal static IEnumerable<int> GetChildPres(Data data, int parentPre, int parentKind, bool includeAttributes)
        {
            int childPre = parentPre + (includeAttributes ? 1 : data.attSize(parentPre, parentKind));
            int size = parentPre + data.size(parentPre, parentKind);
            while (childPre != size)
            {
                yield return childPre;
                childPre += data.size(childPre, data.kind(childPre));
            }
            yield break;
        }

        public IEnumerable<NxNode> ChildNodes
        {
            get
            {
                if (CheckValid(Data.ELEM, Data.DOC))
                {
                    foreach (int childPre in GetChildPres(collection.Data, pre, kind, false))
                    {
                        yield return new NxNode(collection, childPre);
                    }
                }
                yield break;
            }
        }

        public NxNode FirstChild
        {
            get
            {
                return ChildNodes.FirstOrDefault();
            }
        }

        public NxNode LastChild
        {
            get
            {
                if (CheckValid(Data.ELEM, Data.DOC))
                {
                    int lastChildPre = -1;
                    IEnumerator<int> childEnumerator = GetChildPres(collection.Data, pre, kind, false).GetEnumerator();
                    while (childEnumerator.MoveNext())
                    {
                        lastChildPre = childEnumerator.Current;
                    }
                    return lastChildPre == -1 ? null : new NxNode(collection, lastChildPre);
                }
                return null;
            }
        }

        public bool HasChildNodes
        {
            get
            {
                if (CheckValid(Data.ELEM, Data.DOC))
                {
                    return GetChildPres(collection.Data, pre, kind, false).GetEnumerator().MoveNext();
                }
                return false;
            }
        }

        #endregion

        #region Preceding/Following

        public IEnumerable<NxNode> FollowingSiblingNodes
        {
            get
            {
                if (CheckValid(Data.ELEM, Data.TEXT, Data.COMM, Data.PI))
                {
                    int parentPre = collection.Data.parent(pre, kind);
                    if (parentPre != -1)
                    {
                        IEnumerator<int> childEnumerator = GetChildPres(collection.Data, parentPre, collection.Data.kind(parentPre), false).GetEnumerator();
                        while (childEnumerator.MoveNext() && childEnumerator.Current != pre) { }
                        while (childEnumerator.MoveNext())
                        {
                            yield return new NxNode(collection, childEnumerator.Current);
                        }
                    }
                }
                yield break;
            }
        }

        public NxNode FollowingSibling
        {
            get
            {
                return FollowingSiblingNodes.FirstOrDefault();
            }
        }

        //These are returned in order closest to this node (this is different from the ordering of the coorisponding XPath axis)
        public IEnumerable<NxNode> PrecedingSiblingNodes
        {
            get
            {
                if (CheckValid(Data.ELEM, Data.TEXT, Data.COMM, Data.PI))
                {
                    int parentPre = collection.Data.parent(pre, kind);
                    if (parentPre != -1)
                    {
                        IEnumerator<int> childEnumerator = GetChildPres(collection.Data, parentPre, collection.Data.kind(parentPre), false).GetEnumerator();
                        List<int> precedingPres = new List<int>();
                        while (childEnumerator.MoveNext() && childEnumerator.Current != pre)
                        {
                            precedingPres.Add(childEnumerator.Current);
                        }
                        precedingPres.Reverse();
                        foreach (int precedingPre in precedingPres)
                        {
                            yield return new NxNode(collection, precedingPre);
                        }
                    }
                    yield break;
                }
            }
        }

        public NxNode PrecedingSibling
        {
            get
            {
                if (CheckValid(Data.ELEM, Data.TEXT, Data.COMM, Data.PI))
                {
                    int parentPre = collection.Data.parent(pre, kind);
                    if (parentPre != -1)
                    {
                        IEnumerator<int> childEnumerator = GetChildPres(collection.Data, parentPre, collection.Data.kind(parentPre), false).GetEnumerator();
                        int lastChild = -1;
                        while (childEnumerator.MoveNext() && childEnumerator.Current != pre)
                        {
                            lastChild = childEnumerator.Current;
                        }
                        if (lastChild != -1)
                        {
                            return new NxNode(collection, lastChild);
                        }
                    }
                }
                return null;
            }
        }

        //These are returned in order closest to this node (this is different from the ordering of the coorisponding XPath axis)
        public IEnumerable<NxNode> PrecedingNodes
        {
            get
            {
                if (CheckValid(Data.ELEM, Data.TEXT, Data.COMM, Data.PI))
                {
                    List<int> precedingPres = new List<int>();
                    List<int> tempPres = new List<int>();
                    int currentPre = pre;
                    int currentKind = kind;
                    int parentPre = collection.Data.parent(pre, kind);
                    while (parentPre != -1)
                    {
                        int parentKind = collection.Data.kind(parentPre);
                        if (currentKind != Data.ATTR)
                        {
                            tempPres.Clear();
                            IEnumerator<int> childEnumerator = GetChildPres(collection.Data, parentPre, parentKind, false).GetEnumerator();
                            while (childEnumerator.MoveNext() && childEnumerator.Current != currentPre)
                            {
                                tempPres.Add(childEnumerator.Current);
                                tempPres.AddRange(GetDescendantPres(collection.Data, childEnumerator.Current, collection.Data.kind(childEnumerator.Current)));
                            }
                            tempPres.Reverse();
                            precedingPres.AddRange(tempPres);
                        }
                        currentPre = parentPre;
                        currentKind = parentKind;
                        parentPre = collection.Data.parent(parentPre, parentKind);
                    }
                    foreach (int precedingPre in precedingPres)
                    {
                        yield return new NxNode(collection, precedingPre);
                    }
                }
                yield break;
            }
        }

        public IEnumerable<NxNode> FollowingNodes
        {
            get
            {
                if (CheckValid(Data.ELEM, Data.TEXT, Data.COMM, Data.PI))
                {
                    int currentPre = pre;
                    int currentKind = kind;
                    int parentPre = collection.Data.parent(pre, kind);
                    while (parentPre != -1)
                    {
                        int parentKind = collection.Data.kind(parentPre);
                        IEnumerator<int> childEnumerator = GetChildPres(collection.Data, parentPre, parentKind, false).GetEnumerator();
                        while (currentKind != Data.ATTR && childEnumerator.MoveNext() && childEnumerator.Current != currentPre) { }
                        while (childEnumerator.MoveNext())
                        {
                            yield return new NxNode(collection, childEnumerator.Current);
                            foreach (int descendantPre in
                                GetDescendantPres(collection.Data, childEnumerator.Current, collection.Data.kind(childEnumerator.Current)))
                            {
                                yield return new NxNode(collection, descendantPre);
                            }
                        }
                        currentPre = parentPre;
                        currentKind = parentKind;
                        parentPre = collection.Data.parent(parentPre, parentKind);
                    }
                }
                yield break;
            }
        }

        //Conveniences to match XmlNode
        public NxNode NextSibling
        {
            get { return FollowingSibling; }
        }

        public NxNode PreviousSibling
        {
            get { return PrecedingSibling; }
        }

        #endregion

        #region Parent/Ancestor/Descendant

        public NxNode OwnerDocument
        {
            get
            {
                CheckValid();

                //Is this the document?
                if (kind == Data.DOC)
                {
                    return this;
                }

                //Otherwise, crawl up until we find it
                int currentPre = pre;
                int currentKind = kind;
                int parent;
                while (currentKind != Data.DOC && (parent = collection.Data.parent(currentPre, currentKind)) != -1)
                {
                    currentPre = parent;
                    currentKind = collection.Data.kind(currentPre);
                }
                return currentKind == Data.DOC ? new NxNode(collection, currentPre) : null;
            }
        }

        public NxNode ParentNode
        {
            get
            {
                CheckValid();
                int parent = collection.Data.parent(pre, kind);
                if (parent >= 0)
                {
                    return new NxNode(collection, parent);
                }
                return null;
            }
        }

        //These are returned in order closest to this node (this is different from the ordering of the coorisponding XPath axis)
        public IEnumerable<NxNode> AncestorNodes
        {
            get
            {
                CheckValid();
                int parentPre = collection.Data.parent(pre, kind);
                while(parentPre >= 0)
                {
                    yield return new NxNode(collection, parentPre);
                    parentPre = collection.Data.parent(parentPre, collection.Data.kind(parentPre));
                }
                yield break;
            }
        }

        //These are returned in order closest to this node (this is different from the ordering of the coorisponding XPath axis)
        public IEnumerable<NxNode> AncestorOrSelfNodes
        {
            get
            {
                CheckValid();
                yield return this;
                foreach (NxNode ancestorNode in AncestorNodes)
                {
                    yield return ancestorNode;
                }
                yield break;
            }
        }

        public IEnumerable<NxNode> DescendantNodes
        {
            get
            {
                if (CheckValid(Data.ELEM, Data.DOC))
                {
                    foreach (int descendantPre in GetDescendantPres(collection.Data, pre, kind))
                    {
                        yield return new NxNode(collection, descendantPre);
                    }
                }
                yield break;
            }
        }

        public IEnumerable<NxNode> DescendantOrSelfNodes
        {
            get
            {
                if (CheckValid(Data.ELEM, Data.DOC))
                {
                    yield return this;
                    foreach (NxNode descendantNode in DescendantNodes)
                    {
                        yield return descendantNode;
                    }
                }
                yield break;
            }
        }

        internal static IEnumerable<int> GetDescendantPres(Data data, int parentPre, int parentKind)
        {
            foreach (int childPre in GetChildPres(data, parentPre, parentKind, false))
            {
                yield return childPre;
                foreach (int descendantPre in GetDescendantPres(data, childPre, data.kind(childPre)))
                {
                    yield return descendantPre;
                }
            }
            yield break;
        }

        //Gets the total number of descendant nodes, including attributes
        public int Size
        {
            get
            {
                CheckValid();
                return collection.Data.size(pre, kind);
            }
        }

        #endregion

        #region Attributes

        internal static IEnumerable<int> GetAttributePres(Data data, int parentPre, int parentKind)
        {
            int size = parentPre + data.attSize(parentPre, parentKind);
            int attrPre = parentPre + 1;
            while (attrPre != size)
            {
                yield return attrPre;
                attrPre++;
            }
            yield break;
        }

        private int GetAttributePre(string name)
        {
            byte[] token = Token.token(name);
            foreach (int attrPre in GetAttributePres(collection.Data, pre, kind))
            {
                if(Token.eq(token, collection.Data.name(attrPre, Data.ATTR)))
                {
                    return attrPre;
                }
            }
            return -1;
        }

        public IEnumerable<NxNode> Attributes
        {
            get
            {
                if (CheckValid(Data.ELEM))
                {
                    foreach (int attrPre in GetAttributePres(collection.Data, pre, kind))
                    {
                        yield return new NxNode(collection, attrPre);
                    }
                }
                yield break;
            }
        }

        //Returns String.Empty if the attribute doesn't exist and null if this is not an element node
        public string GetAttribute(string name)
        {
            if (CheckValid(Data.ELEM))
            {
                int attrPre = GetAttributePre(name);
                return attrPre == -1 ? string.Empty : Token.@string(collection.Data.text(attrPre, false));
            }
            return null;
        }

        public NxNode GetAttributeNode(string name)
        {
            if (CheckValid(Data.ELEM))
            {
                int attrPre = GetAttributePre(name);
                return attrPre == -1 ? null : new NxNode(collection, attrPre);
            }
            return null;
        }

        public void RemoveAllAttributes()
        {
            CheckValid(true, Data.ELEM);
            int count = collection.Data.attSize(pre, kind);
            while (--count > 0)
            {
                collection.Data.delete(pre + 1);
            }
            FinishUpdate();
        }

        public void RemoveAttributeAt(int i)
        {
            CheckValid(true, Data.ELEM);
            if( i < collection.Data.attSize(pre, kind))
            {
                collection.Data.delete(pre + 1 + i);
            }
            FinishUpdate();
        }

        //Calls RemoveChild but checks for attribute node
        public void RemoveAttribute(NxNode refNode)
        {
            if (refNode == null)
            {
                throw new ArgumentNullException("refNode");
            }
            CheckValid(true, Data.ELEM, Data.DOC);
            refNode.CheckValid(true, Data.ATTR);
            RemoveAttribute(collection.Data, refNode.pre);
            FinishUpdate();
        }

        internal static void RemoveAttribute(Data data, int refPre)
        {
            RemoveChild(data, refPre);
        }

        private void CheckAttributeUpdateParams(string name, string value)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }
            if( name == String.Empty )
            {
                throw new ArgumentException("name must not be empty");
            }
            if (value == String.Empty)
            {
                throw new ArgumentException("value must not be empty");
            }
        }

        public NxNode PrependAttribute(string name, string value)
        {
            CheckAttributeUpdateParams(name, value);
            CheckValid(true, Data.ELEM);
            DataInserter.Insert(XmlNodeType.Attribute, collection.Data, pre + 1, pre, name, value);
            FinishUpdate();
            return new NxNode(collection, pre + 1);
        }

        public NxNode AppendAttribute(string name, string value)
        {
            CheckAttributeUpdateParams(name, value);
            CheckValid(true, Data.ELEM);
            int ipre = AppendAttribute(collection.Data, pre, kind, name, value);
            FinishUpdate();
            return new NxNode(collection, ipre);
        }

        internal static int AppendAttribute(Data data, int pre, int kind, string name, string value)
        {
            int ipre = pre + data.attSize(pre, kind);
            DataInserter.Insert(XmlNodeType.Attribute, data, ipre, pre, name, value);
            return ipre;
        }

        public NxNode InsertAttributeBefore(string name, string value, NxNode refNode)
        {
            CheckAttributeUpdateParams(name, value);
            CheckValid(true, Data.ELEM);
            refNode.CheckValid(true, Data.ATTR);
            if (collection.Data.parent(refNode.pre, refNode.kind) != pre)
            {
                throw new ArgumentException("The specified node is not a direct descendant of this node");
            }
            int ipre = InsertAttributeBefore(collection.Data, pre, name, value, refNode.pre);
            FinishUpdate();
            return new NxNode(collection, ipre);
        }

        internal static int InsertAttributeBefore(Data data, int pre, string name, string value, int refPre)
        {
            DataInserter.Insert(XmlNodeType.Attribute, data, refPre, pre, name, value);
            return refPre;
        }

        public NxNode InsertAttributeAfter(string name, string value, NxNode refNode)
        {
            CheckAttributeUpdateParams(name, value);
            CheckValid(true, Data.ELEM);
            refNode.CheckValid(true, Data.ATTR);
            if (collection.Data.parent(refNode.pre, refNode.kind) != pre)
            {
                throw new ArgumentException("The specified node is not a direct descendant of this node");
            }
            DataInserter.Insert(XmlNodeType.Attribute, collection.Data, refNode.pre + 1, pre, name, value);
            FinishUpdate();
            return new NxNode(collection, refNode.pre + 1);
        }

        #endregion

        #region Reading/Writing

        //Returns the first child insert pre
        private int DeleteAllChildren(bool includeAttributes)
        {
            int count = 0;
            int firstChildPre = pre + collection.Data.attSize(pre, kind);   //Need to include attributes for initial insert pre to account for empty elements with attributes
            foreach (int childPre in GetChildPres(collection.Data, pre, kind, includeAttributes))
            {
                if (count == 0)
                {
                    firstChildPre = childPre;
                }
                count++;
            }
            while (count-- > 0)
            {
                collection.Data.delete(firstChildPre);
            }
            return firstChildPre;
        }

        //Removes all child nodes AND attributes
        public void RemoveAll()
        {
            CheckValid(true, Data.ELEM, Data.DOC);
            DeleteAllChildren(true);
            FinishUpdate();
        }

        //Removes a specific child (including attributes)
        public void RemoveChild(NxNode refNode)
        {
            if (refNode == null)
            {
                throw new ArgumentNullException("refNode");
            }
            CheckValid(true, Data.ELEM, Data.DOC);
            refNode.CheckValid();
            if (collection.Data.parent(refNode.pre, refNode.kind) != pre)
            {
                throw new ArgumentException("The specified node is not a direct descendant of this node");
            }
            RemoveChild(collection.Data, refNode.pre);
            FinishUpdate();
        }

        internal static void RemoveChild(Data data, int refPre)
        {
            data.delete(refPre);
        }

        public void ReplaceChild(XmlReader xmlReader, NxNode refNode)
        {
            if (refNode == null)
            {
                throw new ArgumentNullException("refNode");
            }
            CheckValid(true, Data.ELEM, Data.DOC);
            refNode.CheckValid(true, Data.ELEM, Data.TEXT, Data.COMM, Data.PI);
            if (collection.Data.parent(refNode.pre, refNode.kind) != pre)
            {
                throw new ArgumentException("The specified node is not a direct descendant of this node");
            }
            int ipre = refNode.pre + collection.Data.size(refNode.pre, refNode.kind);
            DataInserter.Insert(xmlReader, collection.Data, ipre, pre);
            collection.Data.delete(refNode.pre);
            FinishUpdate();
        }

        public void ReplaceChild(string xmlContent, NxNode refNode)
        {
            StringBasedOperation(xmlContent, refNode, ReplaceChild);
        }

        public void Match(XmlReader xmlReader)
        {
            CheckValid(true, Data.ELEM);
            DataMatcher.Match(xmlReader, collection.Data, pre);
            FinishUpdate();
        }

        public void Match(string xmlContent)
        {
            StringBasedOperation(xmlContent, Match);
        }

        //Flushes the data and optionaly rebuilds indexes
        private void FinishUpdate()
        {
            collection.Data.flush();
            if (collection.Database.OptimizeCollectionOnUpdate)
            {
                collection.Optimize();
            }
        }

        public string InnerXml
        {
            get
            {
                CheckValid();
                StringBuilder stringBuilder = new StringBuilder();
                using (XmlWriter xmlWriter = XmlWriter.Create(stringBuilder, NxCollection.WriterSettings))
                {
                    WriteContentTo(xmlWriter);
                }
                return stringBuilder.ToString();
            }
            set
            {
                StringBasedOperation(value, ReadContentFrom);
            }
        }

        //Same as InnerXml.set
        public void ReadContentFrom(XmlReader xmlReader)
        {
            if (xmlReader == null)
            {
                throw new ArgumentNullException("xmlReader");
            }
            CheckValid(true, Data.ELEM, Data.DOC);
            int ipre = DeleteAllChildren(false);
            DataInserter.Insert(xmlReader, collection.Data, ipre, pre);
            FinishUpdate();
        }

        public string OuterXml
        {
            get
            {
                CheckValid();
                StringBuilder stringBuilder = new StringBuilder();
                using (XmlWriter xmlWriter = XmlWriter.Create(stringBuilder, NxCollection.WriterSettings))
                {
                    WriteTo(xmlWriter);
                }
                return stringBuilder.ToString();
            }
        }

        public string InnerText
        {
            get
            {
                using (StringWriter stringWriter = new StringWriter())
                {
                    WriteTextTo(stringWriter);
                    stringWriter.Close();
                    return stringWriter.ToString();
                }
            }
            set
            {
                if(value == null)
                {
                    throw new ArgumentNullException("value");
                }
                if (CheckValid(Data.ELEM, Data.DOC))
                {
                    int ipre = DeleteAllChildren(false);
                    DataInserter.Insert(XmlNodeType.Text, collection.Data, ipre, pre, null, value);
                    FinishUpdate();
                }
            }
        }

        public TextReader InnerTextReader
        {
            get
            {
                CheckValid();
                return new NxTextReader(collection, pre, kind);
            }
        }

        //Same as InnerText.get
        public void WriteTextTo(TextWriter textWriter)
        {
            if (textWriter == null)
            {
                throw new ArgumentNullException("textWriter");
            }
            CheckValid();
            using (TextWriterSerializer serializer = new TextWriterSerializer(textWriter))
            {
                serializer.node(collection.Data, pre);
            }
        }

        //Same as OuterXml
        public void WriteTo(XmlWriter xmlWriter)
        {
            if (xmlWriter == null)
            {
                throw new ArgumentNullException("xmlWriter");
            }
            if (CheckValid(Data.ELEM, Data.DOC))
            {
                using (XmlWriterSerializer serializer = new XmlWriterSerializer(xmlWriter, kind == Data.DOC))
                {
                    serializer.node(collection.Data, pre);
                }
            }
        }

        //Same as InnerXml
        public void WriteContentTo(XmlWriter xmlWriter)
        {
            if (xmlWriter == null)
            {
                throw new ArgumentNullException("xmlWriter");
            }
            if (CheckValid(Data.ELEM, Data.DOC))
            {
                using (XmlWriterSerializer serializer = new XmlWriterSerializer(xmlWriter, false))
                {
                    foreach (int childPre in GetChildPres(collection.Data, pre, kind, false))
                    {
                        serializer.node(collection.Data, childPre);
                    }
                }
            }
        }

        public void InsertAfter(XmlReader xmlReader, NxNode refNode)
        {
            if (xmlReader == null)
            {
                throw new ArgumentNullException("xmlReader");
            }
            if (refNode == null)
            {
                throw new ArgumentNullException("refNode");
            }
            CheckValid(true, Data.ELEM, Data.DOC);
            refNode.CheckValid(true, Data.ELEM, Data.TEXT, Data.COMM, Data.PI);
            if (collection.Data.parent(refNode.pre, refNode.kind) != pre)
            {
                throw new ArgumentException("The specified node is not a direct descendant of this node");
            }
            int ipre = refNode.pre + collection.Data.size(refNode.pre, refNode.kind);
            DataInserter.Insert(xmlReader, collection.Data, ipre, pre);
            FinishUpdate();
        }

        public void InsertAfter(XmlNodeType nodeType, string name, string value, NxNode refNode)
        {
            if (refNode == null)
            {
                throw new ArgumentNullException("refNode");
            }
            CheckValid(true, Data.ELEM, Data.DOC);
            refNode.CheckValid(true, Data.ELEM, Data.TEXT, Data.COMM, Data.PI);
            if (collection.Data.parent(refNode.pre, refNode.kind) != pre)
            {
                throw new ArgumentException("The specified node is not a direct descendant of this node");
            }
            int ipre = refNode.pre + collection.Data.size(refNode.pre, refNode.kind);
            DataInserter.Insert(nodeType, collection.Data, ipre, pre, name, value);
            FinishUpdate();
        }

        public void InsertBefore(XmlReader xmlReader, NxNode refNode)
        {
            if (xmlReader == null)
            {
                throw new ArgumentNullException("xmlReader");
            }
            if (refNode == null)
            {
                throw new ArgumentNullException("refNode");
            }
            CheckValid(true, Data.ELEM, Data.DOC);
            refNode.CheckValid(true, Data.ELEM, Data.TEXT, Data.COMM, Data.PI);
            if (collection.Data.parent(refNode.pre, refNode.kind) != pre)
            {
                throw new ArgumentException("The specified node is not a direct descendant of this node");
            }
            InsertBefore(collection.Data, pre, xmlReader, refNode.pre);
            FinishUpdate();
        }

        internal static void InsertBefore(Data data, int pre, XmlReader xmlReader, int refPre)
        {
            DataInserter.Insert(xmlReader, data, refPre, pre);
        }

        public void InsertBefore(XmlNodeType nodeType, string name, string value, NxNode refNode)
        {
            if (refNode == null)
            {
                throw new ArgumentNullException("refNode");
            }
            CheckValid(true, Data.ELEM, Data.DOC);
            refNode.CheckValid(true, Data.ELEM, Data.TEXT, Data.COMM, Data.PI);
            if (collection.Data.parent(refNode.pre, refNode.kind) != pre)
            {
                throw new ArgumentException("The specified node is not a direct descendant of this node");
            }
            InsertBefore(collection.Data, pre, nodeType, name, value, refNode.pre);
            FinishUpdate();
        }

        internal static void InsertBefore(Data data, int pre, XmlNodeType nodeType, string name, string value, int refPre)
        {
            DataInserter.Insert(nodeType, data, refPre, pre, name, value);
        }

        public void AppendChild(XmlReader xmlReader)
        {
            if (xmlReader == null)
            {
                throw new ArgumentNullException("xmlReader");
            }
            CheckValid(true, Data.ELEM, Data.DOC);
            AppendChild(collection.Data, pre, kind, xmlReader);
            FinishUpdate();
        }

        internal static void AppendChild(Data data, int pre, int kind, XmlReader xmlReader)
        {
            int ipre = pre + data.size(pre, kind);
            DataInserter.Insert(xmlReader, data, ipre, pre);
        }

        public void AppendChild(XmlNodeType nodeType, string name, string value)
        {
            CheckValid(true, Data.ELEM, Data.DOC);
            AppendChild(collection.Data, pre, kind, nodeType, name, value);
            FinishUpdate();
        }

        internal static void AppendChild(Data data, int pre, int kind, XmlNodeType nodeType, string name, string value)
        {
            int ipre = pre + data.size(pre, kind);
            DataInserter.Insert(nodeType, data, ipre, pre, name, value);
        }

        public void PrependChild(XmlReader xmlReader)
        {
            if (xmlReader == null)
            {
                throw new ArgumentNullException("xmlReader");
            }
            CheckValid(true, Data.ELEM);
            int ipre = pre + collection.Data.attSize(pre, kind);
            DataInserter.Insert(xmlReader, collection.Data, ipre, pre);
            FinishUpdate();
        }

        public void PrependChild(XmlNodeType nodeType, string name, string value)
        {
            CheckValid(true, Data.ELEM);
            int ipre = pre + collection.Data.attSize(pre, kind);
            DataInserter.Insert(nodeType, collection.Data, ipre, pre, name, value);
            FinishUpdate();
        }

        private static void StringBasedOperation(string xmlContent, Action<XmlReader> action)
        {
            if (xmlContent == null)
            {
                throw new ArgumentNullException("xmlContent");
            }
            using (StringReader stringReader = new StringReader(xmlContent))
            {
                using (XmlReader xmlReader = XmlReader.Create(stringReader, NxCollection.ReaderSettings))
                {
                    action(xmlReader);
                }
            }
        }

        private static void StringBasedOperation(string xmlContent, NxNode refNode, Action<XmlReader, NxNode> action)
        {
            if (xmlContent == null)
            {
                throw new ArgumentNullException("xmlContent");
            }
            using (StringReader stringReader = new StringReader(xmlContent))
            {
                using (XmlReader xmlReader = XmlReader.Create(stringReader, NxCollection.ReaderSettings))
                {
                    action(xmlReader, refNode);
                }
            }
        }

        public void InsertAfter(string xmlContent, NxNode refNode)
        {
            StringBasedOperation(xmlContent, refNode, InsertAfter);
        }

        public void InsertBefore(string xmlContent, NxNode refNode)
        {
            StringBasedOperation(xmlContent, refNode, InsertBefore);
        }

        public void AppendChild(string xmlContent)
        {
            StringBasedOperation(xmlContent, AppendChild);
        }

        public void PrependChild(string xmlContent)
        {
            StringBasedOperation(xmlContent, PrependChild);
        }

        public string Value
        {
            get
            {
                if( CheckValid(Data.TEXT, Data.ATTR, Data.COMM, Data.PI) )
                {
                    return GetValue(collection.Data, pre, kind);
                }
                return null;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }
                CheckValid(true, Data.TEXT, Data.ATTR, Data.COMM, Data.PI);
                SetValue(collection.Data, pre, kind, value);
                FinishUpdate();
            }
        }

        internal static string GetValue(Data data, int pre, int kind)
        {
            return Token.@string(data.text(pre, kind != Data.ATTR));
        }

        internal static void SetValue(Data data, int pre, int kind, string value)
        {
            data.replace(pre, kind, Token.token(value));
        }

        public string Name
        {
            get
            {
                if( CheckValid(Data.ELEM, Data.ATTR, Data.PI) )
                {
                    return GetName(collection.Data, pre, kind);
                }
                return String.Empty;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }
                CheckValid(true, Data.ELEM, Data.ATTR, Data.PI);
                if (!XmlReader.IsName(value))
                {
                    throw new XmlException("Invalid XML name");
                }
                byte[] name = Token.token(value);
                byte[] uri = value.IndexOf(':') > 0
                    ? collection.Data._field_ns.uri(collection.Data._field_ns.uri(name, pre))
                    : new byte[]{};
                collection.Data.rename(pre, kind, Token.token(value), uri);
                FinishUpdate();
            }
        }

        internal static string GetName(Data data, int pre, int kind)
        {
            return Token.@string(data.name(pre, kind));
        }

        public string LocalName
        {
            get
            {
                if (CheckValid(Data.ELEM, Data.ATTR, Data.PI))
                {
                    byte[] name = collection.Data.name(pre, kind);
                    byte[] localName = Token.ln(name);
                    if (localName.Length > 0)
                    {
                        return Token.@string(localName);
                    }
                    return Token.@string(name);
                }
                return String.Empty;
            }
        }

        public string Prefix
        {
            get
            {
                if (CheckValid(Data.ELEM, Data.ATTR, Data.PI))
                {
                    return Token.@string(Token.pref(collection.Data.name(pre, kind)));
                }
                return String.Empty;
            }
        }

        public string NamespaceURI
        {
            get
            {
                if (CheckValid(Data.ELEM, Data.ATTR, Data.PI))
                {
                    byte[] name = collection.Data.name(pre, kind);
                    byte[] pref = Token.pref(name);
                    if(pref.Length > 0)
                    {
                        return Token.@string(collection.Data._field_ns.uri(collection.Data._field_ns.uri(name, pre)));
                    }
                }
                return String.Empty;
            }
        }

        #endregion

        #region Equality/Hashing

        public bool Equals(NxNode other)
        {
            if( other == null )
            {
                return false;
            }
            return collection.Equals(other.Collection) && id == other.Id;
        }

        public override bool Equals(object other)
        {
            NxNode node = other as NxNode;
            if( node != null )
            {
                return Equals(node);
            }
            return false;
        }

        public override int GetHashCode()
        {
            int result = 17;
            result = 37 * result + id.GetHashCode();
            result = 37 * result + collection.GetHashCode();
            return result;
        }

        #endregion
    }
}
