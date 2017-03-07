using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WGApi
{
    public class PlayerIDRecord
    {
        [JsonProperty("nickname")]
        public string Nickname { get; set; }
        [JsonProperty("account_id")]
        public int ID { get; set; }

        public override string ToString()
        {
            return Nickname;
        }
    }
}
