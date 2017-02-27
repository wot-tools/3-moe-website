using Newtonsoft.Json;

namespace MoeFetcher.WgApi
{
    class Moe
    {
        [JsonProperty("tank_id")]
        public int TankID { get; set; }
        [JsonProperty("achievements")]
        private Achievement Achievements { get; set; }

        public int Mark { get { return Achievements.Mark; } }

        public class Achievement
        {
            [JsonProperty("marksOnGun")]
            public int Mark { get; set; }
        }
    }
}
