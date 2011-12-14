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
    }
}
