using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WgApi
{
    class WGApiClient
    {
        private string StillUglyPath { get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..\\.."); } }

        //marksOnGun
        //general problem: should read Methods stop reading when they found the value, or should they still read the complete object to set the current token position on the reader to the end of the object they read

        private int ReadInfo(CustomJsonReader reader)
        {
            if (reader.ReadValue("status", reader.ReadAsString) != "ok")
                throw new Exception();
            return reader.ReadValue("count", reader.ReadAsInt32) ?? throw new Exception();
        }

        public Player GetPlayerMarks(int id, int minMark)
        {
            string path = Path.Combine(StillUglyPath, "MoeList.json");
            using (StreamReader reader = new StreamReader(path))
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

        public Player[] GetPlayerWinrateRecords()
        {
            string path = Path.Combine(StillUglyPath, "TankBattles.json");
            using (StreamReader reader = new StreamReader(path))
                return ReadPlayerWinrateRecords(new CustomJsonReader(reader));
        }

        private Player[] ReadPlayerWinrateRecords(CustomJsonReader reader)
        {
            using (reader)
            {
                if (ReadInfo(reader) < 0) throw new Exception();
                reader.ReadToProperty("data");
                return reader.ReadArray(r => ReadSinglePlayerWinrateRecords(r), _ => true).ToArray();
            }
        }

        private Player ReadSinglePlayerWinrateRecords(CustomJsonReader reader)
        {
            string id;
            if ((id = reader.ReadNextPropertyNameAsData(JsonToken.EndObject)) == null)
                return null;
            Player result = new Player();
            result.ID = int.Parse(id);
            result.WinrateRecords = reader.ReadArray(r => ReadWinrateRecord(r), _ => true).ToArray();
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

        public Player[] GetPlayerStats()
        {
            string path = Path.Combine(StillUglyPath, "AccountInfo.json");
            using (StreamReader reader = new StreamReader(path))
                return ReadPlayerStats(new CustomJsonReader(reader));
        }

        private Player[] ReadPlayerStats(CustomJsonReader reader)
        {
            using (reader)
            {
                if (ReadInfo(reader) < 0) throw new Exception();
                reader.ReadToProperty("data");
                return reader.ReadArray(r => ReadSinglePlayerStats(r), _ => true).ToArray();
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
            result.LastLogin = reader.ReadValue("logout_at", reader.ReadAsEpoch);
            reader.ReadToPropertyIfExisting("", JsonToken.EndObject); //hack to set reader to end of player object
            result.Statistics = stats;
            return result;
        }
    }
}
