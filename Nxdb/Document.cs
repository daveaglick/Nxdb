using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using org.basex.data;
using org.basex.query.item;
using org.basex.query.up.expr;
using org.basex.query.up.primitives;

namespace Nxdb
{
    public class Document : ContainerNode
    {
        internal Document(ANode aNode) : base(aNode, Data.DOC) { }

        public Document(string name) : base(new FDoc(name.Token()), Data.DOC) { }

        public override System.Xml.XmlNodeType NodeType
        {
            get { return System.Xml.XmlNodeType.Document; }
        }

        #region Content

        /// <summary>
        /// Document does not support inserting content before it. This method will always throw
        /// a NotSupportedException.
        /// </summary>
        /// <param name="xmlReader">The XML reader to get content from.</param>
        public override void InsertBefore(System.Xml.XmlReader xmlReader)
        {
            throw new NotSupportedException("cannot insert before a document node");
        }

        /// <summary>
        /// Document does not support inserting content after it. This method will always throw
        /// a NotSupportedException.
        /// </summary>
        /// <param name="xmlReader">The XML reader to get content from.</param>
        public override void InsertAfter(System.Xml.XmlReader xmlReader)
        {
            throw new NotSupportedException("cannot insert after a document node");
        }

        public override string Name
        {
            get
            {
                return BaseUri;
            }
            set
            {
                //Need to use the update primitive (as opposed to the expression) because documents can't be renamed through the expression
                if (value == null) throw new ArgumentNullException("value");
                Check(true);
                using (new Update())
                {
                    Update.Add(new ReplaceValue(DbNode.pre, DbNode.data(), null, value.Token()));
                }
            }
        }

        #endregion
    }
}
