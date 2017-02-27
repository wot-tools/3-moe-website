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

    static class EpochDateTime
    {
        public static DateTime FromEpoch(int epoch)
        {
            return new DateTime(1970, 1, 1).AddSeconds(epoch);
        }
    }
}
