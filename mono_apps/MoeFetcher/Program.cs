using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace MoeFetcher
{
    class Program
    {
        private static string StillUglyPath { get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..\\.."); } }

        static void Main(string[] args)
        {
            WGApiClient client = new WGApiClient();
            Player foo = client.GetPlayerMarks(-1, 2);
            Player[] bar = client.GetPlayerWinrateRecords();
            //Tokenize("TankBattles");
            Console.Read();
        }



        static void Tokenize(string file)
        {
            Tokenize(Path.Combine(StillUglyPath, $"{file}.json"), Path.Combine(StillUglyPath, $"{file}.parsed"));
        }

        static void Tokenize(string jsonPath, string savePath)
        {
            int tabCount, nextTabCount = 0;
            using (FileStream saveStream = File.Create(savePath))
            using (StreamWriter writer = new StreamWriter(saveStream))
            using (StreamReader streamReader = new StreamReader(jsonPath))
            using (JsonTextReader reader = new JsonTextReader(streamReader))
                while (reader.Read())
                {
                    tabCount = nextTabCount;
                    if (reader.TokenType == JsonToken.StartArray || reader.TokenType == JsonToken.StartObject)
                        nextTabCount += 1;
                    if (reader.TokenType == JsonToken.EndArray || reader.TokenType == JsonToken.EndObject)
                        tabCount = nextTabCount -= 1;
                    writer.WriteLine(String.Format("{0}{1}{2}", "\t".Repeat(tabCount), reader.TokenType.ToString().PadRight(20), reader.Value));
                }
        }
    }
}
