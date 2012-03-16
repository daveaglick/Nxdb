using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Nxdb.Node;

namespace Nxdb.Persistence
{
    /// <summary>
    /// Implement this interface to provide custom persistence logic on a per-object basis.
    /// </summary>
    public interface ICustomPersistence
    {
        /// <summary>
        /// Refreshes the persistent object's state from the specified database element.
        /// </summary>
        /// <param name="element">The element the object is currently attached to.</param>
        void Fetch(Element element);

        /// <summary>
        /// Serializes this to an arbitrary object that contains the content to store.
        /// The return value will be passed to Store.
        /// </summary>
        object Serialize();

        /// <summary>
        /// Saves the serialized content to the specified database element.
        /// </summary>
        /// <param name="serialized">The arbitrary serialized content returned by Serialize.</param>
        /// <param name="element">The element the object is currently attached to.</param>
        void Store(Element element, object serialized);
    }
}
