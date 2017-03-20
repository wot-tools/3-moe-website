using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WGApi
{
    public class TankopediaInfo
    {
        [JsonProperty("vehicle_crew_roles")]
        public Dictionary<string, string> CrewRoles { get; set; }
        [JsonProperty("tanks_updated_at")]
        private int TanksUpdatedAtEpoch { set { TanksUpdatedAt = EpochDateTime.FromEpoch(value); } }
        [JsonProperty("languages")]
        public Dictionary<string, string> Languages { get; set; }
        [JsonProperty("achievement_sections")]
        public Dictionary<string, AchievementSection> AchievementSections { get; set; }
        [JsonProperty("vehicle_types")]
        public Dictionary<string, string> VehicleTypes { get; set; }
        [JsonProperty("vehicle_nations")]
        public Dictionary<string, string> Nations { get; set; }
        [JsonProperty("game_version")]
        public string GameVersion { get; set; }


        public DateTime TanksUpdatedAt { get; set; }
    }
}
