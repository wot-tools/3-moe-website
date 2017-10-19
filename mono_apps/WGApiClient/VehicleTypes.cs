using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WGApi
{
    public enum VehicleTypes
    {
        None = 0,
        Heavy = 1 << 0,
        Medium = 1 << 1,
        Light = 1 << 2,
        TD = 1 << 3,
        SPG = 1 << 4,

        All = ~(~0 << 5)
    }
}
