using System;
using System.Linq;

namespace MoeFetcher
{
    static class StringExtensions
    {
        public static string Repeat(this string s, int count)
        {
            return String.Join("", Enumerable.Repeat(s, count));
        }
    }
}
