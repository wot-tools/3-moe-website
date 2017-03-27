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
    class ManualTankClient
    {
        public string BaseUri { get; set; }
        public string RelativePathToPlayerIDs { get; set; }

        public static bool TryLoadTank(string path, out Tank tank)
        {
            try
            {
                using (Stream stream = new FileStream(path, FileMode.Open))
                    tank = ReadTank(stream);
                return true;
            }
            catch (FileNotFoundException)
            {
                SaveExampleTank(path);
                tank = null;
                return false;
            }
            catch (IOException)
            {
                tank = null;
                return false;
            }
        }

        public static bool TrySaveTank(string folder, int tankID, Tank tank)
        {
            try
            {
                using (Stream stream = new FileStream(Path.Combine(folder, $"{tankID}.json"), FileMode.Open))
                {
                    WriteTank(stream, tank);
                }
                return true;
            }
            catch (FileNotFoundException)
            {
                return false;
            }
            catch (IOException)
            {
                return false;
            }
        }

        private static Tank ReadTank(Stream stream)
        {
            using (StreamReader reader = new StreamReader(stream, Encoding.UTF8, true, 1024, true))
                return JsonConvert.DeserializeObject<Tank>(reader.ReadToEnd());
        }

        private static void WriteTank(Stream stream, Tank tank)
        {
            using (StreamWriter writer = new StreamWriter(stream, Encoding.UTF8, 1024, true))
            {
                writer.WriteLine("// possible nations: china, czech, france, germany, japan, sweden, uk, ussr, usa");
                writer.WriteLine("// possible vehicle_types: AT-SPG, heavyTank, lighTank, mediumTank, SPG");
                writer.Write(JsonConvert.SerializeObject(tank, Formatting.Indented));
            }
        }

        private static void SaveExampleTank(string path)
        {
            path += ".example";
            Tank tank = new Tank()
            {
                Name = "",
                ShortName = "",
                IsPremium = false,
                Nation = "",
                VehicleType = "",
                Icons = new Icons()
                {
                    Small = "",
                    Big = "",
                    Contour = ""
                },
                Tag = "",
                Tier = 0
            };

            using (Stream stream = File.Create(path))
                WriteTank(stream, tank);
        }
    }
}
