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
        private static string StillUglyPath { get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..\\.."); } }

        static void Main(string[] args)
        {
            Player foo = GetPlayerMarks(-1, 2);
            Player[] bar = GetPlayerWinrateRecords();
            //Tokenize("TankBattles");
            Console.Read();
        }

        //marksOnGun
        //general problem: should read Methods stop reading when they found the value, or should they still read the complete object to set the current token position on the reader to the end of the object they read

        static Player GetPlayerMarks(int id, int minMark)
        {
            string path = Path.Combine(StillUglyPath, "MoeList.json");
            using (StreamReader reader = new StreamReader(path))
                return ReadPlayerMarks(new CustomJsonReader(reader), minMark);
        }

        static Player ReadPlayerMarks(CustomJsonReader reader, int minMark)
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

        static Moe ReadTank(CustomJsonReader reader)
        {
            if (false == reader.ReadToPropertyIfExisting("achievements", JsonToken.EndArray))
                return null;
            Moe result = new Moe();
            result.Mark = reader.ReadValue("marksOnGun", reader.ReadAsInt32, JsonToken.EndObject) ?? 0;
            result.TankID = reader.ReadValue("tank_id", reader.ReadAsInt32).Value;
            return result;
        }

        static Player[] GetPlayerWinrateRecords()
        {
            string path = Path.Combine(StillUglyPath, "TankBattles.json");
            using (StreamReader reader = new StreamReader(path))
                return ReadPlayerWinrateRecords(new CustomJsonReader(reader));
        }

        static Player[] ReadPlayerWinrateRecords(CustomJsonReader reader)
        {
            using (reader)
            {
                if (ReadInfo(reader) < 0) throw new Exception();
                reader.ReadToProperty("data");
                return reader.ReadArray(r => ReadSinglePlayerWinrateRecords(r), _ => true).ToArray();
            }
        }

        static Player ReadSinglePlayerWinrateRecords(CustomJsonReader reader)
        {
            string id;
            if ((id = reader.ReadNextPropertyNameAsData(JsonToken.EndObject)) == null)
                return null;
            Player result = new Player();
            result.ID = int.Parse(id);
            result.WinrateRecords = reader.ReadArray(r => ReadWinrateRecord(r), _ => true).ToArray();
            return result;
        }

        static WinrateRecord ReadWinrateRecord(CustomJsonReader reader)
        {
            if (false == reader.ReadToPropertyIfExisting("statistics", JsonToken.EndArray))
                return null;
            WinrateRecord result = new WinrateRecord();
            result.Victories = reader.ReadValue("wins", reader.ReadAsInt32).Value;
            result.Battles = reader.ReadValue("battles", reader.ReadAsInt32).Value;
            result.TankID = reader.ReadValue("tank_id", reader.ReadAsInt32).Value;
            return result;
        }

        static int ReadInfo(CustomJsonReader reader)
        {
            if (reader.ReadValue("status", reader.ReadAsString) != "ok")
                throw new Exception();
            return reader.ReadValue("count", reader.ReadAsInt32) ?? -1; //TODO: throw exception here
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

    class CustomJsonReader : JsonTextReader
    {
        public CustomJsonReader(TextReader reader) : base(reader) { }

        //relies on getElement returning null after the last element
        public IEnumerable<T> ReadArray<T>(Func<CustomJsonReader, T> getElement, Func<T, bool> includeElement)
        {
            T currentResult;
            LOOP:
            if ((currentResult = getElement(this)) == null)
                yield break;
            if (includeElement(currentResult))
                yield return currentResult;
            goto LOOP;
        }

        public T ReadValue<T>(string propertyName, Func<T> readValue, JsonToken endToken = JsonToken.None)
        {
            while (Read())
            {
                if (TokenType == endToken)
                    return default(T);
                if (TokenType == JsonToken.PropertyName && Value.ToString() == propertyName)
                    return readValue();
            }
            throw new KeyNotFoundException();
        }

        public void ReadToProperty(string propertyName, JsonToken endToken = JsonToken.None)
        {
            if (false == ReadToPropertyIfExisting(propertyName, endToken))
                throw new KeyNotFoundException();
        }

        public bool ReadToPropertyIfExisting(string propertyName, JsonToken endToken = JsonToken.None)
        {
            while (Read())
            {
                if (TokenType == JsonToken.PropertyName && Value.ToString() == propertyName)
                    return true;
                if (TokenType == endToken)
                    return false;
            }
            return false;
        }

        public string ReadNextPropertyNameAsData(JsonToken endToken = JsonToken.None)
        {
            while (Read())
            {
                if (TokenType == endToken)
                    return null;
                if (TokenType == JsonToken.PropertyName)
                    return Value.ToString();
            }
            throw new Exception();
        }
    }

    class Player
    {
        public int ID { get; set; }
        public int ClanID { get; set; }
        public string Nick { get; set; }
        public string ClientLanguage { get; set; }
        public DateTime LastLogin { get; set; }
        public DateTime AccountCreated { get; set; }
        public DateTime LastBattle { get; set; }
        public DateTime UpdatedAt { get; set; }
        public Moe[] Moes { get; set; }
        public WinrateRecord[] WinrateRecords { get; set; }
    }

    class Moe
    {
        public int TankID { get; set; }
        public int Mark { get; set; }
    }

    class WinrateRecord
    {
        public int TankID { get; set; }
        public int Battles { get; set; }
        public int Victories { get; set; }
    }

    class Statistics
    {
        public int Battles { get; set; }
        public int Victories { get; set; }
        public int Damage { get; set; }
        public int Decap { get; set; }
        public int Cap { get; set; }
        public int Kills { get; set; }
        public int Spots { get; set; }
        public int WGRating { get; set; }
    }

    enum Status { ok }

    static class StringExtensions
    {
        public static string Repeat(this string s, int count)
        {
            return String.Join("", Enumerable.Repeat(s, count));
        }
    }
}
