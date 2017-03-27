using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WGApi;

namespace MoeFetcher
{
    class Setting
    {
        public string BaseUri { get; set; }
        public Region Region { get; set; }
        public string ApplicationID { get; set; }
        public string RelativePathToPlayerIDs { get; set; }
        public string RelativePathToManualTanksDirectory { get; set; }
        public DateTime LastRunStart { get; set; }
        public DateTime LastRunStop { get; set; }

        public static bool TryLoadSettings(string path, out Setting[] settings)
        {
            try
            {
                using (Stream stream = new FileStream(path, FileMode.Open))
                    settings = ReadSettings(stream);
                return true;
            }
            catch (FileNotFoundException)
            {
                SaveExampleSettings(path);
                settings = null;
                return true;
            }
            catch (IOException)
            {
                settings = null;
                return false;
            }
        }

        public static bool TrySaveSettings(string path, Setting[] settings, int activeSetting)
        {
            try
            {
                using (Stream stream = new FileStream(path, FileMode.Open))
                {
                    Setting[] oldSettings = ReadSettings(stream);
                    oldSettings[activeSetting] = settings[activeSetting];
                    stream.Position = 0;
                    WriteSettings(stream, oldSettings);
                }
                return true;
            }
            catch (FileNotFoundException)
            {
                return true;
            }
            catch (IOException)
            {
                return false;
            }
        }

        private static Setting[] ReadSettings(Stream stream)
        {
            using (StreamReader reader = new StreamReader(stream, Encoding.UTF8, true, 1024, true))
                return JsonConvert.DeserializeObject<Setting[]>(reader.ReadToEnd());
        }

        private static void WriteSettings(Stream stream, Setting[] settings)
        {
            using (StreamWriter writer = new StreamWriter(stream, Encoding.UTF8, 1024, true))
            {
                writer.WriteLine("//WARNING: editing this file while the program is running may have unexpected results");
                writer.WriteLine($"//possible regions: {String.Join(", ", (Enum.GetValues(typeof(Region)) as Region[]).Select(r => $"{r.ToString()}: {(int)r}"))}");
                writer.Write(JsonConvert.SerializeObject(settings, Formatting.Indented));
            }
        }

        private static void SaveExampleSettings(string path)
        {
            path += ".example";
            Setting[] settings =
            {
                new Setting
                {
                    BaseUri = "https://api.worldoftanks",
                    Region = Region.eu,
                    ApplicationID = "appid1234",
                    RelativePathToPlayerIDs = "",
                    RelativePathToManualTanksDirectory = "",
                    LastRunStart = new DateTime(2017, 1, 1, 13, 37, 00),
                    LastRunStop = new DateTime(2017, 1, 3, 15, 12, 53)
                },
                new Setting
                {
                    BaseUri = "https://api.worldoftanks",
                    Region = Region.ru,
                    ApplicationID = "appid1234",
                    RelativePathToPlayerIDs = "",
                    RelativePathToManualTanksDirectory = "",
                    LastRunStart = new DateTime(2017, 1, 1, 13, 37, 00),
                    LastRunStop = new DateTime(2017, 1, 3, 15, 12, 53)
                },
                new Setting
                {
                    BaseUri = "https://api.worldoftanks",
                    Region = Region.com,
                    ApplicationID = "appid1234",
                    RelativePathToPlayerIDs = "",
                    RelativePathToManualTanksDirectory = "",
                    LastRunStart = new DateTime(2017, 1, 1, 13, 37, 00),
                    LastRunStop = new DateTime(2017, 1, 3, 15, 12, 53)
                },
                new Setting
                {
                    BaseUri = "https://api.worldoftanks",
                    Region = Region.asia,
                    ApplicationID = "appid1234",
                    RelativePathToPlayerIDs = "",
                    RelativePathToManualTanksDirectory = "",
                    LastRunStart = new DateTime(2017, 1, 1, 13, 37, 00),
                    LastRunStop = new DateTime(2017, 1, 3, 15, 12, 53)
                },
            };

            using (Stream stream = File.Create(path))
                WriteSettings(stream, settings);
        }
    }
}
