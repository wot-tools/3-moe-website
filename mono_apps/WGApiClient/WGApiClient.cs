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

        public async Task<TankopediaInfo> GetTankopediaInformationAsync()
        {
            return await GetApiResponseAsync<TankopediaInfo>("wot/encyclopedia/info/", BuildParameterString());
        }

        public async Task<Moe[]> GetPlayerMarksAsync(int id)
        {
            var result = await GetApiResponseAsync<Dictionary<int, Moe[]>>("wot/tanks/achievements/", BuildParameterString("achievements,tank_id", id.ToString()));
            return result.Single().Value;
        }

        public async Task<Dictionary<int, WinrateRecord[]>> GetPlayerWinrateRecordsAsync(IEnumerable<int> ids)
        {
            return await GetApiResponseAsync<Dictionary<int, WinrateRecord[]>>("wot/account/tanks/", BuildParameterString(accountID: String.Join(",", ids)));
        }

        public async Task<TankStats[]> GetPlayerTankStatsAsync(int id)
        {
            var result = await GetApiResponseAsync<Dictionary<int, TankStats[]>>("wot/tanks/stats/", BuildParameterString("random,tank_id", id.ToString(), extra: "random"));
            return result.Single().Value;
        }

        public async Task<Dictionary<int, PlayerInfo>> GetPlayerStatsAsync(IEnumerable<int> ids)
        {
            string fields = "statistics.random,client_language,global_rating,logout_at,created_at,last_battle_time,updated_at,clan_id,nickname";
            return await GetApiResponseAsync<Dictionary<int, PlayerInfo>>("wot/account/info/", BuildParameterString(fields, String.Join(",", ids), extra: "statistics.random"));
        }

        public async Task<Dictionary<int, Clan>> GetClanInformationAsync(IEnumerable<int> ids)
        {
            return await GetApiResponseAsync<Dictionary<int, Clan>>("wgn/clans/info/", BuildParameterString(clanID: String.Join(",", ids)));
        }

        public async Task<Dictionary<int, Tank>> GetVehiclesAsync()
        {
            string fields = "tag,name,nation,is_premium,short_name,tier,type,images";
            return await GetApiResponseAsync<Dictionary<int, Tank>>("wot/encyclopedia/vehicles/", BuildParameterString(fields));
        }

        public async Task<Dictionary<string, int>> SearchPlayerStartsWithAsync(string search)
        {
            if (search.Length < 3)
                return null;
            var result = await GetApiResponseAsync<PlayerIDRecord[]>("wot/account/list/", BuildParameterString(search: search));
            return result.ToDictionary(r => r.Nickname, r => r.ID);
        }

        public async Task<int> SearchPlayerExactAsync(string search)
        {
            try
            {
                var result = await GetApiResponseAsync<PlayerIDRecord[]>("wot/account/list/", BuildParameterString(search: search, type: "exact"));
                return result.SingleOrDefault()?.ID ?? -1;
            }
            catch (JsonSerializationException)
            {
                return -1;
            }
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

        private async Task<T> GetApiResponseAsync<T>(string endpoint, string parameters)
        {
            string apiResponse = await GetApiResponseAsync(endpoint, parameters);
            return JsonConvert.DeserializeObject<WrappedResponse<T>>(apiResponse).Data;
        }

        private async Task<string> GetApiResponseAsync(string endpoint, string parameters)
        {
#if MEASURE
            Interlocked.Increment(ref ApiResponsesCount);
#endif
            WebRequest request = WebRequest.Create($"{BaseUri}{endpoint}{parameters}");
            using (var response = await request.GetResponseAsync())
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
