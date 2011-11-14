using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using ikvm.@internal;
using java.io;
using Nxdb.Io;
using java.lang;
using org.basex.api.xmldb;
using org.basex.core;
using org.basex.core.cmd;
using org.basex.data;
using org.basex.io;
using org.basex.util;
using org.basex.build;
using org.xmldb.api;
using File = java.io.File;
using String = System.String;
using StringReader = java.io.StringReader;

namespace Nxdb
{
    //Represents a single BaseX database into which all documents should be stored
    //Unlike the previous versions, it's recommended just one overall NxDatabase be used
    //and querying between them is not supported
    //The documents in the database can be grouped using paths in the document name,
    //i.e.: "folderA/docA.xml", "folderA/docB.xml", "folderB/docC.xml"
    public class NxDatabase : IDisposable
    {
        private static string _home = null;

        public static void SetHome(string path)
        {
            if (path == null) throw new ArgumentNullException("path");
            if (path == String.Empty) throw new ArgumentException("path");

            _home = path;

            //Create the home path
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            //Set the home path so the BaseX preference file will go there (rather than user path)
            Prop.HOME = Path.Combine(_home, "pref");
        }

        public static bool Drop(string name)
        {
            if (name == null) throw new ArgumentNullException("name");
            if (name == String.Empty) throw new ArgumentException("name");

            return Run(new DropDB(name), GetContext());
        }

        private static Context GetContext()
        {
            //Set a default home if one hasn't been provided yet
            if (_home == null)
            {
                SetHome(Path.Combine(Environment.CurrentDirectory, "Nxdb"));
            }

            //Now we can create the context since the path for preferences has been set
            Context context = new Context();

            //Now set the database path
            context.mprop.set(MainProp.DBPATH, _home);

            return context;
        }

        private readonly Context _context;

        public NxDatabase(string name)
        {
            if (name == null) throw new ArgumentNullException("name");
            if (name == String.Empty) throw new ArgumentException("name");
            
            _context = GetContext();

            //Open/create the requested database
            if(!Run(new Open(name)))
            {
                //Unable to open it, try creating
                if (!Run(new CreateDB(name)))
                {
                    throw new ArgumentException();
                }
            }
        }

        public void Dispose()
        {
            Run(new Close());
        }

        //BaseX commands can be run in one of two ways:
        //1) An instance of the command class can be created and passed to the following Run() method
        //2) When the above won't work, a Func or Action can be used to wrap a command and run it

        internal static bool Run(Command command, Context context)
        {
            bool ret = command.run(context);
            string info = command.info();
            Debug.WriteLine(info);  //TODO: Replace this with some kind of logging mechanism
            return ret;
        }

        internal static bool Run(Func<Context, string> func, Context context)
        {
            return Run(new FuncCommand(func), context);
        }

        internal static bool Run(Action<Context> action, Context context)
        {
            return Run(new FuncCommand(action), context);
        }

        internal bool Run(Command command)
        {
            return Run(command, _context);
        }

        internal bool Run(Func<Context, string> func)
        {
            return Run(new FuncCommand(func));
        }

        internal bool Run(Action<Context> action)
        {
            return Run(new FuncCommand(action));
        }

        //The following perform the same operation as their equivalent BaseX command:
        //http://docs.basex.org/wiki/Commands
        
        public bool Add(string path, string content)
        {
            return Run(new Add(path, content));
        }

        public bool Delete(string path)
        {
            return Run(new Delete(path));
        }

        public bool Rename(string path, string newPath)
        {
            return Run(new Rename(path, newPath));
        }

        public bool Replace(string path, string content)
        {
            return Run(new Replace(path, content));
        }

        //More direct access

        internal Data Data
        {
            get { return _context.data(); }
        }

        public NxNode Get(string path)
        {
            int pre = Data.doc(path);
            return pre == -1 ? null : new NxNode(this, pre);
        }
    }
}
