using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MoeFetcher.WgApi
{
    class WGApiClient
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
#if DEBUG
            WindowEnd = DateTime.Now.AddSeconds(WindowSize);
            StartMeasurePerformanceThread();
#endif
        }

        private int ReadInfo(CustomJsonReader reader)
        {
            if (reader.ReadValue("status", reader.ReadAsString) != "ok")
                throw new Exception();
            return reader.ReadValue("count", reader.ReadAsInt32) ?? throw new Exception();
        }

        public Player GetPlayerMarks(int id, int minMark)
        {
            string apiResponse = GetApiResponse("wot/tanks/achievements/", BuildParameterString("achievements,tank_id", id.ToString()));
            using (StringReader reader = new StringReader(apiResponse))
                return ReadPlayerMarks(new CustomJsonReader(reader), minMark);
        }

        private Player ReadPlayerMarks(CustomJsonReader reader, int minMark)
        {
            Player result = new Player();
            using (reader)
            {
                if (ReadInfo(reader) != 1) throw new Exception();
                reader.ReadToProperty("data");
                result.ID = int.Parse(reader.ReadNextPropertyNameAsData());
                result.Moes = reader.ReadArray(r => ReadTank(r), t => t.Mark >= minMark).ToArray();
            }
            return result;
        }

        private Moe ReadTank(CustomJsonReader reader)
        {
            if (false == reader.ReadToPropertyIfExisting("achievements", JsonToken.EndArray))
                return null;
            Moe result = new Moe();
            result.Mark = reader.ReadValue("marksOnGun", reader.ReadAsInt32, JsonToken.EndObject) ?? 0;
            result.TankID = reader.ReadValue("tank_id", reader.ReadAsInt32).Value;
            return result;
        }

        public Player[] GetPlayerWinrateRecords(IEnumerable<int> ids)
        {
            string apiResponse = GetApiResponse("wot/account/tanks/", BuildParameterString(account_id: String.Join(",", ids)));
            using (StringReader reader = new StringReader(apiResponse))
                return ReadPlayerWinrateRecords(new CustomJsonReader(reader));
        }

        private Player[] ReadPlayerWinrateRecords(CustomJsonReader reader)
        {
            using (reader)
            {
                if (ReadInfo(reader) < 0) throw new Exception();
                reader.ReadToProperty("data");
                return reader.ReadArray(r => ReadSinglePlayerWinrateRecords(r)).ToArray();
            }
        }

        private Player ReadSinglePlayerWinrateRecords(CustomJsonReader reader)
        {
            string id;
            if ((id = reader.ReadNextPropertyNameAsData(JsonToken.EndObject)) == null)
                return null;
            Player result = new Player();
            result.ID = int.Parse(id);
            result.WinrateRecords = reader.ReadArray(r => ReadWinrateRecord(r)).ToArray();
            return result;
        }

        private WinrateRecord ReadWinrateRecord(CustomJsonReader reader)
        {
            if (false == reader.ReadToPropertyIfExisting("statistics", JsonToken.EndArray))
                return null;
            WinrateRecord result = new WinrateRecord();
            result.Victories = reader.ReadValue("wins", reader.ReadAsInt32).Value;
            result.Battles = reader.ReadValue("battles", reader.ReadAsInt32).Value;
            result.TankID = reader.ReadValue("tank_id", reader.ReadAsInt32).Value;
            return result;
        }

        public Player[] GetPlayerStats(IEnumerable<int> ids)
        {
            string fields = "statistics.all,client_language,global_rating,logout_at,created_at,last_battle_time,updated_at,clan_id,nickname";
            string apiResponse = GetApiResponse("wot/account/info/", BuildParameterString(fields, String.Join(",", ids)));
            using (StringReader reader = new StringReader(apiResponse))
                return ReadPlayerStats(new CustomJsonReader(reader));
        }

        private Player[] ReadPlayerStats(CustomJsonReader reader)
        {
            using (reader)
            {
                if (ReadInfo(reader) < 0) throw new Exception();
                reader.ReadToProperty("data");
                return reader.ReadArray(r => ReadSinglePlayerStats(r)).ToArray();
            }
        }

        private Player ReadSinglePlayerStats(CustomJsonReader reader)
        {
            string id;
            if ((id = reader.ReadNextPropertyNameAsData(JsonToken.EndObject)) == null)
                return null;

            Player result = new Player();
            Statistics stats = new Statistics();
            result.ID = int.Parse(id);
            result.ClientLanguage = reader.ReadValue("client_language", reader.ReadAsString);
            stats.Spots = reader.ReadValue("spotted", reader.ReadAsInt32).Value;
            stats.Decap = reader.ReadValue("dropped_capture_points", reader.ReadAsInt32).Value;
            stats.Damage = reader.ReadValue("damage_dealt", reader.ReadAsInt32).Value;
            stats.Battles = reader.ReadValue("battles", reader.ReadAsInt32).Value;
            stats.Kills = reader.ReadValue("frags", reader.ReadAsInt32).Value;
            stats.Cap = reader.ReadValue("capture_points", reader.ReadAsInt32).Value;
            stats.Victories = reader.ReadValue("wins", reader.ReadAsInt32).Value;
            result.ClanID = reader.ReadValue("clan_id", reader.ReadAsInt32);
            result.AccountCreated = reader.ReadValue("created_at", reader.ReadAsEpoch);
            result.UpdatedAt = reader.ReadValue("updated_at", reader.ReadAsEpoch);
            stats.WGRating = reader.ReadValue("global_rating", reader.ReadAsInt32).Value;
            result.LastBattle = reader.ReadValue("last_battle_time", reader.ReadAsEpoch);
            result.Nick = reader.ReadValue("nickname", reader.ReadAsString);
            result.LastLogout = reader.ReadValue("logout_at", reader.ReadAsEpoch);
            reader.ReadToPropertyIfExisting("", JsonToken.EndObject); //hack to set reader to end of player object
            result.Statistics = stats;
            return result;
        }

        public Clan[] GetClanInformation(IEnumerable<int> ids)
        {
            string apiResponse = GetApiResponse("wgn/clans/info/", BuildParameterString(clan_id: String.Join(",", ids)));
            using (StringReader reader = new StringReader(apiResponse))
                return ReadClanInformation(new CustomJsonReader(reader));
        }

        private Clan[] ReadClanInformation(CustomJsonReader reader)
        {
            using (reader)
            {
                if (ReadInfo(reader) < 0) throw new Exception();
                reader.ReadToProperty("data");
                return reader.ReadArray(r => ReadSingleClan(r)).ToArray();
            }
        }

        private Clan ReadSingleClan(CustomJsonReader reader)
        {
            string id;
            if ((id = reader.ReadNextPropertyNameAsData(JsonToken.EndObject)) == null)
                return null;
            Clan result = new Clan();
            result.ID = int.Parse(id);
            result.Color = reader.ReadValue("color", reader.ReadAsString);
            result.UpdatedAt = reader.ReadValue("updated_at", reader.ReadAsEpoch);
            result.Tag = reader.ReadValue("tag", reader.ReadAsString);
            result.Count = reader.ReadValue("members_count", reader.ReadAsInt32).Value;
            result.Emblems = ReadEmblems(reader);
            //result.ID = reader.ReadValue("clan_id", reader.ReadAsInt32).Value;
            result.Name = reader.ReadValue("name", reader.ReadAsString);
            reader.ReadToPropertyIfExisting("", JsonToken.EndObject); //hack to set reader to end of player object
            return result;
        }

        private Emblems ReadEmblems(CustomJsonReader reader)
        {
            Emblems result = new Emblems();
            reader.ReadToProperty("emblems");
            result.x32 = reader.ReadValue("portal", reader.ReadAsString);
            result.x24 = reader.ReadValue("portal", reader.ReadAsString);
            result.x256 = reader.ReadValue("wowp", reader.ReadAsString);
            result.x64 = reader.ReadValue("portal", reader.ReadAsString);
            result.x195 = reader.ReadValue("portal", reader.ReadAsString);
            return result;
        }

#if DEBUG
        private DateTime WindowEnd;
        private int WindowSize = 1;
        private int ApiResponsesCount = 0;

        private void StartMeasurePerformanceThread()
        {
            Thread thread = new Thread(_ =>
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
            thread.Start();
        }
#endif

        private string GetApiResponse(string endpoint, string parameters)
        {
#if DEBUG
            Interlocked.Increment(ref ApiResponsesCount);
#endif
            WebRequest request = WebRequest.Create($"{BaseUri}{endpoint}{parameters}");
            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            using (Stream stream = response.GetResponseStream())
            using (StreamReader reader = new StreamReader(stream))
                return reader.ReadToEnd();
        }

        private string BuildParameterString(string fields = null, string account_id = null, string clan_id = null)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append($"?application_id={ApplicationID}");
            if (fields != null)
                builder.Append($"&fields={fields}");
            if (account_id != null)
                builder.Append($"&account_id={account_id}");
            if (clan_id != null)
                builder.Append($"&clan_id={clan_id}");

            return builder.ToString();
        }
    }
}
