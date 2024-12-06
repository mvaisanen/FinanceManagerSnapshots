using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Server.Util
{
    public static class StringExtensions
    {
        public static string ToFirstLetterUpper(this string s)
        {
            var res = s.ToLower();
            if (res.Length > 0)
                res = res[0].ToString().ToUpper() + res.Substring(1);
            return res;
        }

        public static string ToCamelCase(this string s)
        {
            if (s.Length == 1)
                return s.ToLower();
            return Char.ToLowerInvariant(s[0]) + s.Substring(1);
        }
    }
}
