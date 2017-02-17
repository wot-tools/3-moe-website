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
        private static string ReallyUglyStaticpath = @"D:\repos\3-moe-website\mono_apps\MoeFetcher\";

        static void Main(string[] args)
        {
            Player foo = ReadPlayerMarks(GetStream(), 2);
            //TokenizeTest();
            Console.Read();
        }

        //marksOnGun
        //general problem: should read Methods stop reading when they found the value, or should they still read the complete object to set the current token position on the reader to the end of the object they read

        static Player ReadPlayerMarks(Stream jsonStream, int minMark)
        {
            Player result = new Player();
            using (StreamReader streamReader = new StreamReader(jsonStream))
            using (JsonTextReader reader = new JsonTextReader(streamReader))
            {
                if (ReadValue(reader, "status", reader.ReadAsString) != "ok")
                    throw new Exception();
                if (ReadValue(reader, "count", r => r.ReadAsInt32()).Value != 1)
                    throw new Exception();
                ReadToProperty(reader, "data");
                result.ID = int.Parse(ReadNextPropertyNameAsData(reader));
                result.Tanks = ReadTanksArray(reader, minMark).ToArray();
            }
            return result;
        }

        static IEnumerable<Tank> ReadTanksArray(JsonTextReader reader, int minMark)
        {
            Tank currentResult;
            while (true)
            {
                if ((currentResult = ReadTank(reader)) == null)
                    yield break;
                if (currentResult.Mark >= minMark)
                    yield return currentResult;
            }
        }

        static Tank ReadTank(JsonTextReader reader)
        {
            Tank result = new Tank();
            if (false == ReadToPropertyIfExisting(reader, "achievements", JsonToken.EndArray))
                return null;
            result.Mark = GetMarkFromAchievements(reader);
            result.ID = ReadValue(reader, "tank_id", r => r.ReadAsInt32()).Value;
            return result;
        }

        static T ReadValue<T>(JsonTextReader reader, string propertyName, Func<T> readValue)
        {
            return ReadValue(reader, propertyName, _ => readValue());
        }

        static T ReadValue<T>(JsonTextReader reader, string propertyName, Func<JsonTextReader, T> readValue)
        {
            while (reader.Read())
                if (reader.TokenType == JsonToken.PropertyName && reader.Value.ToString() == propertyName)
                    return readValue(reader);
            throw new KeyNotFoundException();
        }

        static int GetMarkFromAchievements(JsonTextReader reader)
        {
            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.EndObject)
                    return 0;
                if (reader.TokenType == JsonToken.PropertyName && reader.Value.ToString() == "marksOnGun")
                    return reader.ReadAsInt32() ?? 0; // TODO: throw exception here
            }
            throw new KeyNotFoundException();
        }

        static void ReadToProperty(JsonTextReader reader, string propertyName, JsonToken endToken = JsonToken.None)
        {
            if (false == ReadToPropertyIfExisting(reader, propertyName, endToken))
                throw new KeyNotFoundException();
        }

        static bool ReadToPropertyIfExisting(JsonTextReader reader, string propertyName, JsonToken endToken = JsonToken.None)
        {
            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.PropertyName && reader.Value.ToString() == propertyName)
                    return true;
                if (reader.TokenType == endToken)
                    return false;
            }
            return false;
        }

        static string ReadNextPropertyNameAsData(JsonTextReader reader)
        {
            while (reader.Read())
                if (reader.TokenType == JsonToken.PropertyName)
                    return reader.Value.ToString();
            throw new Exception();
        }

        static Stream GetStream()
        {
            return File.OpenRead(Path.Combine(ReallyUglyStaticpath, "MoeList.json"));
        }

        static void TokenizeTest()
        {
            Tokenize(GetStream(), Path.Combine(ReallyUglyStaticpath, "MoeList.parsed"));
        }

        static void Tokenize(Stream jsonStream, string savePath)
        {
            int tabCount, nextTabCount = 0;
            using (FileStream saveStream = File.Create(savePath))
            using (StreamWriter writer = new StreamWriter(saveStream))
            using (StreamReader streamReader = new StreamReader(jsonStream))
            using (JsonTextReader reader = new JsonTextReader(streamReader))
                while (reader.Read())
                {
                    tabCount = nextTabCount;
                    if (reader.TokenType == JsonToken.StartArray || reader.TokenType == JsonToken.StartObject)
                        nextTabCount += 1;
                    if (reader.TokenType == JsonToken.EndArray || reader.TokenType == JsonToken.EndObject)
                        tabCount = nextTabCount -= 1;
                    writer.WriteLine(String.Format("{0}{1} {2}", "\t".Repeat(tabCount), reader.TokenType.ToString().PadRight(20), reader.Value));
                }
        }
    }

    class Player
    {
        public int ID { get; set; }
        public Tank[] Tanks { get; set; }
    }

    class Tank
    {
        public int ID { get; set; }
        public int Mark { get; set; }
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
