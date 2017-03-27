using WGApi;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MoeFetcher
{
    class App
    {
        private const int SettingsAccessTryCount = 25;
        private const int TimeoutAfterFailedLoadTry = 200; //ms
        private const int MaxIDCountForApi = 100;
        private const int MinMark = 3;

        private string BasePath;
        private string SettingsPath { get { return Path.Combine(BasePath, "settings.json"); } }

        private WGApiClient Client;
        private DBClient DatabaseClient;
        private Setting[] Settings;
        private Setting ActiveSetting;
        private int SettingIndex;
        private ILogger Logger;
        private DateTime RunStart;

        private Dictionary<string, string> Arguments = new Dictionary<string, string>();
        private HashSet<int> ClanIDs = new HashSet<int>();
        private HashSet<int> PlayerIDs = new HashSet<int>();


        //TODO: mit neuer tuple syntax schreiben
        private Dictionary<string, Tuple<string, string>> ArgCombinations = new Dictionary<string, Tuple<string, string>>
        {
            ["h"] = new Tuple<string, string>("help", null),
            ["s"] = new Tuple<string, string>("active-setting", "0"),
        };

        public App(ILogger logger)
        {
            Logger = logger;
        }

        public int Run(string[] args)
        {
            if (false == ParseArgs(args))
                return (int)ExitCodes.Error;
            SetDefaultArgs();
            if (Arguments.ContainsKey("help") || Arguments.ContainsKey("h"))
            {
                DisplayHelp();
                return (int)ExitCodes.Success;
            }

            if (false == InitializeClient())
                return (int)ExitCodes.Error;

            RunStart = DateTime.Now;

            ExpectedValueList expectedValueList = new ExpectedValueList();

            TankopediaInfo Tankopedia = Client.GetTankopediaInformation();

            DatabaseClient.UpsertNations(Tankopedia.Nations);
            DatabaseClient.UpsertVehicleTypes(Tankopedia.VehicleTypes);
            Dictionary<int, Tank> tankDict = Client.GetVehicles();
            DatabaseClient.UpsertTanks(tankDict);

            RecheckPlayers(GetPlayersToRecheck(ProcessIDs(ReadLines(Path.Combine(BasePath, ActiveSetting.RelativePathToPlayerIDs)))));
            CheckClans(ProcessIDs(ClanIDs));

            CalculateWN8ForPlayers(expectedValueList);

            DatabaseClient.UpsertPlayers(TempPlayers);


            Terminate();
            return (int)ExitCodes.Success;
       }

        private void CalculateWN8ForPlayers(ExpectedValueList expectedValueList)
        {
            foreach(var player in TempPlayers)
            {
                // does not handle tanks without expected values properly
                // potentially inflating wn8
                // chosen because it needs less api requests
                player.WN8 = (int)Math.Floor(WN8.AccountWN8Newest(expectedValueList, player.Winrates, player.PlayerInfo.Statistics.Random));
            }
        }

        private List<Player> TempPlayers = new List<Player>();

        private void DumpPlayersIntoList(IEnumerable<Player[]> playerBatches)
        {
            foreach (Player[] playerBatch in playerBatches)
                TempPlayers.AddRange(playerBatch);
        }

        private bool ParseArgs(string[] args)
        {
            Stack<string> arguments = new Stack<string>(args.Reverse());

            while (arguments.Count > 0)
            {
                string argument = arguments.Pop();
                //put combined single-char arguments on stack seperated (-av => -a -v)
                if (argument.Length > 2 && argument[0] == '-' && argument[1] != '-')
                {
                    foreach (string arg in argument.Skip(1).Select(c => " - " + c).Reverse())
                        arguments.Push(arg);
                    continue;
                }

                string value = null;
                if (arguments.Count != 0 && arguments.Peek().First() != '-')
                    value = arguments.Pop();

                string trimmedArg = argument.TrimStart('-');
                if (false == ArgCombinations.Any(p => p.Key == trimmedArg || p.Value.Item1 == trimmedArg))
                {
                    Logger.CriticalError("unrecognized argument: {0}", argument);
                    return false;
                }

                Arguments.Add(trimmedArg, value ?? "true");
            }
            return true;
        }

        private void SetDefaultArgs()
        {
            foreach (var kvp in ArgCombinations)
            {
                if (Arguments.TryGetValue(kvp.Key, out string value))
                {
                    Arguments.Remove(kvp.Key);
                    Arguments.Add(kvp.Value.Item1, value);
                }
                else if (false == Arguments.ContainsKey(kvp.Value.Item1) && kvp.Value.Item2 != null)
                {
                    Arguments.Add(kvp.Value.Item1, kvp.Value.Item2);
                }
            }
        }

        private void DisplayHelp()
        {

        }

        private bool InitializeClient()
        {
            int remainingLoadTries = SettingsAccessTryCount;
            Uri baseUri = new Uri(System.Reflection.Assembly.GetExecutingAssembly().CodeBase);
            BasePath = System.Net.WebUtility.UrlDecode(Path.GetDirectoryName(baseUri.AbsolutePath));

            while (false == Setting.TryLoadSettings(SettingsPath, out Settings))
            {
                if (remainingLoadTries-- > 0)
                    Thread.Sleep(TimeoutAfterFailedLoadTry);
                else
                {
                    Logger.CriticalError("could not open settings at {0}", SettingsPath);
                    return false;
                }
            }
            if (Settings == null)
            {
                Logger.CriticalError("could not find settings at {0}. see settings.json.example", SettingsPath);
                return false;
            }

            Logger.Info("settings loaded from {0}", SettingsPath);

            if (false == int.TryParse(Arguments["active-setting"], out SettingIndex))
            {
                Logger.CriticalError("could not parse active-setting \"{0}\" as integer", Arguments["active-setting"]);
                return false;
            }
            if (SettingIndex < 0 || SettingIndex >= Settings.Length)
            {
                Logger.CriticalError("setting index {0} is out of range", SettingIndex);
                return false;
            }
            ActiveSetting = Settings[SettingIndex];
            Client = new WGApiClient(ActiveSetting.BaseUri, ActiveSetting.Region, ActiveSetting.ApplicationID, Logger);
            DatabaseClient = new DBClient("localhost", "root", "root", "moe", Logger);
            Logger.Info("app set to region {0}", ActiveSetting.Region);
            return true;
        }

        private void Terminate()
        {
            ActiveSetting.LastRunStart = RunStart;
            ActiveSetting.LastRunStop = DateTime.Now;
            int remainingSaveTries = SettingsAccessTryCount;
            while (false == Setting.TrySaveSettings(SettingsPath, Settings, SettingIndex))
            {
                if (remainingSaveTries-- > 0)
                    Thread.Sleep(TimeoutAfterFailedLoadTry);
                else
                {
                    Logger.Error("could not open settings for saving at {0}", SettingsPath);
                    Logger.Info("app closed without saving settings");
                }
            }
        }

        private IEnumerable<int> ReadLines(string path)
        {
            int i = 1010;
            int id;
            string line;
            using (Stream stream = new FileStream(path, FileMode.Open))
            using (StreamReader reader = new StreamReader(stream))
                while (i-- > 0 && (line = reader.ReadLine()) != null)
                    if (int.TryParse(line, out id))
                        PlayerIDs.Add(id);
            return PlayerIDs;
        }

        private IEnumerable<int[]> ProcessIDs(IEnumerable<int> ids)
        {
            int index = 0;
            int maxCount = MaxIDCountForApi;
            int[] result = new int[maxCount];

            foreach (int id in ids)
            {
                result[index++] = id;
                if (index >= maxCount)
                {
                    yield return result;
                    index = 0;
                    result = new int[maxCount];
                }
            }

            if (index == 0)
                yield break;
            int[] lastResult = new int[index];
            Array.Copy(result, lastResult, index);
            yield return lastResult;
        }

        private IEnumerable<Player[]> GetPlayersToRecheck(IEnumerable<int[]> idBatches)
        {
            DateTime lastRun = ActiveSetting.LastRunStart;
            int index = 0;
            int maxCount = MaxIDCountForApi;
            Player[] result = new Player[maxCount];

            foreach (int[] idBatch in idBatches)
            {
                foreach (var player in Client.GetPlayerStats(idBatch))
                {
                    if (null == player.Value)
                    {
                        Logger.Error("Received null from server for player id {0}", player.Key);
                        continue;
                    }

                    if (player.Value.LastBattle <= lastRun)
                        continue;
                    result[index++] = new Player
                    {
                        ID = player.Key,
                        PlayerInfo = player.Value,
                    };
                    if (index >= maxCount)
                    {
                        yield return result;
                        index = 0;
                        result = new Player[maxCount];
                    }
                }
            }

            if (index == 0)
                yield break;
            Player[] lastResult = new Player[index];
            Array.Copy(result, lastResult, index);
            yield return lastResult;
        }

        private void RecheckPlayers(IEnumerable<Player[]> playerBatches)
        {
            Stopwatch watch = Stopwatch.StartNew();
            int minMark = MinMark;
            int threadcount = 0;
            using (ManualResetEvent resetEvent = new ManualResetEvent(false))
            {
                foreach (Player[] players in playerBatches)
                {

                    ThreadPool.QueueUserWorkItem(_ =>
                    {
                        Interlocked.Increment(ref threadcount);
                        var winrateRecords = Client.GetPlayerWinrateRecords(players.Select(p => p.ID));
                        for (int i = 0; i < players.Length; i++)
                        {
                            players[i].Winrates = winrateRecords[players[i].ID];
                            players[i].Moes = Client.GetPlayerMarks(players[i].ID).Where(x => x.Mark >= minMark).ToArray();
                            if (players[i].PlayerInfo.ClanID.HasValue && players[i].Moes.Length > 0)
                                ClanIDs.Add(players[i].PlayerInfo.ClanID.Value);
                        }
                        lock (TempPlayers)
                            TempPlayers.AddRange(players);
                        if (Interlocked.Decrement(ref threadcount) == 0)
                            resetEvent.Set();
                    });
                }
                resetEvent.WaitOne();
            }
            watch.Stop();
            Logger.Info(watch.Elapsed.ToString());
        }

        private void CheckClans(IEnumerable<int[]> clanIDBatches)
        {
            Stopwatch watch = Stopwatch.StartNew();
            int threadcount = 0;
            using (ManualResetEvent resetEvent = new ManualResetEvent(false))
            {
                foreach (int[] clans in clanIDBatches)
                {
                    ThreadPool.QueueUserWorkItem(_ =>
                    {
                        Interlocked.Increment(ref threadcount);
                        DatabaseClient.UpsertClans(Client.GetClanInformation(clans));
                        if (Interlocked.Decrement(ref threadcount) == 0)
                            resetEvent.Set();
                    });
                }
                resetEvent.WaitOne();
            }
            watch.Stop();
            Logger.Info(watch.Elapsed.ToString());
        }
    }
}
