using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using org.basex.data;
using org.basex.query.item;

namespace Nxdb
{
    public class Document : ContainerNode
    {
        internal Document(ANode aNode, Database database) : base(aNode, database, Data.DOC) { }

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

        #endregion
    }
}
