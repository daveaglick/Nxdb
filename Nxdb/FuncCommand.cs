using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using org.basex.core;

namespace Nxdb
{
    //internal class FuncCommand : Command
    //{
    //    private readonly Func<Context, string> _func;

    //    public FuncCommand(Func<Context, string> func)
    //        : base(User.ADMIN)
    //    {
    //        if (func == null) throw new ArgumentNullException("func");
    //        _func = func;
    //    }

    //    public FuncCommand(Action<Context> action)
    //        : base(User.ADMIN)
    //    {
    //        if (action == null) throw new ArgumentNullException("action");
    //        _func = ctx =>
    //        {
    //            action(ctx);
    //            return String.Empty;
    //        };
    //    }

    //    protected override bool run()
    //    {
    //        try
    //        {
    //            return info(_func(context));
    //        }
    //        catch (Exception ex)
    //        {
    //            return error(ex.Message);
    //        }
    //    }
    //}
}
