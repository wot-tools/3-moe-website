using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WGApi
{
    [Flags]
    public enum Tiers
    {
        None    = 0,
        One     = 1 << 0,
        Two     = 1 << 1,
        Three   = 1 << 2,
        Four    = 1 << 3,
        Five    = 1 << 4,
        Six     = 1 << 5,
        Seven   = 1 << 6,
        Eight   = 1 << 7,
        Nine    = 1 << 8,
        Ten     = 1 << 9,

        All     = ~(~0 << 10),
    }
}
