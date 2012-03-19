using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Nxdb.Node;

namespace Nxdb.Persistence
{
    /// <summary>
    /// Implement this interface to provide custom persistence logic.
    /// If only specific persistence operations need custom logic, the individual
    /// custom persistence interfaces can be implemented instead (ICustomInitialize,
    /// ICustomFetch, and ICustomStore). The custom persistence methods will be
    /// called after any persistence attributes are processed.
    /// </summary>
    public interface ICustomPersistence :
        ICustomInitialize, ICustomFetch, ICustomStore
    {
    }

    /// <summary>
    /// Implement this interface to provide custom persistence initialization logic.
    /// The custom persistence method will be called after any persistence attributes
    /// are processed.
    /// </summary>
    public interface ICustomInitialize
    {
        /// <summary>
        /// Initializes a persistent object after construction. Since the object must
        /// have an empty default constructor and none of the persistent members are
        /// populated at construction, this allows the object to provide more complete
        /// initialization that uses persistent members if required.
        /// </summary>
        void Initialize(Element element);
    }

    /// <summary>
    /// Implement this interface to provide custom persistence fetch logic.
    /// The custom persistence method will be called after any persistence attributes
    /// are processed.
    /// </summary>
    public interface ICustomFetch
    {
        /// <summary>
        /// Refreshes the persistent object's state from the specified database element.
        /// </summary>
        /// <param name="element">The element the object is currently attached to.</param>
        void Fetch(Element element);
    }

    /// <summary>
    /// Implement this interface to provide custom persistence store logic.
    /// The custom persistence methods will be called after any persistence attributes
    /// are processed.
    /// </summary>
    public interface ICustomStore
    {
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
