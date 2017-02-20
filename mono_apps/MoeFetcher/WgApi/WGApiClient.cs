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

        private int ReadInfo(CustomJsonReader reader)
        {
            if (reader.ReadValue("status", reader.ReadAsString) != "ok")
                throw new Exception();
            return reader.ReadValue("count", reader.ReadAsInt32) ?? throw new Exception();
        }
    }
}
