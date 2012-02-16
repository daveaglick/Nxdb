/*
 * Copyright 2012 WildCard, LLC
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 * 
 */

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
