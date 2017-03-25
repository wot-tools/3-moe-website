using Newtonsoft.Json;
using System;

namespace WGApi
{
    public class PlayerInfo
    {
        [JsonProperty("clan_id")]
        public int? ClanID { get; set; }
        [JsonProperty("nickname")]
        public string Nick { get; set; }
        [JsonProperty("client_language")]
        public string ClientLanguage { get; set; }
        [JsonProperty("logout_at")]
        private int EpochLastLogout { set { LastLogout = EpochDateTime.FromEpoch(value); } }
        [JsonProperty("created_at")]
        private int EpochAccountCreated { set { AccountCreated = EpochDateTime.FromEpoch(value); } }
        [JsonProperty("last_battle_time")]
        private int EpochLastBattle { set { LastBattle = EpochDateTime.FromEpoch(value); } }
        [JsonProperty("updated_at")]
        private int EpochUpdatedAt { set { UpdatedAt = EpochDateTime.FromEpoch(value); } }
        [JsonProperty("global_rating")]
        public int WGRating { get; set; }
        [JsonProperty("statistics")]
        public Stats Statistics { get; set; }

        public DateTime LastLogout { get; set; }
        public DateTime AccountCreated { get; set; }
        public DateTime LastBattle { get; set; }
        public DateTime UpdatedAt { get; set; }

        public class Stats
        {
            [JsonProperty("random")]
            public Statistics Random { get; set; }
        }
    }
}
