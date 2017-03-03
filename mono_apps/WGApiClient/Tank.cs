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
        public string Nation { get; set; }
    }
}
