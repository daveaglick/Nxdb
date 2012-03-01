using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nxdb.Persistence
{
    internal class CustomBehavior : IPersistenceBehavior
    {
        public void Fetch(ContainerNode node, object obj)
        {
            ((ICustomPersistentObject)obj).Fetch(node);
        }

        public void Store(ContainerNode node, object obj)
        {
            ((ICustomPersistentObject)obj).Store(node);
        }
    }
}
