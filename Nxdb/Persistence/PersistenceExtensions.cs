using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nxdb.Persistence
{
    /// <summary>
    /// Provides extension methods for the persistence framework.
    /// </summary>
    public static class PersistenceExtensions
    {
        public static T GetObject<T>(this ContainerNode node) where T : class
        {
            return PersistenceManager.Default.GetObject<T>(node);
        }

        public static T GetObject<T>(this ContainerNode node, bool attach, bool searchCache) where T : class
        {
            return PersistenceManager.Default.GetObject<T>(node, attach, searchCache);
        }

        public static IEnumerable<T> GetObjects<T>(this ContainerNode node, string expression) where T : class
        {
            return PersistenceManager.Default.GetObjects<T>(node, expression);
        }

        public static IEnumerable<T> GetObjects<T>(this ContainerNode node, string expression,
            bool attach, bool searchCache, bool attachResults) where T : class
        {
            return PersistenceManager.Default.GetObjects<T>(node, expression, attach, searchCache, attachResults);
        }

        public static void Attach(this object obj, ContainerNode node)
        {
            PersistenceManager.Default.Attach(obj, node);
        }

        public static void Attach(this ContainerNode node, object obj)
        {
            PersistenceManager.Default.Attach(obj, node);
        }

        public static void Append(this object obj, ContainerNode parent)
        {
            PersistenceManager.Default.Append(obj, parent);
        }

        public static void Append(this object obj, ContainerNode parent, string elementName)
        {
            PersistenceManager.Default.Append(obj, parent, elementName);
        }

        public static void Append(this ContainerNode parent, object obj)
        {
            PersistenceManager.Default.Append(obj, parent);
        }

        public static void Append(this ContainerNode parent, object obj, string elementName)
        {
            PersistenceManager.Default.Append(obj, parent, elementName);
        }

        public static void Detach(this object obj)
        {
            PersistenceManager.Default.Detach(obj);
        }

        public static void Fetch(this object obj, ContainerNode node)
        {
            PersistenceManager.Default.Fetch(obj, node);
        }

        public static void Fetch(this ContainerNode node, object obj)
        {
            PersistenceManager.Default.Fetch(obj, node);
        }

        public static void Fetch(this object obj)
        {
            PersistenceManager.Default.Fetch(obj);
        }

        public static void Store(this object obj, ContainerNode node)
        {
            PersistenceManager.Default.Store(obj, node);
        }

        public static void Store(this ContainerNode node, object obj)
        {
            PersistenceManager.Default.Store(obj, node);
        }

        public static void Store(this object obj)
        {
            PersistenceManager.Default.Store(obj);
        }
    }
}
