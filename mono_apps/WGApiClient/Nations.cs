using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WGApi
{
    [Flags]
    public enum Nations
    {
        None    = 0,
        USSR    = 1 << 0,
        France  = 1 << 1,
        Germany = 1 << 2,
        USA     = 1 << 3,
        UK      = 1 << 4,
        China   = 1 << 5,
        Japan   = 1 << 6,
        Czech   = 1 << 7,
        Sweden  = 1 << 8,

        All     = ~(~0 << 9),
    }
}
