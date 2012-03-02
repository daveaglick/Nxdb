namespace Nxdb.Persistence.Behaviors
{
    /// <summary>
    /// Implement this interface to provide custom behavior on a per-object basis.
    /// </summary>
    public interface ICustomPersistence
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

    internal class CustomBehavior : PersistenceBehavior
    {
        internal override void Fetch(ContainerNode node, object obj, TypeCache typeCache)
        {
            ((ICustomPersistence)obj).Fetch(node);
        }

        internal override void Store(ContainerNode node, object obj, TypeCache typeCache)
        {
            ((ICustomPersistence)obj).Store(node);
        }
    }
}
