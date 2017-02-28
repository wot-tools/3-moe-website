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
    }
}
