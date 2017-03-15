using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace WGApi
{
    public class ExpectedValueList
    {
        private string LinkFormat = @"http://www.wnefficiency.net/exp/expected_tank_values_{0}.json";

        private string _Directory;
        private string Directory
        {
            get
            {
                if (_Directory == null)
                {
                    Uri baseUri = new Uri(System.Reflection.Assembly.GetExecutingAssembly().CodeBase);
                    _Directory = System.Net.WebUtility.UrlDecode(Path.GetDirectoryName(baseUri.AbsolutePath));
                }
                return _Directory;
            }
        }

        private string SaveFilePath { get { return Path.Combine(Directory, "expectedValues.json"); } }

        private Dictionary<int, Dictionary<int, ExpectedValues>> Values;

        public Dictionary<int, ExpectedValues> this[int i] { get { return Values[i]; } }
        public IEnumerable<int> Versions { get { return Values.Select(p => p.Key); } }

        public ExpectedValueList()
        {
            Initialize();
        }

        private void Initialize()
        {
            if (Open(SaveFilePath))
                AddNewValues(Versions.Max());
            else
            {
                Values = new Dictionary<int, Dictionary<int, ExpectedValues>>();
                AddNewValues(-1, 29);
            }
            Save(SaveFilePath);
        }

        private bool Open(string path)
        {
            try
            {
                using (Stream stream = File.OpenRead(path))
                using (StreamReader reader = new StreamReader(stream))
                    Values = JsonConvert.DeserializeObject<Dictionary<int, Dictionary<int, ExpectedValues>>>(reader.ReadToEnd());
                return true;
            }
            catch (FileNotFoundException)

            {
                return false;
            }
        }

        private void Save(string path)
        {
            using (Stream stream = File.Create(path))
            using (StreamWriter writer = new StreamWriter(stream))
                writer.Write(JsonConvert.SerializeObject(Values));
        }

        private void AddNewValues(int latestVersion, int checkAtLeastTo = 0)
        {
            int version = latestVersion + 1;
            while (TryAddNewValues(version) || checkAtLeastTo >= version)
                version += 1;
        }

        private bool TryAddNewValues(int version)
        {
            ApiWrapper<ExpectedValues[]> apiObject;
            try
            {
                apiObject = JsonConvert.DeserializeObject<ApiWrapper<ExpectedValues[]>>(GetApiResponse(version));
            }
            catch
            {
                return false;
            }
            if (apiObject.Header.Version != version)
                throw new Exception("expected-value-version is not the requested version");


            Values.Add(version, apiObject.Data.ToDictionary(v => v.TankID));
            return true;
        }

        private string GetApiResponse(int version)
        {
            WebRequest request = WebRequest.Create(String.Format(LinkFormat, version));
            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            using (Stream stream = response.GetResponseStream())
            using (StreamReader reader = new StreamReader(stream))
                return reader.ReadToEnd();
        }


        private class ApiWrapper<T>
        {
            [JsonProperty("header")]
            public Header Header { get; set; }
            [JsonProperty("data")]
            public T Data { get; set; }
        }

        private class Header
        {
            [JsonProperty("version")]
            public int Version { get; set; }
        }
    }
}
