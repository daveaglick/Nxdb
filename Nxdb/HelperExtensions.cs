using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using org.basex.util;

namespace Nxdb
{
    internal static class HelperExtensions
    {
        //A couple extension methods to help with the tokenizing of strings
        internal static string Token(this byte[] bytes)
        {
            return org.basex.util.Token.@string(bytes);
        }

        internal static byte[] Token(this string str)
        {
            return org.basex.util.Token.token(str);
        }
    }
}
