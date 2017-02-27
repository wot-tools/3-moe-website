using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WGApi
{
    public class WinrateRecord
    {
        [JsonProperty("tank_id")]
        public int TankID { get; set; }
        [JsonProperty("statistics")]
        private Statistics Stats { get; set; }

        public int Battles { get { return Stats.Battles; } }
        public int Victories { get { return Stats.Victories; } }

        private class Statistics
        {
            [JsonProperty("wins")]
            public int Victories { get; set; }
            [JsonProperty("battles")]
            public int Battles { get; set; }
        }
    }
}
