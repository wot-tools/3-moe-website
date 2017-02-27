using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WGApi
{
    internal static class EpochDateTime
    {
        public static DateTime FromEpoch(int epoch)
        {
            return new DateTime(1970, 1, 1).AddSeconds(epoch);
        }
    }
}
