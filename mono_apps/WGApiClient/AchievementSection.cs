using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WGApi
{
    public class AchievementSection
    {
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("order")]
        public int Order { get; set; }
    }
}
