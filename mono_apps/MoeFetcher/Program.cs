using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using MoeFetcher.WgApi;
using System.Threading;

namespace MoeFetcher
{
    class Program
    {
        private static string StillUglyPath { get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..\\.."); } }

        static int Main(string[] args)
        {
            App app = new App(null);
            return app.Run(args);
        }

        static void TestApiClient()
        {
            int[] playerIDs = { 501114475, 523993923 };
            int[] clanIDs = { 500025989, 500034335 };

            WGApiClient client = new WGApiClient("https://api.worldoftanks", Region.eu, "de900a7eb3e71b2c44543abdcc2ee8ea");
            Player marks = client.GetPlayerMarks(playerIDs[0], 2);
            Player[] winrates = client.GetPlayerWinrateRecords(playerIDs);
            Player[] playerStats = client.GetPlayerStats(playerIDs);
            Clan[] clanInfo = client.GetClanInformation(clanIDs);
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
