using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using WGApi;
using System.Threading;
using System.Linq;

namespace MoeFetcher
{
    class Program
    {
        static int Main(string[] args)
        {
            App app = new App(new Logger());
            return app.Run(args);
        }

        static void TestApiClient()
        {
            int[] playerIDs = { 501114475, 523993923 };
            int[] clanIDs = { 500025989, 500034335 };

            WGApiClient client = new WGApiClient("https://api.worldoftanks", Region.eu, "de900a7eb3e71b2c44543abdcc2ee8ea", new Logger());
            var marks = client.GetPlayerMarks(playerIDs.First());
            var wr = client.GetPlayerWinrateRecords(playerIDs);
            var vehicleStats = client.GetPlayerTankStats(playerIDs.First());
            var players = client.GetPlayerStats(playerIDs);
            var clans = client.GetClanInformation(clanIDs);
        }
    }
}
