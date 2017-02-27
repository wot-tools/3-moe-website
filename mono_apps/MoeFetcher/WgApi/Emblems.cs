using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MoeFetcher.WgApi
{
    class Emblem
    {
        [JsonProperty("portal")]
        public string Portal { get; set; }
        [JsonProperty("wowp")]
        public string Wowp { get; set; }
        [JsonProperty("wot")]
        public string Wot { get; set; }
    }
}
