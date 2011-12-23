using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nxdb
{
    public interface IQuery
    {
        IEnumerable<object> Eval(string expression);
        IEnumerable<T> Eval<T>(string expression);
        IList<object> EvalList(string expression);
        IList<T> EvalList<T>(string expression);
        object EvalSingle(string expression);
        T EvalSingle<T>(string expression) where T : class;
    }
}
