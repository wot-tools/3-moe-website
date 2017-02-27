using Newtonsoft.Json;

namespace WGApi
{
    public class TankStats
    {
        [JsonProperty("tank_id")]
        public int TankID { get; set; }
        [JsonProperty("random")]
        public Statistics Stats { get; set; }
    }
}
