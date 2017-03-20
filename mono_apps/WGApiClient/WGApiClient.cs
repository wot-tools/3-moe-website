using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WGApi
{
    public class WGApiClient
    {
        public Region Region { get; private set; }
        private string BaseUri;
        private string ApplicationID;
        private ILogger Logger;

        public WGApiClient(string baseUriWithoutTld, Region region, string applicationID, ILogger logger)
        {
            Region = region;
            ApplicationID = applicationID;

            BaseUri = $"{baseUriWithoutTld}.{region}/";
            Logger = logger;
#if MEASURE
            WindowEnd = DateTime.Now.AddSeconds(WindowSize);
            StartPerformanceMeasureThread();
#endif
        }

        public TankopediaInfo GetTankopediaInformation()
        {
            return GetApiResponse<TankopediaInfo>("wot/encyclopedia/info/", BuildParameterString());
        }

        public Moe[] GetPlayerMarks(int id)
        {
            return GetApiResponse<Dictionary<int, Moe[]>>("wot/tanks/achievements/", BuildParameterString("achievements,tank_id", id.ToString())).Single().Value;
        }

        public Dictionary<int, WinrateRecord[]> GetPlayerWinrateRecords(IEnumerable<int> ids)
        {
            return GetApiResponse<Dictionary<int, WinrateRecord[]>>("wot/account/tanks/", BuildParameterString(accountID: String.Join(",", ids)));
        }

        public TankStats[] GetPlayerTankStats(int id)
        {
            return GetApiResponse<Dictionary<int, TankStats[]>>("wot/tanks/stats/", BuildParameterString("random,tank_id", id.ToString(), extra: "random")).Single().Value;
        }

        public Dictionary<int, PlayerInfo> GetPlayerStats(IEnumerable<int> ids)
        {
            string fields = "statistics.random,client_language,global_rating,logout_at,created_at,last_battle_time,updated_at,clan_id,nickname";
            return GetApiResponse<Dictionary<int, PlayerInfo>>("wot/account/info/", BuildParameterString(fields, String.Join(",", ids), extra: "statistics.random"));
        }

        public Dictionary<int, Clan> GetClanInformation(IEnumerable<int> ids)
        {
            return GetApiResponse<Dictionary<int, Clan>>("wgn/clans/info/", BuildParameterString(clanID: String.Join(",", ids)));
        }

        public Dictionary<int, Tank> GetVehicles()
        {
            string fields = "tag,name,nation,is_premium,short_name,tier,type,images";
            return GetApiResponse<Dictionary<int, Tank>>("wot/encyclopedia/vehicles/", BuildParameterString(fields));
        }

        public Dictionary<string, int> SearchPlayerStartsWith(string search)
        {
            if (search.Length < 3)
                return null;
            return GetApiResponse<PlayerIDRecord[]>("wot/account/list/", BuildParameterString(search: search)).ToDictionary(r => r.Nickname, r => r.ID);
        }

        public int SearchPlayerExact(string search)
        {
            return GetApiResponse<PlayerIDRecord[]>("wot/account/list/", BuildParameterString(search: search, type: "exact")).SingleOrDefault()?.ID ?? -1;
        }

#if MEASURE
        private DateTime WindowEnd;
        private int WindowSize = 1;
        private int ApiResponsesCount = 0;
        Thread PerformanceMeasureThread;

        private void StartPerformanceMeasureThread()
        {
            PerformanceMeasureThread = new Thread(_ =>
            {
                while (true)
                    if (WindowEnd < DateTime.Now)
                    {
                        WindowEnd = WindowEnd.AddSeconds(WindowSize);
                        int count = Interlocked.Exchange(ref ApiResponsesCount, 0);
                        Logger.Verbose("{0:000} api calls performed in the last {1} seconds", count, WindowSize);
                    }
                    else
                        Thread.Sleep(50);
            });
            PerformanceMeasureThread.Start();
        }

        ~WGApiClient()
        {
            PerformanceMeasureThread.Abort();
        }
#endif

        private T GetApiResponse<T>(string endpoint, string parameters)
        {
            string apiResponse = GetApiResponse(endpoint, parameters);
            return JsonConvert.DeserializeObject<WrappedResponse<T>>(apiResponse).Data;
        }

        private string GetApiResponse(string endpoint, string parameters)
        {
#if MEASURE
            Interlocked.Increment(ref ApiResponsesCount);
#endif
            WebRequest request = WebRequest.Create($"{BaseUri}{endpoint}{parameters}");
            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            using (Stream stream = response.GetResponseStream())
            using (StreamReader reader = new StreamReader(stream))
                return reader.ReadToEnd();
        }

        private string BuildParameterString(string fields = null, string accountID = null, string clanID = null, string extra = null, string search = null, string type = null)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append($"?application_id={ApplicationID}");
            if (fields != null)
                builder.Append($"&fields={fields}");
            if (accountID != null)
                builder.Append($"&account_id={accountID}");
            if (clanID != null)
                builder.Append($"&clan_id={clanID}");
            if (extra != null)
                builder.Append($"&extra={extra}");
            if (search != null)
                builder.Append($"&search={search}");
            if (type != null)
                builder.Append($"&type={type}");

            return builder.ToString();
        }

        private class WrappedResponse<T>
        {
            [JsonProperty("status")]
            public Status Status { get; set; }
            [JsonProperty("meta")]
            public Meta Meta { get; set; }
            [JsonProperty("data")]
            public T Data { get; set; }
        }

        private class Meta
        {
            [JsonProperty("count")]
            public int Count { get; set; }
        }
    }
}
