using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nxdb.Persistence
{
    /// <summary>
    /// Implement this member to explicitly declare a persistent object. If this interface is
    /// provided for an object, it will be inspected for persistent attributes that
    /// indicate how the object should be persisted. If an object is attached without the
    /// IPersistentObject interface, all fields will be implicitly persisted.
    /// </summary>
    public interface IPersistentObject
    {
    }

    /// <summary>
    /// Implement this interface to provide custom behavior for refreshing,
    /// saving, etc. This interface does not need to be implemented in order
    /// to make an object persistent.
    /// </summary>
    public interface ICustomPersistentObject : IPersistentObject
    {
        /// <summary>
        /// Refreshes the persistent object's state from the specified database node.
        /// </summary>
        /// <param name="node">The node the object is currently attached to.</param>
        void Fetch(ContainerNode node);

        /// <summary>
        /// Saves the persistent object's state to the specified database node.
        /// </summary>
        /// <param name="node">The node the object is currently attached to.</param>
        void Store(ContainerNode node);
    }
}
