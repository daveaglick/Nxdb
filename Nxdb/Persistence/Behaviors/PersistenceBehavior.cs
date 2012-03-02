using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace Nxdb.Persistence.Behaviors
{
    /// <summary>
    /// This is the base class for reusable persistence behaviors. To add new persistence
    /// behaviors, create a derived class and override Fetch, Store, or both.
    /// </summary>
    public abstract class PersistenceBehavior
    {
        internal virtual void Fetch(ContainerNode node, object obj, TypeCache typeCache)
        {
            Fetch(node, obj);
        }

        internal virtual void Store(ContainerNode node, object obj, TypeCache typeCache)
        {
            Store(node, obj);
        }

        /// <summary>
        /// Fetches data for the specified object from the specified node.
        /// </summary>
        /// <param name="obj">The object to fetch data for.</param>
        /// <param name="node">The node to fetch data from.</param>
        public virtual void Fetch(ContainerNode node, object obj)
        {
        }

        /// <summary>
        /// Stores data for the specified object to the specified node.
        /// </summary>
        /// <param name="obj">The object to store data for.</param>
        /// <param name="node">The node to store data to.</param>
        public virtual void Store(ContainerNode node, object obj)
        {
        }
    }
}
