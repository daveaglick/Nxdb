using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nxdb.Persistence
{
    /// <summary>
    /// Attribute that can be placed on persistent objects to control their behavior.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class PersistentObjectAttribute : System.Attribute
    {

    }
}
