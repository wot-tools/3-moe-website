using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WGApi
{
    public class Tank
    {
        [JsonProperty("is_premium")]
        public bool IsPremium { get; set; }
        [JsonProperty("tag")]
        public string Tag { get; set; }
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("short_name")]
        public string ShortName { get; set; }
        [JsonProperty("nation")]
        public Nations Nation { get; set; }
        [JsonProperty("tier")]
        public int Tier { get; set; }
        [JsonProperty("type")]
        public string VehicleType { get; set; }
        [JsonProperty("images")]
        public Icons Icons { get; set; }
    }
}
