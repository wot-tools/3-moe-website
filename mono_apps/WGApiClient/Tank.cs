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
        public Nations Nation { get; set; }
        [JsonProperty("tier")]
        public int Tier { get; set; }
        [JsonProperty("type"), JsonConverter(typeof(VehicleTypeConverter))]
        public VehicleTypes VehicleType { get; set; }
        [JsonProperty("images")]
        public Icons Icons { get; set; }

        private class VehicleTypeConverter : JsonConverter
        {
            private static VehicleTypeFactory Factory = new VehicleTypeFactory();

            public override bool CanConvert(Type objectType)
            {
                return true;
            }

            public override bool CanWrite => false;

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                return Factory.GetVehicleType(serializer.Deserialize<string>(reader));
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                throw new NotImplementedException();
            }

            private class VehicleTypeFactory
            {
                public VehicleTypes GetVehicleType(string s)
                {
                    switch (s)
                    {
                        case "heavyTank": return VehicleTypes.Heavy;
                        case "mediumTank": return VehicleTypes.Medium;
                        case "lightTank": return VehicleTypes.Light;
                        case "AT-SPG": return VehicleTypes.TD;
                        case "SPG": return VehicleTypes.SPG;
                        default: throw new NotImplementedException();
                    }
                }
            }
        }
    }
}
