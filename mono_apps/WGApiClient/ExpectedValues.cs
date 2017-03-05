using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WGApi
{
    public class ExpectedValues
    {
        [JsonProperty("IDNum")]
        public int TankID { get; set; }
        [JsonProperty("expFrag")]
        public double Frags { get; set; }
        [JsonProperty("expDamage")]
        public double Damage { get; set; }
        [JsonProperty("expSpot")]
        public double Spots { get; set; }
        [JsonProperty("expDef")]
        public double Defense { get; set; }
        [JsonProperty("expWinRate")]
        public double Winrate { get; set; }

        public static ExpectedValues operator +(ExpectedValues values1, ExpectedValues values2)
        {
            return Operate(values1, values2, (d1, d2) => d1 + d2);
        }

        public static ExpectedValues operator *(ExpectedValues values, double multiplier)
        {
            return Operate(values, multiplier, (d1, d2) => d1 * d2);
        }

        public static ExpectedValues operator /(ExpectedValues values, double multiplier)
        {
            return Operate(values, multiplier, (d1, d2) => d1 / d2);
        }

        private static ExpectedValues Operate(ExpectedValues values1, ExpectedValues values2, Func<double, double, double> operation)
        {
            return new ExpectedValues
            {
                Frags = operation(values1.Frags, values2.Frags),
                Damage = operation(values1.Damage, values2.Damage),
                Spots = operation(values1.Spots, values2.Spots),
                Defense = operation(values1.Defense, values2.Defense),
                Winrate = operation(values1.Winrate, values2.Winrate),
            };
        }

        private static ExpectedValues Operate(ExpectedValues values, double d, Func<double, double, double> operation)
        {
            return new ExpectedValues
            {
                Frags = operation(values.Frags, d),
                Damage = operation(values.Damage, d),
                Spots = operation(values.Spots, d),
                Defense = operation(values.Defense, d),
                Winrate = operation(values.Winrate, d),
            };
        }
    }
}
