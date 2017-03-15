using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WGApi
{
    public class Icons
    {
        [JsonProperty("small_icon")]
        public string Small { get; set; }
        [JsonProperty("contour_icon")]
        public string Contour { get; set; }
        [JsonProperty("big_icon")]
        public string Big { get; set; }
    }
}
