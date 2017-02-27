using Newtonsoft.Json;

namespace MoeFetcher.WgApi
{
    class TankStats
    {
        [JsonProperty("tank_id")]
        public int TankID { get; set; }
        [JsonProperty("random")]
        public Statistics Stats { get; set; }
    }
}
