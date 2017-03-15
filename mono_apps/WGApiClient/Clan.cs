using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WGApi
{
    public class Clan
    {
        [JsonProperty("clan_id")]
        public int ID { get; set; }
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("tag")]
        public string Tag { get; set; }
        [JsonProperty("members_count")]
        public int Count { get; set; }
        [JsonProperty("color")]
        public string Color { get; set; }
        [JsonProperty("updated_at")]
        public int EpochUpdatedAt { set { UpdatedAt = EpochDateTime.FromEpoch(value); } }
        [JsonProperty("created_at")]
        private int EpochCreatedAt { set { CreatedAt = EpochDateTime.FromEpoch(value); } }
        [JsonProperty("emblems")]
        public Dictionary<string, Emblem> Emblems { get; set; }

        public DateTime UpdatedAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
