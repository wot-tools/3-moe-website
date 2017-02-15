using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MoeFetcher
{
    class Program
    {
        static void Main(string[] args)
        {
            object foo;
            string testJson = @"""data"": {""052124"": [{}] }";
            using (Stream stream = File.OpenRead(@"F:\repos\3-moe-website\mono_apps\MoeFetcher\MoeList.json"))
            using (StreamReader reader = new StreamReader(stream))
                foo = JsonConvert.DeserializeObject<Foo>(reader.ReadToEnd());
            //var foo = JsonConvert.DeserializeObject<Foo>(testJson);
        }
    }

    class Foo
    {
        public DataWrapper data { get; set; }
    }

    class DataWrapper
    {
        [JsonProperty(Order = 1)]
        public Tank[] foo { get; set; }
    }

    class Tank
    {
        public MarkObject achievements { get; set; }
        public long account_id { get; set; }
        public int tank_id { get; set; }
    }

    class MarkObject
    {
        public int markOfMastery { get; set; }
    }
}
