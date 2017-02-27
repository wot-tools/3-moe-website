using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WGApi
{
    public class Statistics
    {
        [JsonProperty("spotted")]
        public int Spotted { get; set; }
        [JsonProperty("battles")]
        public int Battles { get; set; }
        [JsonProperty("wins")]
        public int Victories { get; set; }
        [JsonProperty("damage_dealt")]
        public int Damage { get; set; }
        [JsonProperty("xp")]
        public int Experience { get; set; }
        [JsonProperty("frags")]
        public int Frags { get; set; }
        [JsonProperty("survived_battles")]
        public int SurvivedBattles { get; set; }
        [JsonProperty("capture_points")]
        public int Cap { get; set; }
        [JsonProperty("dropped_capture_points")]
        public int Decap { get; set; }
        [JsonProperty("shots")]
        public int Shots { get; set; }
        [JsonProperty("hits")]
        public int Hits { get; set; }
        [JsonProperty("draws")]
        public int Draws { get; set; }
        [JsonProperty("damage_received")]
        public int DamageReveived { get; set; }
    }
}
