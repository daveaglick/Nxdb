using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace Nxdb.Persistence
{
    internal interface IPersistenceBehavior
    {
        void Fetch(ContainerNode node, object obj);
        void Store(ContainerNode node, object obj);
    }
}
