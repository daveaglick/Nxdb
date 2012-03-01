using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nxdb.Persistence
{
    internal class ExplicitBehavior : IPersistenceBehavior
    {
        public void Fetch(ContainerNode node, object obj)
        {
            throw new NotImplementedException();
        }

        public void Store(ContainerNode node, object obj)
        {
            throw new NotImplementedException();
        }
    }
}
