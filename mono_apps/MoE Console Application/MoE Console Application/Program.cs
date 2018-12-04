using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Threading;
using System.Xml.Serialization;
using System.Diagnostics;
using System.Collections.Concurrent;
using MySql.Data.MySqlClient;

namespace MoE_Console_Application
{
    class Program
    {
        List<MoETank> MoETanks = new List<MoETank>();
        List<string> checkedIDs = new List<string>();
        List<string> failedIDs = new List<string>();
        List<string> playerIDsToCheck = new List<string>();
        List<Player> Players = new List<Player>();
        List<Clan> Clans = new List<Clan>();
        List<ExpectedValueItem> ExpectedValues = new List<ExpectedValueItem>();
        List<string> currentlyCheckingIDs = new List<string>();
        List<string> WN8SkippedTanksIDs = new List<string>();
        Dictionary<string, WebClientInfoItem> WebClientDict = new Dictionary<string, WebClientInfoItem>();
        ConcurrentDictionary<string, List<double>> currentlyCheckingIDLists = new ConcurrentDictionary<string, List<double>>();
        Dictionary<string, List<double>> failedIDLists = new Dictionary<string, List<double>>();

        MoELog Log = new MoELog(true, 3);

        string currentServerID = "";
        string scriptVersion = "420.docker.db.test";
        int runningAsyncs = 0;
        int processingRequests = 0;
        int runningAsyncsMax = 20;
        int currentRequests = 0;
        int totalRequests = 0;
        int logLevel = 3;
        bool isSavingData = false;

        static void Main(string[] args)
        {
            Program p = new Program();
            p.Log = new MoELog(true, p.logLevel);

            if (args.Length < 2) // not enough arguments
            {
                p.HandleStartUpArgumentError();
            }
            else
            {
                string serverID = args[0];
                string appMode = args[1];
                                
                p.HandeStartUp(serverID, appMode);

                double startID = p.GetStartIDByServer(serverID);
                double endID = p.GetEndIDByServer(serverID);

                if (args.Length == 3)
                    endID = Convert.ToDouble(args[2]);
                else if (args.Length == 4)
                {
                    startID = Convert.ToDouble(args[2]);
                    endID = Convert.ToDouble(args[3]);
                }
                //try
                //{
                if (appMode == MoEStatic.appModeMoE || appMode == MoEStatic.appModeMoERetry)
                {
                    bool retry = appMode == MoEStatic.appModeMoERetry;
                    p.HandleInitialDataBuilding(serverID, "en", retry);
                    p.HandleMoEGathering(serverID, p.GetAPIKeyFromSuffix(serverID), startID, endID, retry);
                }
                else if (appMode == MoEStatic.appModePlayer || appMode == MoEStatic.appModePlayerRetry || appMode == MoEStatic.appModePlayerFailed)
                {
                    bool retry = appMode == MoEStatic.appModePlayerRetry;
                    bool failedMode = appMode == MoEStatic.appModePlayerFailed;


                    p.HandlePlayerAndClanInfo(serverID, p.GetAPIKeyFromSuffix(serverID), retry, failedMode);
                }
                else if (appMode == MoEStatic.appModePlayerIDs)
                {
                    IDCheckingHandler idHandler = new IDCheckingHandler(serverID, p.GetAPIKeyFromSuffix(serverID), startID, endID, p.Log);
                    idHandler.CheckAndWriteToDatabase();
                    //p.HandlePlayerIDListGathering(serverID, p.GetAPIKeyFromSuffix(serverID), startID, endID);
                }
                else if (appMode == MoEStatic.appModeUpdateTankInformation)
                {
                    p.HandleTankInformationUpdate(serverID, p.GetAPIKeyFromSuffix(serverID), "en", false);
                    p.HandleTankInformationUpdate(serverID, p.GetAPIKeyFromSuffix(serverID), "en", true);
                }
                else if (appMode == MoEStatic.appModeDataBase)
                {
                    p.HandleDataBaseTest(serverID, p.GetAPIKeyFromSuffix(serverID));
                }
                else if (appMode == MoEStatic.appModeFixing)
                {
                    p.HandleFixingStuff(serverID, p.GetAPIKeyFromSuffix(serverID));
                }
                else if (appMode == MoEStatic.appModeIDDoubleChecking)
                {
                    p.HandeDoubleIDChecking(serverID);
                }
                else if(appMode == MoEStatic.appModeDockerDataBase)
                {
                    p.HandleDockerDataBaseTest(serverID);
                }
                else
                {

                }
                //}
                //catch (Exception excp)
                //{
                //    p.HandleErrorInMain("SOMETHING SERIOUSLY GOT FUCKED UP!!!!", excp);
                //}
            }

            p.HandleShutDown();
        }

        private void HandleStartUpArgumentError()
        {
            Log.AddError("Not enough startup arguments you fuking kent");
        }

        private void HandeStartUp(string serverID, string appMode)
        {
            Log.AddResult($"App started with server ID {serverID}, app Mode {appMode} and current version: {scriptVersion}.");
        }
        private void HandleShutDown()
        {
            Log.AddInfo("Shutting App down");
            Log.AddInfo("Writing log file");
            Log.WriteLog();
            Log.AddInfo("Finished shutting down");
        }
        private void HandleErrorInMain(string line, Exception excp)
        {
            Log.AddError(line, excp);
        }

        private void HandleInitialDataBuilding(string serverID, string languageID, bool retry)
        {
            if (!retry)
            {
                Log.AddResult("Getting Tank List from API");
                List<Tank> Tanks = GetTankListFromAPI(serverID, GetAPIKeyFromSuffix(serverID),languageID);

                MoETanks = new List<MoETank>();

                foreach (Tank tank in Tanks)
                {
                    MoETank moeTank = new MoETank();
                    moeTank.Tank = tank;
                    MoETanks.Add(moeTank);
                }
            }
            else
            {
                Log.AddResult($"Loading MoE Tank list from file ({GetMoETanksFileName(serverID)}) for retrying failed ids.");
                MoETanks = ReadObjectFromFile<List<MoETank>>(GetMoETanksFileName(serverID));
            }
        }

        #region id related stuff
        private bool FilterIDsByServer(string server, string id)
        {
            double d_ID = Convert.ToDouble(id);
            switch (server)
            {
                case "RU":
                    return d_ID >= 1 && d_ID <= 499999999;
                case "EU":
                    return d_ID >= 500000000 && d_ID <= 999999999;
                case "NA":
                case "US":
                    return d_ID >= 1000000000 && d_ID <= 1499999999;
                case "ASIA":
                    return d_ID >= 2000000000 && d_ID <= 2499999999;
                case "VTC":
                    return d_ID >= 2500000000 && d_ID <= 2999999999;
                case "KR":
                    return d_ID >= 3000000000 && d_ID <= 3499999999;
            }
            return false;
        }
        private double GetStartIDByServer(string server)
        {
            switch (server)
            {
                case "RU":
                    return 1;
                case "EU":
                    return 500000000;
                case "NA":
                case "US":
                    return 1000000000;
                case "ASIA":
                    return 2000000000;
                case "VTC":
                    return 2500000000;
                case "KR":
                    return 3000000000;
            }
            return -1;
        }
        private double GetEndIDByServer(string server)
        {
            switch (server)
            {
                case "RU":
                    return 499999999;
                case "EU":
                    return 999999999;
                case "NA":
                case "US":
                    return 1499999999;
                case "ASIA":
                    return 2499999999;
                case "VTC":
                    return 2999999999;
                case "KR":
                    return 3499999999;
            }
            return -1;
        }
        #endregion

        #region testing and fixing stuff
        private void HandleFixingStuff(string serverID, string appID)
        {
            string ids = "16815503;30332033;;30356753;35520224;35545556;35623701;;;102853;143227;14793;212693;;9733;950;81339;92911;74368;185866;92192;183267;175005;162483;16815503;30332033;30356753;35520224;35545556;35623701;24021075;24021291;24021473;24021933;24023025;24023178;24023224;24023264;24023360;24023452;24024118;24024205;24024287;24024439;24024745;24025510;24025634;24025831;24025943;24026764;24026807;24026908;24027279;24028218;24028469;24028569;24029458;24030511;24030743;24030752;24030762;24030813;24031000;24031357;24032221;24032849;24032897;24033100;24034503;24034514;24035200;24035350;24035464;24035892;24036772;24037732;24038234;24038419;24038450;24039111;24039126;24039336;24039906;24040502;24040981;24041332;24042035;24042697;24042845;24042925;24043551;24044182;24044445;24045855;24045873;24046640;24046666;24046792;24046916;24047134;24047412;24047679;24048549;24048873;24049051;24049499;24049980;24050092;24050515;24050700;24050780;24051104;24052002;24052244;24052305;24052659;24053075;24053901;24053983;24054552;24055351;24055561;24055921;24055929;24056015;24057279;24057510;24057861;24058058;24058079";

            List<string> skippedIDs = ids.Split(';').Distinct().Where(x => !String.IsNullOrEmpty(x)).ToList();

            SaveObjectToJsonFile(skippedIDs, GetSkippedPlayerIDListFileName(serverID));
        }
        #endregion

        #region get moe data
        private void HandleMoEGathering(string serverID, string appID, bool retry)
        {
            HandleMoEGathering(serverID, appID, 0, 0, retry);
        }
        private void HandleMoEGathering(string serverID, string appID, double startIDval, double endIDval, bool retry)
        {
            List<string> ids = new List<string>();
            Stopwatch stopWatch = new Stopwatch();
            WebClientDict = new Dictionary<string, WebClientInfoItem>();
            currentlyCheckingIDs = new List<string>();

            if (!retry)
            {
                Log.AddResult("Regular MoE checking run");
                StreamReader sr = new StreamReader(GetIDListFileName(serverID));

                Log.AddResult("Reading IDs from file: " + GetIDListFileName(serverID));

                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    ids.Add(line);
                }
                sr.Close();

                Log.AddResult("Making id list distinct and removing empty/null strings");
                ids = ids.Distinct().Where(x => !String.IsNullOrEmpty(x)).ToList();
            }
            else
            {
                Log.AddResult("Manual MoE checking run to check failed IDs and IDs without answer");
                Log.AddResult("Generating ID list to recheck");

                Log.AddResult($"Loading failed IDs from file: {GetMoEFailedPlayersFileName(serverID)}");
                List<string> failedIDs = ReadObjectFromFile<List<string>>(GetMoEFailedPlayersFileName(serverID));

                Log.AddResult($"Loading IDs without answer from file: {GetMoEPlayersWithoutAnswerFileName(serverID)}");
                List<string> noAnswerIDs = ReadObjectFromFile<List<string>>(GetMoEPlayersWithoutAnswerFileName(serverID));

                failedIDs.ForEach(x => ids.Add(x));
                noAnswerIDs.ForEach(x => ids.Add(x));

                ids = ids.Distinct().Where(x => !String.IsNullOrEmpty(x)).ToList();

                Log.AddResult($"Loading already checked player IDs from file: {GetPlayerIDListFileName(serverID)}");
                playerIDsToCheck = ReadObjectFromFile<List<string>>(GetPlayerIDListFileName(serverID));
            }

            if (startIDval > 0)
            {
                ids = ids.Where(x => Convert.ToDouble(x) >= startIDval).ToList();
                Log.AddResult($"Getting MoE from players starting at ID {startIDval}");
            }
            else
                Log.AddResult("Getting MoE without startID");

            if (endIDval > 0)
            {
                ids = ids.Where(x => Convert.ToDouble(x) <= endIDval).ToList();
                Log.AddResult($"Getting MoE from players up to ID {endIDval}");
            }
            else
                Log.AddResult("Getting MoE without endID");

            runningAsyncs = 0;
            currentRequests = 0;
            totalRequests = ids.Count;
            Log.AddResult($"About to send {totalRequests} requests with max {runningAsyncsMax} running parallel");
            checkedIDs = new List<string>();
            failedIDs = new List<string>();
            
            string requesturl = @"https://api.worldoftanks.{2}/wot/tanks/achievements/?application_id={0}&fields=achievements%2C%20tank_id&account_id={1}";

            Log.AddResult("Starting to do async requests for players' moes");

            foreach (string id in ids)
            {
                stopWatch.Restart();
                while (runningAsyncs >= runningAsyncsMax)
                {
                    Thread.Sleep(20);

                    if (stopWatch.ElapsedMilliseconds > MoEStatic.MaxMillisecondRunTimeOfRequest)
                        CheckWebClientsMaxRuntime();


                    Log.AddInfo($"Currently waiting for {stopWatch.ElapsedMilliseconds} ms for a request to finish");

                    #region save data in between
                    if (MoEStatic.SaveInBetween && currentRequests % MoEStatic.SaveInterval == 0 && currentRequests > 0)
                    {
                        // wait for all requests to finish and then save the data
                        Log.AddInfo($"Reached save interval ({MoEStatic.SaveInterval}) with {currentRequests} requests, will wait for all requests to finish");

                        CheckWebClientsMaxRuntime();

                        Stopwatch idleStopWatch = Stopwatch.StartNew();
                        while (runningAsyncs > 0)
                        {
                            Thread.Sleep(1000);
                            Log.AddInfo($"Waited 1s, {runningAsyncs} asyncs still running");
                            Log.AddInfo($"Still running: {String.Join(";", currentlyCheckingIDs)}");

                            idleStopWatch.Stop();

                            if (idleStopWatch.ElapsedMilliseconds > MoEStatic.MaxMillisecondRunTimeOfRequest)
                            {
                                Log.AddInfo($"Waited long enough ({idleStopWatch.ElapsedMilliseconds} ms with {MoEStatic.MaxMillisecondRunTimeOfRequest} ms) for {runningAsyncs} asyncs to finish");
                                CheckWebClientsMaxRuntime();
                                idleStopWatch.Reset();
                            }

                            idleStopWatch.Start();
                        }

                        idleStopWatch.Stop();

                        Log.AddInfo($"No async request is running currently");
                        Log.AddInfo("Clearing webclient dict");
                        WebClientDict.Clear();
                        Log.AddInfo($"Saving data of {currentRequests} requests");
                        HandleMoEDataSaving(serverID, retry);
                        Log.AddInfo("Saved data at saving interval, resuming downloading");
                    }
                    #endregion
                }
                stopWatch.Stop();

                if (stopWatch.ElapsedMilliseconds > 0)
                    Log.AddInfo(String.Format("Waited {0} ms for an request to get back", stopWatch.ElapsedMilliseconds));

                if (currentRequests % MoEStatic.ClearInterval == 0 && currentRequests > 0)
                    ClearWebClientsDictFromFinishedItems();

                WebClient client = new WebClient();
                client.Encoding = Encoding.UTF8;
                client.Proxy = null;
                client.DownloadStringCompleted += Client_DownloadStringCompleted;
                                
                currentlyCheckingIDs.Add(id);
                WebClientDict.Add(id, new WebClientInfoItem(client));
                Log.AddInfo(String.Format("Sending request ({0}/{1}) to API: ", runningAsyncs + 1, runningAsyncsMax) + String.Format(requesturl, appID, id, GetAPISuffix(serverID)));
                client.DownloadStringAsync(new Uri(String.Format(requesturl, appID, id, GetAPISuffix(serverID))), id);
                runningAsyncs++;
            }

            //Log.AddInfo("Waiting for requests to finish");

            #region wait for stuff to finish
            Stopwatch waitingWatch = Stopwatch.StartNew();

            while (runningAsyncs > 0)
            {
                Log.AddInfo($"Waiting for {runningAsyncs} requests to finish");
                Thread.Sleep(5000);

                if (waitingWatch.Elapsed.TotalMinutes >= 10)
                {
                    Log.AddInfo($"Waited long enough, won't wait any longer on {runningAsyncs} running requests");
                    break;
                }
            }

            waitingWatch = Stopwatch.StartNew();

            while (processingRequests > 0)
            {
                Log.AddInfo($"Waiting for {processingRequests} requests to be processed");
                Thread.Sleep(5000);

                if (waitingWatch.Elapsed.TotalMinutes >= 10)
                {
                    Log.AddInfo($"Waited long enough, won't wait any longer on {processingRequests} requests still to be processed");
                    break;
                }
            }

            if (currentlyCheckingIDs.Count > 0)
            {
                Log.AddInfo("Did not get requests and/or processed them for these IDs:");
                Log.AddInfo(String.Join(";", currentlyCheckingIDs));
            }
            #endregion

            Log.AddResult("Writing final results to log");
            LogResultsMoE();

            Log.AddResult("Making all lists disinct where needed (MoETanks' players, player ids to check)");

            HandleMoEDataSaving(serverID, retry);
        }

        private void CheckWebClientsMaxRuntime()
        {
            Log.AddInfo($"Checking {runningAsyncs} web clients for their run time without getting a response yet");
            foreach (KeyValuePair<string, WebClientInfoItem> keyValuePair in WebClientDict)
            {
                WebClientInfoItem webInfoItem = keyValuePair.Value;
                webInfoItem.Stopwatch.Stop();

                if (webInfoItem.Stopwatch.ElapsedMilliseconds > MoEStatic.MaxMillisecondRunTimeOfRequest && webInfoItem.WebClient.IsBusy)
                {
                    Log.AddWarning($"Cancelling web client request for player ID {keyValuePair.Key}, because it already runs {webInfoItem.Stopwatch.ElapsedMilliseconds} ms with an allowed maximum of {MoEStatic.MaxMillisecondRunTimeOfRequest} ms");
                    webInfoItem.WebClient.CancelAsync();
                }
                else
                    webInfoItem.Stopwatch.Start();
            }
        }
        private void ClearWebClientsDictFromFinishedItems()
        {
            Log.AddInfo($"Checking {WebClientDict.Count} items in the webclient dict");

            List<string> idsToRemove = WebClientDict.Where(x => !x.Value.WebClient.IsBusy).Select(y => y.Key).ToList();

            foreach (string key in idsToRemove)
            {
                WebClientDict.Remove(key);
            }

            Log.AddInfo($"{WebClientDict.Count} items still remaining in the dict");
        }

        private void HandleMoEDataSaving(string serverID, bool retry)
        {
            if (!retry)
                SaveMoEData(serverID);
            else
                SaveMoERetryData(serverID);
        }

        private void SaveMoEData(string serverID)
        {
            Log.AddResult($"Saving MoETTanks ({GetMoETanksFileName(serverID)}), player IDs ({GetPlayerIDListFileName(serverID)}), failed player IDs ({GetMoEFailedPlayersFileName(serverID)}) and player IDs stuck in processing ({GetMoEPlayersWithoutAnswerFileName(serverID)})");
            SaveObjectToJsonFile(MoETanks, GetMoETanksFileName(serverID));
            SaveObjectToJsonFile(playerIDsToCheck, GetPlayerIDListFileName(serverID));
            SaveObjectToJsonFile(failedIDs, GetMoEFailedPlayersFileName(serverID));
            SaveObjectToJsonFile(currentlyCheckingIDs, GetMoEPlayersWithoutAnswerFileName(serverID));
        }

        private void SaveMoERetryData(string serverID)
        {
            Log.AddResult($"Saving retry MoETTanks ({GetMoETanksRetryFileName(serverID)}), retry player IDs ({GetPlayerIDListRetryFileName(serverID)}), failed player IDs ({GetMoEFailedPlayersRetryFileName(serverID)}) and player IDs stuck in processing ({GetMoEPlayersWithoutAnswerRetryFileName(serverID)})");
            SaveObjectToJsonFile(MoETanks, GetMoETanksRetryFileName(serverID));
            SaveObjectToJsonFile(playerIDsToCheck, GetPlayerIDListRetryFileName(serverID));
            SaveObjectToJsonFile(failedIDs, GetMoEFailedPlayersRetryFileName(serverID));
            SaveObjectToJsonFile(currentlyCheckingIDs, GetMoEPlayersWithoutAnswerRetryFileName(serverID));
        }

        private void LogResultsMoE()
        {
            Log.AddResult($"Checked {currentRequests} of {totalRequests} player IDs");
            Log.AddResult($"Found {playerIDsToCheck.Count} players with at least 1x 2/3 MoE");
            Log.AddResult($"Failed to parse and/or get proper request for {failedIDs.Count} player IDs");
            Log.AddResult($"Player IDs stuck in processing and/or getting the request: {currentlyCheckingIDs.Count}");
            Log.AddResult($"Async requests still running: {runningAsyncs} from {runningAsyncsMax} max");
            Log.AddResult($"Requests still being processed: {processingRequests}");
        }

        private void LogResultsPlayerIDs()
        {
            Log.AddResult($"Checked {currentRequests} of {totalRequests} possible player IDs");
            //Log.AddInfo($"Found {playerIDsToCheck.Count} players with at least 1x 2/3 MoE");
            Log.AddResult($"Failed to parse and/or get proper request for {failedIDs.Count} player IDs");
            Log.AddResult($"Possible player IDs stuck in processing and/or getting the request: {currentlyCheckingIDs.Count}");
            Log.AddResult($"Async requests still running: {runningAsyncs} from {runningAsyncsMax} max");
            Log.AddResult($"Requests still being processed: {processingRequests}");
        }

        private void Client_DownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            runningAsyncs--;

            if (e.Cancelled)
            {
                Log.AddError($"Request for player ID {e.UserState} was cancelled, adding ID to list with failed IDs");
                failedIDs.Add(e.UserState.ToString());

                if (currentlyCheckingIDs.Contains(e.UserState.ToString()))
                {
                    Log.AddInfo($"Removed player {e.UserState} from currently checking id list");
                    currentlyCheckingIDs.Remove(e.UserState.ToString());
                }
                else
                    Log.AddWarning($"Player {e.UserState} not found in currently checking id list");
            }
            else
            {
                Log.AddInfo($"Request answer received for ID {e.UserState}");

                processingRequests++;

                if (e.Error == null)
                {
                    try
                    {
                        if (!String.IsNullOrEmpty(e.UserState.ToString()) && !String.IsNullOrEmpty(e.Result) && e.Result != "{}")
                        {
                            bool hasToCheckPlayer = false;

                            #region parse data
                            string id = e.UserState.ToString();
                            string jsonstring = e.Result;

                            JObject jobj = JObject.Parse(jsonstring);
                            checkedIDs.Add(id);

                            if (jobj["data"] != null && jobj["data"][id] != null)
                            {
                                //foreach (JToken jt in jobj["data"][id].Children())
                                foreach (JObject jTank in jobj["data"][id].Children<JObject>())
                                {
                                    //JObject jTank = JObject.Parse(jt.ToString());

                                    if (jTank["achievements"]["marksOnGun"] != null)
                                    {
                                        Tank t = new Tank();
                                        double marks = Convert.ToDouble(jTank["achievements"]["marksOnGun"]);

                                        if (!MoETanks.Any(x => x.Tank.TankIDNumeric == jTank.GetString("tank_id")))
                                        {
                                            MoETank newMoETank = new MoETank();

                                            newMoETank.Tank.TankIDNumeric = jTank.GetString("tank_id");
                                            newMoETank.Tank.Tier = 420;
                                            newMoETank.Tank.Name = "@@@Tank not found in Tenkopedia but in Player's list@@@";

                                            Log.AddWarning($"Tank missing from Tenkopedia: {jTank.GetString("tank_id")}, player: {id}");

                                            MoETanks.Add(newMoETank);
                                        }
                                        MoETank moeTank = MoETanks.First(x => x.Tank.TankIDNumeric == jTank.GetString("tank_id"));

                                        if (marks == 3)
                                        {
                                            moeTank.Players_3MoE.Add(id);
                                            hasToCheckPlayer = true;
                                        }
                                        else if (marks == 2)
                                        {
                                            //moeTank.Players_2MoE.Add(id);
                                            //hasToCheckPlayer = true;
                                        }
                                        else if (marks == 1)
                                        {

                                        }
                                        else
                                        {

                                        }
                                    }
                                }
                            }
                            else
                            {
                                //failedIDs.Add(id);
                                AddToFailedIDsSavingSave(id);
                                Log.AddError(String.Format("Failed to get/parse data for player {0}, json is null", e.UserState));
                                if (jobj["error"] != null)
                                    Log.AddError("Error trying to get player's vehicles' achievements data: " + jobj["error"].ToString());
                            }
                            #endregion

                            currentRequests++;

                            if (hasToCheckPlayer)
                            {
                                Log.AddInfo($"Adding player {id} to list with players to be checked");
                                AddToPlayerIDsToCheckSavingSave(id);
                            }
                            else
                                Log.AddInfo($"Player {id} does not have to be checked");
                            
                            ReportRequestProgress($"Checked player {id}");
                        }
                        else
                        {
                            Log.AddError($"Invalid Result for player {e.UserState}");

                            AddToFailedIDsSavingSave(e.UserState);
                        }
                    }
                    catch (Exception excp)
                    {
                        // something got fucked
                        AddToFailedIDsSavingSave(e.UserState);
                        Log.AddError($"Failed to get/parse data for player {e.UserState}", excp);
                    }
                }
                else
                {
                    Log.AddError($"An error occuring in the request for player {e.UserState}", e.Error);
                    AddToFailedIDsSavingSave(e.UserState);
                }

                if (currentlyCheckingIDs.Contains(e.UserState.ToString()))
                {
                    Log.AddInfo($"Removed player {e.UserState} from currently checking id list");
                    currentlyCheckingIDs.RemoveAll(x => x == e.UserState.ToString());
                }
                else
                    Log.AddWarning($"Player {e.UserState} not found in currently checking id list");

                processingRequests--;
                Log.AddInfo($"Finished string download event for player {e.UserState}");
            }            
        }

        private void AddToFailedIDsSavingSave(object obj)
        {
            AddToFailedIDsSavingSave(obj.ToString());
        }
        private void AddToFailedIDsSavingSave(string id)
        {
            CheckDataSavingState($"Adding {id} to failed IDs");
            failedIDs.Add(id);
        }
        private void AddToPlayerIDsToCheckSavingSave(string id)
        {
            CheckDataSavingState($"Adding {id} to player IDs to check");
            playerIDsToCheck.Add(id);
        }
        private void CheckDataSavingState(string message)
        {
            while (isSavingData)
            {
                Thread.Sleep(1000);
                Log.AddInfo($"{message}: waited 1s because data is saving");
            }
        }

        private void SaveObjectToJsonFile(object obj, string filePath)
        {
            Log.AddResult("Saving object to xml file: " + filePath);

            try
            {
                XmlSerializer SerializerObj = new XmlSerializer(obj.GetType());

                TextWriter WriteFileStream = new StreamWriter(filePath);
                SerializerObj.Serialize(WriteFileStream, obj);

                WriteFileStream.Close();
                Log.AddResult($"Saved object {filePath} successfully");
            }
            catch(Exception excp)
            {
                Log.AddError($"Failed to save object {filePath}", excp);
            }
        }
        private void SaveIDListToTxtFile(List<string> list, string filePath)
        {
            Log.AddResult("Saving string list to txt file: " + filePath);

            try
            {
                File.WriteAllLines(filePath, list);
                Log.AddResult($"Saved object {filePath} successfully");
            }
            catch (Exception excp)
            {
                Log.AddError($"Failed to save object {filePath}", excp);
            }
        }
        #endregion

        #region double id checking 
        private void HandeDoubleIDChecking(string serverID)
        {
            List<string> prePlayerIDs = ReadObjectFromFile<List<string>>(@"E:\MoE Stuff\Dank MoE Server Stuff\eu-2\moe retry\Retry-MoEPlayers-EU.xml");
            List<string> newPlayerIDs = ReadObjectFromFile<List<string>>(@"E:\MoE Stuff\Dank MoE Server Stuff\eu\moe\MoEPlayers-EU.xml");

            List<string> playersToCheckAgain = new List<string>();

            double counter = 1;
            int totalCount = prePlayerIDs.Count;

            foreach(string preID in prePlayerIDs)
            {
                Console.WriteLine($"Checking player id {counter}/{totalCount} ({counter / totalCount:P2})");

                if (!newPlayerIDs.Contains(preID))
                    playerIDsToCheck.Add(preID);

                counter++;
            }

            Console.WriteLine("Loading original ids without answer");
            List<string> previousFailedIDs = ReadObjectFromFile<List<string>>(@"E:\MoE Stuff\Dank MoE Server Stuff\eu\moe\MoEPlayersWithoutAnswer-EU.xml");
            previousFailedIDs.Where(x => !String.IsNullOrEmpty(x)).ToList().ForEach(x => playerIDsToCheck.Add(x));

            Console.WriteLine("Making new list distinct");
            playerIDsToCheck = playerIDsToCheck.Distinct().ToList();

            Console.WriteLine($"Saving {playerIDsToCheck.Count} ids to the new list");
            SaveIDListToTxtFile(playerIDsToCheck, @"E:\MoE Stuff\Dank MoE Server Stuff\eu\moe\MoEPlayersWithoutAnswer-EU-DANKSTUFF.xml");
        }
        #endregion

        #region super krasser database shit
        private void HandleDataBaseTest(string serverID, string appID)
        {
            // load moe tanks
            Log.AddInfo($"Loading MoETank list from {GetMoETanksFileName(serverID)}");
            List<MoETank> moeTanks = ReadObjectFromFile<List<MoETank>>(GetMoETanksFileName(serverID));
            Log.AddInfo($"Found {moeTanks.Count} tanks in the list");

            Log.AddInfo($"Loading player list from {GetPlayerListFileName(serverID)}");
            List<Player> Players = ReadObjectFromFile<List<Player>>(GetPlayerListFileName(serverID));
            Log.AddInfo($"Found {Players.Count} players in the list");

            Log.AddInfo($"Loading clan list from {GetClanListFileName(serverID)}");
            List<Clan> Clans = ReadObjectFromFile<List<Clan>>(GetClanListFileName(serverID));
            Log.AddInfo($"Found {Clans.Count} clans in the list");

            MoETank specialMoETank = moeTanks.First(x => x.Tank.TankIDNumeric == "16913");
            specialMoETank.Tank.Name = "Waffenträger auf E 100";
            specialMoETank.Tank.NameShort = "WT auf E 100";
            specialMoETank.Tank.NationID = MoEStatic.NationGermany;
            specialMoETank.Tank.TankTypeID = MoEStatic.TankTypeTD;
            specialMoETank.Tank.Tier = 10;

            JObject jTankopedia = GetJObjectFromAPI("wot/encyclopedia/info/", serverID, appID, "en");
            Dictionary<string, string> nationDict = GetNationDictionary(jTankopedia);
            Dictionary<string, string> tankDict = GetTankTypeDictionary(jTankopedia);

            moeTanks.ForEach(x => x.Tank.Nation = nationDict[x.Tank.NationID]);
            moeTanks.ForEach(x => x.Tank.TankType = tankDict[x.Tank.TankTypeID]);

            //tanks.Add(new Tank("16913", "Waffenträger auf E 100", "WT auf E 100", MoEStatic.NationGermany, MoEStatic.TankTypeTD, 10, false, ""));
            // setup connection and connectionstring
            MySqlConnection connection;
            string myConnectionString;

            double currentCount = 0;
            int totalCount = 0;

            myConnectionString = "Server=localhost;Database=Tanks;Uid=marvin;Pwd=SqLpW134";

            try
            {
                // open connection
                Log.AddInfo("Opening connection...");
                connection = new MySqlConnection();
                connection.ConnectionString = myConnectionString;
                connection.Open();
                Log.AddInfo("Opened connection successfully");

                //Log.AddInfo("Updating Tanks info");

                //foreach(MoETank moeTank in moeTanks)
                //{
                //    MySqlCommand command = connection.CreateCommand();
                //    command.CommandText = $"UPDATE T_Tanks SET nation_id='{moeTank.Tank.NationID}', tank_type_id='{moeTank.Tank.TankTypeID}' WHERE p_tank_id='{moeTank.Tank.TankIDNumeric}'";
                //    command.ExecuteNonQuery();
                //    Log.AddInfo($"Updated data for Tank {moeTank.Tank.TankIDNumeric}:{moeTank.Tank.Name}");
                //}

                Log.AddInfo("Adding tanks to T_Tanks table");
                foreach (MoETank moeTank in moeTanks)
                {
                    MySqlCommand command = connection.CreateCommand();
                    command.CommandText = "INSERT INTO T_Tanks (p_tank_id, name_long, name_short, nation_id, nation, tank_type_id, tank_type, tier, contour_icon_path, small_icon_path, big_icon_path) ";
                    command.CommandText += $"VALUES ('{moeTank.Tank.TankIDNumeric}', '{moeTank.Tank.Name}', '{moeTank.Tank.NameShort}', '{moeTank.Tank.NationID}', '{moeTank.Tank.Nation}', '{moeTank.Tank.TankTypeID}', '{moeTank.Tank.TankType}', '{moeTank.Tank.Tier}', '{moeTank.Tank.IconContourUrl}', '{moeTank.Tank.IconSmallUrl}', '{moeTank.Tank.IconBigUrl}')";
                    command.CommandText += "ON DUPLICATE KEY UPDATE ";
                    command.CommandText+= $"p_tank_id='{moeTank.Tank.TankIDNumeric}', name_long='{moeTank.Tank.Name.Replace("'","\'")}', name_short='{moeTank.Tank.NameShort}', nation_id='{moeTank.Tank.NationID}', nation='{moeTank.Tank.Nation}', tank_type_id='{moeTank.Tank.TankTypeID}', tank_type='{moeTank.Tank.TankType}', tier='{moeTank.Tank.Tier}', contour_icon_path='{moeTank.Tank.IconContourUrl}', small_icon_path='{moeTank.Tank.IconSmallUrl}', big_icon_path='{moeTank.Tank.IconBigUrl}'";

                    Log.AddInfo($"Inserting Tank {moeTank.Tank.TankIDNumeric}:{moeTank.Tank.Name} into table T_Tanks");
                    command.ExecuteNonQuery();
                }
                Log.AddInfo("Finished adding tanks to T_Tanks table");

                currentCount = 1;
                totalCount = Clans.Count;
                Log.AddInfo("Adding clans to T_Clans");
                foreach (Clan clan in Clans)
                {
                    Log.AddInfo($"Adding clan {currentCount:N0}/{totalCount:N0} ({currentCount/totalCount:P2}), {clan.Name.Replace("'", "''")}");
                    MySqlCommand command = connection.CreateCommand();
                    command.CommandText = "INSERT INTO T_Clans (p_clan_id, name, tag, member_count, color, logo_path)";
                    command.CommandText += $"VALUES ('{clan.DBID}', '{clan.Name.Replace("'","''")}', '{clan.Tag}', '{clan.MemberCount}', '{clan.ColorHex}', '{clan.ClanIcon256pxUrl}')";
                    command.CommandText += " ON DUPLICATE KEY UPDATE ";
                    command.CommandText += $"p_clan_id='{clan.DBID}', name='{clan.Name.Replace("'","''")}', tag='{clan.Tag}', member_count='{clan.MemberCount}', color='{clan.ColorHex}', logo_path='{clan.ClanIcon256pxUrl}'";

                    command.ExecuteNonQuery();
                    currentCount++;
                }

                Log.AddInfo("Add dummy clan for clan less players");
                MySqlCommand specialCommand = connection.CreateCommand();
                specialCommand.CommandText = "INSERT INTO T_Clans (p_clan_id, name, tag, member_count, color, logo_path)";
                specialCommand.CommandText += $"VALUES ('0', 'NO CLAN', '', '0', '#FFFFFF', '')";
                specialCommand.CommandText += " ON DUPLICATE KEY UPDATE ";
                specialCommand.CommandText += $"p_clan_id='0', name='NO CLAN', tag='', member_count='0', color='#FFFFFF', logo_path=''";

                specialCommand.ExecuteNonQuery();
                Log.AddInfo("Finished adding clans to T_Clans");

                currentCount = 1;
                totalCount = Players.Count;
                Log.AddInfo("Adding players to T_Players");
                foreach(Player player in Players)
                {
                    Log.AddInfo($"Adding player {currentCount:N0}/{totalCount:N0} ({currentCount / totalCount:P2}), {player.Name}");
                    MySqlCommand command = connection.CreateCommand();
                    command.CommandText = "INSERT INTO T_Players (p_account_id, f_clan_id, name, client_lang, battles, wins, last_battle, account_created, wg_rating, wn8)";
                    command.CommandText += $"VALUES ('{player.AccountDBID}', '{player.GetClanDBID()}', '{player.Name}', '{player.ClientLanguage}', '{player.Battles}', '{player.GetWins(Log)}', '{player.GetLastBattleTimeStamp()}', '{player.GetAccountCreatedTimeStamp()}', '{player.WGRating}', '{player.GetWN8(Log)}')";
                    command.CommandText += " ON DUPLICATE KEY UPDATE ";
                    command.CommandText += $"p_account_id='{player.AccountDBID}', f_clan_id='{player.GetClanDBID()}', name='{player.Name}', client_lang='{player.ClientLanguage}', battles='{player.Battles}', wins='{player.GetWins(Log)}', last_battle='{player.GetLastBattleTimeStamp()}', account_created='{player.GetAccountCreatedTimeStamp()}', wg_rating='{player.WGRating}', wn8='{player.GetWN8(Log)}'";

                    command.ExecuteNonQuery();
                    currentCount++;
                }
                Log.AddInfo("Finished adding players to T_Players");


                Log.AddInfo("Adding player/tank/moes to T_Marks");
                currentCount = 1;
                totalCount = moeTanks.Sum(x => x.Players_3MoE.Count);
                foreach(MoETank moeTank in moeTanks)
                {
                    foreach(string playerID in moeTank.Players_3MoE)
                    {
                        Player player = Players.First(x => x.AccountDBID == playerID);

                        Log.AddInfo($"Adding player/tank/moe combo {currentCount:N0}/{totalCount:N0} ({currentCount / totalCount:P2}), {player.Name} + {moeTank.Tank.Name}");
                        MySqlCommand command = connection.CreateCommand();
                        command.CommandText = "INSERT INTO T_Marks (p_f_account_id, p_f_tank_id, battles, damage, spots, kills, decap, cap, wins, marks)";
                        command.CommandText += $"VALUES ('{player.AccountDBID}', '{moeTank.Tank.TankIDNumeric}', '0', '0', '0', '0', '0', '0', '0', '3')";
                        command.CommandText += " ON DUPLICATE KEY UPDATE ";
                        command.CommandText += $"p_f_account_id='{player.AccountDBID}', p_f_tank_id='{moeTank.Tank.TankIDNumeric}', battles='0', damage='0', spots='0', kills='0', decap='0', cap='0', wins='0', marks='3'";

                        command.ExecuteNonQuery();
                        currentCount++;
                    }
                }
                Log.AddInfo("Finished adding player/tank/moes to T_Marks");
                //Log.AddInfo("Reading tanks from T_Tanks table");

                //MySqlCommand readCommand = connection.CreateCommand();
                //readCommand.CommandText = "SELECT * FROM T_Tanks";
                //MySqlDataReader dataReader = readCommand.ExecuteReader();

                //while (dataReader.Read())
                //{
                //    Log.AddInfo($"Found Tank: {dataReader["p_tank_id"]}, {dataReader["name_long"]}, {dataReader["name_short"]}, {dataReader["nation"]}, {dataReader["tank_type"]}, {dataReader["tier"]}, {dataReader["contour_icon_path"]}, {dataReader["small_icon_path"]}, {dataReader["big_icon_path"]}");
                //}

                //Log.AddInfo("Closing DataReader");
                //dataReader.Close();

                // close connection
                Log.AddInfo("Closing connection");
                connection.Close();
                Log.AddInfo("Connection closed");
            }
            catch (MySqlException ex)
            {
                Log.AddError("Error connecting to the database", ex);
            }
        }

        private MySqlConnection GetMySqlConnection()
        {            
            string myConnectionString = "Server=db;Database=moe;Uid=root;Pwd=root";

            Log.AddInfo("Opening connection...");
            MySqlConnection connection = new MySqlConnection();
            connection.ConnectionString = myConnectionString;
            connection.Open();
            Log.AddInfo("Connection opened");
            return connection;
        }

        private void HandleDockerDataBaseTest(string serverID)
        {
            Log.AddInfo("Starting dockerized database test");

            string appID = "b8f444ecca8a5752b421ef9c610254b9";

            Log.AddInfo($"Loading MoETank list from {GetMoETanksFileName(serverID)}");
            List<MoETank> moeTanks = ReadObjectFromFile<List<MoETank>>(GetMoETanksFileName(serverID));
            Log.AddInfo($"Found {moeTanks.Count} tanks in the list");

            Log.AddInfo($"Loading player list from {GetPlayerListFileName(serverID)}");
            List<Player> Players = ReadObjectFromFile<List<Player>>(GetPlayerListFileName(serverID));
            Log.AddInfo($"Found {Players.Count} players in the list");

            Log.AddInfo($"Loading clan list from {GetClanListFileName(serverID)}");
            List<Clan> Clans = ReadObjectFromFile<List<Clan>>(GetClanListFileName(serverID));
            Log.AddInfo($"Found {Clans.Count} clans in the list");

            JObject jTankopedia = GetJObjectFromAPI("wot/encyclopedia/info/", serverID, appID, "en");
            Dictionary<string, string> nationDict = GetNationDictionary(jTankopedia);
            Dictionary<string, string> tankDict = GetTankTypeDictionary(jTankopedia);

            moeTanks = GetUpdatedMoETankList(moeTanks, serverID, appID);
            //moeTanks.ForEach(x => x.Tank.Nation = nationDict[x.Tank.NationID]);
            //moeTanks.ForEach(x => x.Tank.TankType = tankDict[x.Tank.TankTypeID]);

            List<Player> playersWithoutClan = Players.Where(x => !String.IsNullOrEmpty(x.ClanDBID) && !Clans.Any(z => z.DBID == x.ClanDBID)).ToList();
            string requesturl = String.Format("https://api.worldoftanks.{0}/wgn/clans/info/?application_id={1}&clan_id={2}", GetAPISuffix(serverID), appID, String.Join(",", playersWithoutClan.Select(x => x.ClanDBID)));

            WebClient wc = new WebClient();
            wc.Encoding = Encoding.UTF8;
            wc.Proxy = null;
            string jsonstring = wc.DownloadString(requesturl);
            //requestresponses.Remove(id);

            JObject jobj = JObject.Parse(jsonstring);

            foreach (string id in playersWithoutClan.Select(x => x.ClanDBID))
            {
                //Log.AddInfo(String.Format("Checking clan {0}", id));
                Clan c = new Clan();
                c.DBID = id;

                c.Name = jobj["data"][c.DBID]["name"].ToString();
                c.Tag = jobj["data"][c.DBID]["tag"].ToString();
                c.MemberCount = GetInt(jobj["data"][c.DBID]["members_count"]);
                c.ColorHex = jobj["data"][c.DBID]["color"].ToString();
                c.UpdatedAt = jobj["data"][c.DBID]["updated_at"].ToString();
                c.ClanIcon24pxUrl = jobj["data"][c.DBID]["emblems"]["x24"]["portal"].ToString();
                c.ClanIcon32pxUrl = jobj["data"][c.DBID]["emblems"]["x32"]["portal"].ToString();
                c.ClanIcon64pxUrl = jobj["data"][c.DBID]["emblems"]["x64"]["portal"].ToString();
                c.ClanIcon195pxUrl = jobj["data"][c.DBID]["emblems"]["x195"]["portal"].ToString();
                c.ClanIcon256pxUrl = jobj["data"][c.DBID]["emblems"]["x256"]["wowp"].ToString();

                Clans.Add(c);
            }

            playersWithoutClan = Players.Where(x => !String.IsNullOrEmpty(x.ClanDBID) && !Clans.Any(z => z.DBID == x.ClanDBID)).ToList();

            MySqlConnection connection;
            MySqlCommand command;

            string tableName;
            double counter = 0;
            int totalCount = 0;
            string ignore = " IGNORE";

            string myConnectionString = "Server=db;Database=moe;Uid=root;Pwd=root";
            Log.AddInfo($"Connection string: {myConnectionString}");

            Log.AddInfo("Opening connection...");
            connection = new MySqlConnection();
            connection.ConnectionString = myConnectionString;
            connection.Open();
            Log.AddInfo("Opened connection successfully");

            tableName = "nations";
            Log.AddInfo($"Adding nations to {tableName} table");
            foreach (var kvp in nationDict)
            {
                command = connection.CreateCommand();
                command.CommandText = $"INSERT{ignore} INTO {tableName} (id, name, created_at, updated_at) ";
                command.CommandText += $"VALUES ('{kvp.Key}', '{kvp.Value}', @now, @now)";
                command.Parameters.AddWithValue("@now", DateTime.Now);
                //command.CommandText += "ON DUPLICATE KEY UPDATE ";
                //command.CommandText += $"id='{moeTank.Tank.TankIDNumeric}', name='{moeTank.Tank.Name.Replace("'", "\'")}', created_at='{moeTank.Tank.NameShort}', nation_id='{moeTank.Tank.NationID}', nation='{moeTank.Tank.Nation}', tank_type_id='{moeTank.Tank.TankTypeID}', tank_type='{moeTank.Tank.TankType}', tier='{moeTank.Tank.Tier}', contour_icon_path='{moeTank.Tank.IconContourUrl}', small_icon_path='{moeTank.Tank.IconSmallUrl}', big_icon_path='{moeTank.Tank.IconBigUrl}'";

                Log.AddInfo($"Inserting nation {kvp.Key}:{kvp.Value} into table {tableName}");
                command.ExecuteNonQuery();
            }
            Log.AddInfo($"Finished adding tanks to {tableName} table");

            tableName = "vehicle_types";
            Log.AddInfo($"Adding vehicletypes to {tableName} table");
            foreach (var kvp in tankDict)
            {
                command = connection.CreateCommand();
                command.CommandText = $"INSERT{ignore} INTO {tableName} (id, name, created_at, updated_at) ";
                command.CommandText += $"VALUES ('{kvp.Key}', '{kvp.Value}', @now, @now)";
                command.Parameters.AddWithValue("@now", DateTime.Now);
                //command.CommandText += "ON DUPLICATE KEY UPDATE ";
                //command.CommandText += $"id='{moeTank.Tank.TankIDNumeric}', name='{moeTank.Tank.Name.Replace("'", "\'")}', created_at='{moeTank.Tank.NameShort}', nation_id='{moeTank.Tank.NationID}', nation='{moeTank.Tank.Nation}', tank_type_id='{moeTank.Tank.TankTypeID}', tank_type='{moeTank.Tank.TankType}', tier='{moeTank.Tank.Tier}', contour_icon_path='{moeTank.Tank.IconContourUrl}', small_icon_path='{moeTank.Tank.IconSmallUrl}', big_icon_path='{moeTank.Tank.IconBigUrl}'";

                Log.AddInfo($"Inserting vehicle type {kvp.Key}:{kvp.Value} into table {tableName}");
                command.ExecuteNonQuery();
            }
            Log.AddInfo($"Finished adding vehicletypes to {tableName} table");

            counter = 1; totalCount = moeTanks.Count;
            tableName = "tanks";
            Log.AddInfo($"Adding tanks to {tableName} table");
            foreach (var value in moeTanks.Where(x => x.Tank.Tier >= 5))
            {
                Tank tank = value.Tank;
                command = connection.CreateCommand();
                command.CommandText = $"INSERT{ignore} INTO {tableName} (id, ispremium, name, shortname, tier, bigicon, contouricon, smallicon, nation_id, vehicle_type_id, created_at, updated_at) ";
                command.CommandText += $"VALUES ('{tank.TankIDNumeric}', '{tank.IsPremium}','{MySqlHelper.EscapeString(tank.Name)}','{MySqlHelper.EscapeString(tank.NameShort)}','{tank.Tier}','{tank.IconBigUrl}','{tank.IconContourUrl}','{tank.IconSmallUrl}','{tank.NationID}','{tank.TankTypeID}', @now, @now)";
                command.Parameters.AddWithValue("@now", DateTime.Now);
                //command.CommandText += "ON DUPLICATE KEY UPDATE ";
                //command.CommandText += $"id='{moeTank.Tank.TankIDNumeric}', name='{moeTank.Tank.Name.Replace("'", "\'")}', created_at='{moeTank.Tank.NameShort}', nation_id='{moeTank.Tank.NationID}', nation='{moeTank.Tank.Nation}', tank_type_id='{moeTank.Tank.TankTypeID}', tank_type='{moeTank.Tank.TankType}', tier='{moeTank.Tank.Tier}', contour_icon_path='{moeTank.Tank.IconContourUrl}', small_icon_path='{moeTank.Tank.IconSmallUrl}', big_icon_path='{moeTank.Tank.IconBigUrl}'";

                Log.AddInfo($"{counter:N0}/{totalCount:N0} ({counter / totalCount:P2}) | Inserting tank {tank.TankIDNumeric}:{tank.Name} into table {tableName}");
                counter++;
                command.ExecuteNonQuery();
            }
            Log.AddInfo($"Finished adding tanks to {tableName} table");

            counter = 1; totalCount = Clans.Count;
            tableName = "clans";
            Log.AddInfo($"Adding clans to {tableName} table");
            foreach (var clan in Clans)
            {
                command = connection.CreateCommand();
                command.CommandText = $"INSERT{ignore} INTO {tableName} (id, name, tag, cHex, members, updatedAtWG, clanCreated, icon24px, icon32px, icon64px, icon195px, icon256px, created_at, updated_at) ";
                command.CommandText += $"VALUES ('{clan.DBID}', '{MySqlHelper.EscapeString(clan.Name)}', '{clan.Tag}', '{clan.ColorHex}', '{clan.MemberCount}', '', '{GetMySQLDateTimeString(clan.UpdatedAt)}', '{clan.ClanIcon24pxUrl}', '{clan.ClanIcon32pxUrl}', '{clan.ClanIcon64pxUrl}', '{clan.ClanIcon195pxUrl}', '{clan.ClanIcon256pxUrl}', @now, @now)";
                command.Parameters.AddWithValue("@now", DateTime.Now);
                //command.CommandText += "ON DUPLICATE KEY UPDATE ";
                //command.CommandText += $"id='{moeTank.Tank.TankIDNumeric}', name='{moeTank.Tank.Name.Replace("'", "\'")}', created_at='{moeTank.Tank.NameShort}', nation_id='{moeTank.Tank.NationID}', nation='{moeTank.Tank.Nation}', tank_type_id='{moeTank.Tank.TankTypeID}', tank_type='{moeTank.Tank.TankType}', tier='{moeTank.Tank.Tier}', contour_icon_path='{moeTank.Tank.IconContourUrl}', small_icon_path='{moeTank.Tank.IconSmallUrl}', big_icon_path='{moeTank.Tank.IconBigUrl}'";

                Log.AddInfo($"{counter:N0}/{totalCount:N0} ({counter / totalCount:P2}) | Inserting clan {clan.DBID}:{clan.Name} into table {tableName}");
                counter++;
                command.ExecuteNonQuery();
            }
            Log.AddInfo($"Finished adding clans to {tableName} table");

            counter = 1; totalCount = Players.Count;
            tableName = "players";
            Log.AddInfo($"Adding players to {tableName} table");
            foreach (var player in Players)
            {
                command = connection.CreateCommand();
                command.CommandText = $"INSERT{ignore} INTO {tableName} (id, name, battles, wgrating, winratio, lastLogout, lastBattle, accountCreated, updatedAtWG, wn8, clientLang, clan_id, created_at, updated_at) ";
                command.CommandText += $"VALUES ('{player.AccountDBID}', '{player.Name}', '{player.Battles}', '{player.WGRating}', '{player.WinRate}', '{GetMySQLDateTimeString(player.LastLogout)}', '{GetMySQLDateTimeString(player.LastBattle)}', '{GetMySQLDateTimeString(player.AccountCreated)}', '{GetMySQLDateTimeString(player.UpdatedAt)}', '{Math.Round(player.WN8Data.WN8, 0)}', '{player.ClientLanguage}', {GetMySQLClanString(player)}, @now, @now)";
                command.Parameters.AddWithValue("@now", DateTime.Now);
                //command.CommandText += "ON DUPLICATE KEY UPDATE ";
                //command.CommandText += $"id='{moeTank.Tank.TankIDNumeric}', name='{moeTank.Tank.Name.Replace("'", "\'")}', created_at='{moeTank.Tank.NameShort}', nation_id='{moeTank.Tank.NationID}', nation='{moeTank.Tank.Nation}', tank_type_id='{moeTank.Tank.TankTypeID}', tank_type='{moeTank.Tank.TankType}', tier='{moeTank.Tank.Tier}', contour_icon_path='{moeTank.Tank.IconContourUrl}', small_icon_path='{moeTank.Tank.IconSmallUrl}', big_icon_path='{moeTank.Tank.IconBigUrl}'";

                Log.AddInfo($"{counter:N0}/{totalCount:N0} ({counter / totalCount:P2}) | Inserting player {player.AccountDBID}:{player.Name} into table {tableName}");
                counter++;
                command.ExecuteNonQuery();
            }
            Log.AddInfo($"Finished adding players to {tableName} table");

            counter = 1; totalCount = moeTanks.Sum(x => x.Players_3MoE.Count);
            tableName = "marks";
            Log.AddInfo($"Adding marks to {tableName} table");
            foreach (var moeTank in moeTanks.Where(x => x.Tank.Tier >= 5))
            {
                foreach (var playerID in moeTank.Players_3MoE)
                {
                    command = connection.CreateCommand();
                    command.CommandText = $"INSERT{ignore} INTO {tableName} (tank_id, player_id, created_at, updated_at) ";
                    command.CommandText += $"VALUES ('{moeTank.Tank.TankIDNumeric}', '{playerID}', @now, @now)";
                    command.Parameters.AddWithValue("@now", DateTime.Now);
                    //command.CommandText += "ON DUPLICATE KEY UPDATE ";
                    //command.CommandText += $"id='{moeTank.Tank.TankIDNumeric}', name='{moeTank.Tank.Name.Replace("'", "\'")}', created_at='{moeTank.Tank.NameShort}', nation_id='{moeTank.Tank.NationID}', nation='{moeTank.Tank.Nation}', tank_type_id='{moeTank.Tank.TankTypeID}', tank_type='{moeTank.Tank.TankType}', tier='{moeTank.Tank.Tier}', contour_icon_path='{moeTank.Tank.IconContourUrl}', small_icon_path='{moeTank.Tank.IconSmallUrl}', big_icon_path='{moeTank.Tank.IconBigUrl}'";

                    Log.AddInfo($"{counter:N0}/{totalCount:N0} ({counter / totalCount:P2}) | Inserting mark {moeTank.Tank.TankIDNumeric}:{playerID} into table {tableName}");
                    counter++;
                    command.ExecuteNonQuery();
                }
            }
            Log.AddInfo($"Finished adding clans to {tableName} table");

            Log.AddInfo("YOU FUCKING DID IT MATE!!!");
            Log.AddInfo("ALL DATA ADDED, MOM GET THE TTOURS!!!!");
        }
        private string GetString(DateTime dateTime)
        {
            return dateTime.ToString("yyyy-MM-dd HH:MM:ss");
        }
        private string GetMySQLDateTimeString(string unixString)
        {
            return GetString(GetDateTimeFromUnixString(unixString));
        }
        private DateTime GetDateTimeFromUnixString(string unixString)
        {
            return new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(Convert.ToDouble(unixString));
        }
        private string GetMySQLClanString(Player player)
        {
            if (String.IsNullOrEmpty(player.ClanDBID))
                return "NULL";
            else
                return $"'{player.ClanDBID}'";
        }

        private List<MoETank> GetUpdatedMoETankList(List<MoETank> MoETanks, string serverID, string appID)
        {
            try
            {
                JObject jTankData = GetJObjectFromAPI("wot/encyclopedia/vehicles/", serverID, appID, "en");
                JObject jTankopedia = GetJObjectFromAPI("wot/encyclopedia/info/", serverID, appID, "en");

                Dictionary<string, string> nationDict = GetNationDictionary(jTankopedia);
                Dictionary<string, string> tankDict = GetTankTypeDictionary(jTankopedia);
                List<Tank> manuallyAddedTanks = GetManualTankList(nationDict, tankDict);

                #region manually update missing tenks
                foreach (MoETank moeTank in MoETanks)
                {
                    if (manuallyAddedTanks.Any(x => x.TankIDNumeric == moeTank.Tank.TankIDNumeric))
                    {
                        moeTank.Tank = manuallyAddedTanks.First(x => x.TankIDNumeric == moeTank.Tank.TankIDNumeric);
                    }
                }
                #endregion

                #region update premium status
                foreach (MoETank moeTank in MoETanks)
                {
                    if (jTankData["data"][moeTank.Tank.TankIDNumeric] != null)
                        moeTank.Tank.IsPremium = (bool)jTankData["data"][moeTank.Tank.TankIDNumeric]["is_premium"];
                }
                #endregion

                #region update nation type and tank type
                foreach (MoETank moeTank in MoETanks)
                {
                    //Log.AddResult($"Updating tank information for tank {moeTank.Tank.TankIDNumeric}");

                    if (!String.IsNullOrEmpty(moeTank.Tank.NationID))
                    {
                        if (nationDict.ContainsKey(moeTank.Tank.NationID))
                            moeTank.Tank.Nation = nationDict[moeTank.Tank.NationID];
                        else
                            Log.AddWarning($"Could not find nationID {moeTank.Tank.NationID} from tank {moeTank.Tank.TankIDNumeric} in nation dict");
                    }
                    else
                    {
                        Log.AddWarning($"Nation ID of tank {moeTank.Tank.TankIDNumeric} is null/empty");
                    }

                    if (!String.IsNullOrEmpty(moeTank.Tank.TankTypeID))
                    {
                        if (tankDict.ContainsKey(moeTank.Tank.TankTypeID))
                            moeTank.Tank.TankType = tankDict[moeTank.Tank.TankTypeID];
                        else
                            Log.AddWarning($"Could not find nationID {moeTank.Tank.TankTypeID} from tank {moeTank.Tank.TankIDNumeric} in tank type dict");
                    }
                    else
                    {
                        Log.AddWarning($"Tank type ID of tank {moeTank.Tank.TankIDNumeric} is null or empty, trying to fix it");

                        if (!String.IsNullOrEmpty(moeTank.Tank.TankType) && tankDict.ContainsValue(moeTank.Tank.TankType))
                        {
                            moeTank.Tank.TankTypeID = tankDict.First(x => x.Value == moeTank.Tank.TankType).Key;
                            Log.AddInfo($"Fixed tank type ID of tank {moeTank.Tank.TankIDNumeric}");
                        }
                        else
                        {
                            Log.AddError($"Cannot fix null/empty tank type of tank {moeTank.Tank.TankIDNumeric}!");
                        }
                    }
                }
                #endregion
            }
            catch (Exception excp)
            {
                Log.AddError("Cannot update tank information", excp);
            }

            return MoETanks;
        }
        #endregion

        #region get file names
        private string GetMoEFailedPlayersRetryFileName(string serverID)
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Retry-MoEFailedPlayers-" + serverID + ".xml");
        }
        private string GetMoETanksRetryFileName(string serverID)
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Retry-MoETanks-" + serverID + ".xml");
        }
        private string GetMoEPlayersWithoutAnswerRetryFileName(string serverID)
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Retry-MoEPlayersWithoutAnswer-" + serverID + ".xml");
        }
        private string GetPlayerIDListRetryFileName(string serverID)
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Retry-MoEPlayers-" + serverID + ".xml");
        }

        private string GetIDListFileName(string serverID)
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ids_" + serverID + ".txt");
        }
        private string GetMoETanksFileName(string serverID)
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MoETanks-" + serverID + ".xml");
        }
        private string GetIDListToCheckFileName(string serverID)
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PlayerIDList-" + serverID + ".xml");
        }
        private string GetFailedIDListFileName(string serverID)
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PlayerIDListFailed-" + serverID + ".xml");
        }
        private string GetNewIDListFileName(string serverID)
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "new_ids-" + serverID + ".txt");
        }
        private string GetIDListWithoutAnswerFileName(string serverID)
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PlayerIDListWithoutAnswer-" + serverID + ".xml");
        }
        private string GetMoEFailedPlayersFileName(string serverID)
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MoEFailedPlayers-" + serverID + ".xml");
        }
        private string GetMoEPlayersWithoutAnswerFileName(string serverID)
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MoEPlayersWithoutAnswer-" + serverID + ".xml");
        }
        private string GetPlayerIDListFileName(string serverID)
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MoEPlayers-" + serverID + ".xml");
        }
        private string GetSkippedPlayerIDListFileName(string serverID)
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MoESkippedPlayers-" + serverID + ".xml");
        }
        private string GetSkippedClanIDListFileName(string serverID)
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MoESkippedClans-" + serverID + ".xml");
        }

        private string GetWN8SkippedTanksFileName(string serverID)
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "WN8SkippedTanks-" + serverID + ".xml");
        }

        private string GetPlayerListFileName(string serverID)
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Player Data-" + serverID + ".xml");
        }
        private string GetClanListFileName(string serverID)
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Clan Data-" + serverID + ".xml");
        }
        #endregion

        #region load data
        public T ReadObjectFromFile<T>(string fileName)
        {
            XmlSerializer mySerializer = new XmlSerializer(typeof(T));
            using (FileStream myFileStream = new FileStream(fileName, FileMode.Open))
                return (T)mySerializer.Deserialize(myFileStream);
        }
        #endregion

        #region update tank data
        public void HandleTankInformationUpdate(string serverID, string appID, string languageID, bool retry)
        {
            Log.AddResult($"Updating tank information, retry={retry}");
            string moeTankFilename = retry ? GetMoETanksRetryFileName(serverID) : GetMoETanksFileName(serverID);
            Log.AddResult($"Loading MoE tank list from file: {moeTankFilename}");
            List<MoETank> MoETanks = ReadObjectFromFile<List<MoETank>>(moeTankFilename);

            try
            {
                JObject jTankData = GetJObjectFromAPI("wot/encyclopedia/vehicles/", serverID, appID, languageID);
                JObject jTankopedia = GetJObjectFromAPI("wot/encyclopedia/info/", serverID, appID, languageID);

                Dictionary<string, string> nationDict = GetNationDictionary(jTankopedia);
                Dictionary<string, string> tankDict = GetTankTypeDictionary(jTankopedia);
                List<Tank> manuallyAddedTanks = GetManualTankList(nationDict, tankDict);

                #region manually update missing tenks
                foreach (MoETank moeTank in MoETanks)
                {
                    if (manuallyAddedTanks.Any(x => x.TankIDNumeric == moeTank.Tank.TankIDNumeric))
                    {
                        moeTank.Tank = manuallyAddedTanks.First(x => x.TankIDNumeric == moeTank.Tank.TankIDNumeric);
                    }
                }
                #endregion

                #region update premium status
                foreach(MoETank moeTank in MoETanks)
                {
                    if (jTankData["data"][moeTank.Tank.TankIDNumeric] != null)
                        moeTank.Tank.IsPremium = (bool)jTankData["data"][moeTank.Tank.TankIDNumeric]["is_premium"];
                }
                #endregion

                #region update nation type and tank type
                foreach (MoETank moeTank in MoETanks)
                {
                    Log.AddResult($"Updating tank information for tank {moeTank.Tank.TankIDNumeric}");

                    if (!String.IsNullOrEmpty(moeTank.Tank.NationID))
                    {
                        if (nationDict.ContainsKey(moeTank.Tank.NationID))
                            moeTank.Tank.Nation = nationDict[moeTank.Tank.NationID];
                        else
                            Log.AddWarning($"Could not find nationID {moeTank.Tank.NationID} from tank {moeTank.Tank.TankIDNumeric} in nation dict");
                    }
                    else
                    {
                        Log.AddWarning($"Nation ID of tank {moeTank.Tank.TankIDNumeric} is null/empty");
                    }

                    if (!String.IsNullOrEmpty(moeTank.Tank.TankTypeID))
                    {
                        if (tankDict.ContainsKey(moeTank.Tank.TankTypeID))
                            moeTank.Tank.TankType = tankDict[moeTank.Tank.TankTypeID];
                        else
                            Log.AddWarning($"Could not find nationID {moeTank.Tank.TankTypeID} from tank {moeTank.Tank.TankIDNumeric} in tank type dict");
                    }
                    else
                    {
                        Log.AddWarning($"Tank type ID of tank {moeTank.Tank.TankIDNumeric} is null or empty, trying to fix it");

                        if (!String.IsNullOrEmpty(moeTank.Tank.TankType) && tankDict.ContainsValue(moeTank.Tank.TankType))
                        {
                            moeTank.Tank.TankTypeID = tankDict.First(x => x.Value == moeTank.Tank.TankType).Key;
                            Log.AddInfo($"Fixed tank type ID of tank {moeTank.Tank.TankIDNumeric}");
                        }
                        else
                        {
                            Log.AddError($"Cannot fix null/empty tank type of tank {moeTank.Tank.TankIDNumeric}!");
                        }
                    }
                }
                #endregion
            }
            catch (Exception excp)
            {
                Log.AddError("Cannot update tank information", excp);
            }

            SaveObjectToJsonFile(MoETanks, moeTankFilename);
        }
        #endregion

        #region get player and clan info
        private void HandlePlayerAndClanInfo(string serverID, string appID, bool useRetryData, bool useFailedIDs)
        {
            string playerIDListFilename = useRetryData ? GetPlayerIDListRetryFileName(serverID) : GetPlayerIDListFileName(serverID);
            List<string> totalResultText = new List<string>();

            List<string> failedClanIDs = new List<string>();

            if (useFailedIDs)
            {
                playerIDListFilename = GetSkippedPlayerIDListFileName(serverID);
                failedClanIDs = ReadObjectFromFile<List<string>>(GetSkippedClanIDListFileName(serverID));
            }

            failedClanIDs.ForEach(x => Clans.Add(new Clan(x)));

            List<string> playerIDs = ReadObjectFromFile<List<string>>(playerIDListFilename);

            Log.AddResult("Initializing wn8 skipped tank list and player list");
            WN8SkippedTanksIDs = new List<string>();
            currentlyCheckingIDs = new List<string>();
            Players = new List<Player>();
            Stopwatch stopWatch = new Stopwatch();

            Log.AddResult("Creating Player objects");
            foreach(string id in playerIDs)
            {
                Players.Add(new Player(id));
            }

            #region get player info
            Log.AddResult("Creating list of player sub lists (100 players each)");
            List<List<Player>> sublistlist = splitList(Players, 100);
            runningAsyncs = 0;
            totalRequests = sublistlist.Count;

            Log.AddResult($"About to send {totalRequests} requests for the sub lists with max {runningAsyncsMax} running parallel");
            Log.AddResult("Starting to send requests for player sub lists");
            foreach (List<Player> sublist in sublistlist)
            {
                stopWatch = Stopwatch.StartNew();
                while (runningAsyncs >= runningAsyncsMax)
                {
                    Thread.Sleep(5);
                }
                stopWatch.Stop();

                if (stopWatch.ElapsedMilliseconds > 0)
                    Log.AddInfo($"Waited {stopWatch.ElapsedMilliseconds} ms for a request to finish");

                WebClient client = new WebClient();
                client.Encoding = Encoding.UTF8;
                client.Proxy = null;
                client.DownloadStringCompleted += Client_PlayerNames_DownloadStringCompleted;

                //string requesturl = String.Format("https://api.worldoftanks.{0}/wot/account/info/?application_id={1}&account_id={2}", GetAPISuffix(serverID), appID, String.Join(",", sublist.Select(x => x.AccountDBID)));
                string requesturl = $@"https://api.worldoftanks.{GetAPISuffix(serverID)}/wot/account/info/?application_id={appID}&fields=statistics.all%2Cclient_language%2Cglobal_rating%2Clogout_at%2Ccreated_at%2Clast_battle_time%2Cupdated_at%2Cclan_id%2Cnickname&account_id={String.Join(",", sublist.Select(x => x.AccountDBID))}";
                //Log.AddInfo("Adding sublist to checking list");
                sublist.ForEach(x => currentlyCheckingIDs.Add(x.AccountDBID));

                Log.AddInfo($"Sending request ({runningAsyncs + 1}/{runningAsyncsMax}) to API: " + requesturl);
                client.DownloadStringAsync(new Uri(requesturl), String.Join(",", sublist.Select(x => x.AccountDBID)));
                runningAsyncs++;
            }
            #endregion

            #region wait for player info to finish
            Stopwatch waitingWatch = Stopwatch.StartNew();

            while (runningAsyncs > 0)
            {
                Log.AddInfo($"Waiting for {runningAsyncs} player sub list requests to finish");
                Thread.Sleep(5000);

                if (waitingWatch.Elapsed.TotalMinutes >= 10)
                {
                    Log.AddInfo($"Waited long enough, won't wait any longer on {runningAsyncs} running requests");
                    break;
                }
            }

            waitingWatch = Stopwatch.StartNew();

            while (processingRequests > 0)
            {
                Log.AddInfo($"Waiting for {processingRequests} player sub list requests to be processed");
                Thread.Sleep(5000);

                if (waitingWatch.Elapsed.TotalMinutes >= 10)
                {
                    Log.AddInfo($"Waited long enough, won't wait any longer on {processingRequests} requests still to be processed");
                    break;
                }
            }

            if (currentlyCheckingIDs.Count > 0)
            {
                Log.AddResult("Did not get requests and/or processed them for these player IDs:");
                Log.AddResult(String.Join(";", currentlyCheckingIDs));

                totalResultText.Add("Did not get requests and/or processed them for these player IDs:");
                totalResultText.Add(String.Join(";", currentlyCheckingIDs));
            }

            if (failedIDs.Count > 0)
            {
                Log.AddResult("Requests parsing/getting failed for these player IDs:");
                Log.AddResult(String.Join(";", failedIDs));

                totalResultText.Add("Requests parsing/getting failed for these player IDs:");
                totalResultText.Add(String.Join(";", failedIDs));
            }
            #endregion

            #region get data for wn8 and calculate
            Log.AddResult("Getting wn8 expected values");
            ExpectedValues = GetExpectedValues();
                        
            currentRequests = 0;

            Log.AddResult("Starting to send request to calculate player's wn8 for sublists");
            foreach (List<Player> sublist in sublistlist)
            {
                stopWatch = Stopwatch.StartNew();
                while (runningAsyncs >= runningAsyncsMax)
                {
                    Thread.Sleep(5);
                }
                stopWatch.Stop();

                if (stopWatch.ElapsedMilliseconds > 0)
                    Log.AddInfo($"Waited {stopWatch.ElapsedMilliseconds} ms for a request to finish");

                WebClient client = new WebClient();
                client.Encoding = Encoding.UTF8;
                client.Proxy = null;
                client.DownloadStringCompleted += Client_WN8Stuff_DownloadStringCompleted;

                string requesturl = String.Format("https://api.worldoftanks.{0}/wot/account/tanks/?application_id={1}&account_id={2}", GetAPISuffix(serverID), appID, String.Join(",", sublist.Select(x => x.AccountDBID)));

                //Log.AddInfo("Adding sublist to currently checking ids");
                sublist.ForEach(x => currentlyCheckingIDs.Add(x.AccountDBID));

                Log.AddInfo($"Sending request ({runningAsyncs + 1}/{runningAsyncsMax}) to API: " + requesturl);
                client.DownloadStringAsync(new Uri(requesturl), String.Join(",", sublist.Select(x => x.AccountDBID)));
                runningAsyncs++;
            }
            #endregion

            #region wait for wn8 calculation to finish
            waitingWatch = Stopwatch.StartNew();

            while (runningAsyncs > 0)
            {
                Log.AddInfo($"Waiting for {runningAsyncs} player wn8 calc requests to finish");
                Thread.Sleep(5000);

                if (waitingWatch.Elapsed.TotalMinutes >= 10)
                {
                    Log.AddInfo($"Waited long enough, won't wait any longer on {runningAsyncs} running requests");
                    break;
                }
            }

            waitingWatch = Stopwatch.StartNew();

            while (processingRequests > 0)
            {
                Log.AddInfo($"Waiting for {processingRequests} player wn8 calc requests to be processed");
                Thread.Sleep(5000);

                if (waitingWatch.Elapsed.TotalMinutes >= 10)
                {
                    Log.AddInfo($"Waited long enough, won't wait any longer on {processingRequests} requests still to be processed");
                    break;
                }
            }

            if (currentlyCheckingIDs.Count > 0)
            {
                Log.AddResult("Did not get requests and/or processed them for these player IDs:");
                Log.AddResult(String.Join(";", currentlyCheckingIDs));
            }

            if (failedIDs.Count > 0)
            {
                Log.AddResult("Requests parsing/getting failed for these player IDs:");
                Log.AddResult(String.Join(";", failedIDs));
            }
            #endregion

            #region clans
            Log.AddResult("Creating distinct clan list");
            Clans = Clans.Distinct().ToList();
            //Clans = Clans.GroupBy(x => x.DBID).Select(y => y.First()).ToList();
            Log.AddResult("Creating list of clan sub lists (100 clans each");
            List<List<Clan>> subclanlistlists = splitList(Clans, 100);

            currentRequests = 0;
            totalRequests = subclanlistlists.Count;

            Log.AddResult($"About to send {totalRequests} requests for clan sub lists with max {runningAsyncsMax} parallel");
            Log.AddResult("Starting to send requests for clan sub lists");
            foreach (List<Clan> sublist in subclanlistlists)
            {
                stopWatch = Stopwatch.StartNew();
                while (runningAsyncs >= runningAsyncsMax)
                {
                    Thread.Sleep(5);
                }
                stopWatch.Stop();

                if (stopWatch.ElapsedMilliseconds > 0)
                    Log.AddInfo($"Waited {stopWatch.ElapsedMilliseconds} ms for a request to finish");

                WebClient client = new WebClient();
                client.Encoding = Encoding.UTF8;
                client.Proxy = null;
                client.DownloadStringCompleted += Client_Clans_DownloadStringCompleted;

                string requesturl = String.Format("https://api.worldoftanks.{0}/wgn/clans/info/?application_id={1}&clan_id={2}", GetAPISuffix(serverID), appID, String.Join(",", sublist.Select(x => x.DBID)));

                //Log.AddInfo("Adding clan sub list ids to currently checking ids");
                sublist.ForEach(x => currentlyCheckingIDs.Add(x.DBID));

                Log.AddInfo($"Sending request ({runningAsyncs + 1}/{runningAsyncsMax}) to API: " + requesturl);
                client.DownloadStringAsync(new Uri(requesturl), String.Join(",", sublist.Select(x => x.DBID)));
                runningAsyncs++;
            }

            #region wait for clan sublists to finish
            waitingWatch = Stopwatch.StartNew();

            while (runningAsyncs > 0)
            {
                Log.AddInfo($"Waiting for {runningAsyncs} clan sublist requests to finish");
                Thread.Sleep(5000);

                if (waitingWatch.Elapsed.TotalMinutes >= 10)
                {
                    Log.AddInfo($"Waited long enough, won't wait any longer on {runningAsyncs} running requests");
                    break;
                }
            }

            waitingWatch = Stopwatch.StartNew();

            while (processingRequests > 0)
            {
                Log.AddInfo($"Waiting for {processingRequests} clan sublist requests to be processed");
                Thread.Sleep(5000);

                if (waitingWatch.Elapsed.TotalMinutes >= 10)
                {
                    Log.AddInfo($"Waited long enough, won't wait any longer on {processingRequests} requests still to be processed");
                    break;
                }
            }

            if (currentlyCheckingIDs.Count > 0)
            {
                Log.AddResult("Did not get requests and/or processed them for these clan IDs:");
                Log.AddResult(String.Join(";", currentlyCheckingIDs));

                totalResultText.Add("Did not get requests and/or processed them for these clan IDs:");
                totalResultText.Add(String.Join(";", currentlyCheckingIDs));
            }

            if (failedIDs.Count > 0)
            {
                Log.AddResult("Requests parsing/getting failed for these clan IDs:");
                Log.AddResult(String.Join(";", failedIDs));

                totalResultText.Add("Requests parsing/getting failed for these clan IDs:");
                totalResultText.Add(String.Join(";", failedIDs));
            }
            #endregion

            Log.AddResult("Removing clans with empty/whitespace clan tag");
            Clans.RemoveAll(x => String.IsNullOrWhiteSpace(x.Tag));
            #endregion

            #region write skipped wn8 tank ids to file
            Log.AddResult("Writing tanks skipped for wn8 calculation to file; " + GetWN8SkippedTanksFileName(serverID));
            StreamWriter sw = new StreamWriter(GetWN8SkippedTanksFileName(serverID));
            WN8SkippedTanksIDs.ForEach(x => sw.WriteLine(x));
            sw.Close();
            #endregion

            #region fix stuff if use failed ids
            if (useFailedIDs)
            {
                List<Player> TotalPlayers = ReadObjectFromFile<List<Player>>(GetPlayerListFileName(serverID));
                List<Clan> TotalClans = ReadObjectFromFile<List<Clan>>(GetClanListFileName(serverID));

                TotalPlayers = TotalPlayers.Where(x => !Players.Any(z => z.AccountDBID == x.AccountDBID)).ToList();
                TotalPlayers.ForEach(x => Players.Add(x));

                TotalClans = TotalClans.Where(x => !Clans.Any(z => z.DBID == x.DBID)).ToList();
                TotalClans.ForEach(x => Clans.Add(x));
            }
            #endregion

            SavePlayerAndClanData(serverID);

            if (!useFailedIDs)
                SaveObjectToJsonFile(failedIDs, GetSkippedPlayerIDListFileName(serverID));


            totalResultText.ForEach(x => Log.AddInfo(x));

            // test stuff
            List<Player> playersWithoutClanInList = Players.Where(x => !String.IsNullOrEmpty(x.ClanDBID) && !Clans.Any(z => z.DBID == x.ClanDBID)).ToList();
        }


        private void Client_Clans_DownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            Log.AddInfo("Received request for clan sublist");
            processingRequests++;
            runningAsyncs--;

            string[] ids = e.UserState.ToString().Split(',');

            if (e.Error == null)
            {
                try
                {
                    if (!String.IsNullOrEmpty(e.UserState.ToString()) && !String.IsNullOrEmpty(e.Result) && e.Result != "{}")
                    {
                        #region parse data
                                               
                        string jsonstring = e.Result;
                        //requestresponses.Remove(id);

                        JObject jobj = JObject.Parse(jsonstring);

                        foreach (string id in ids)
                        {
                            //Log.AddInfo(String.Format("Checking clan {0}", id));
                            Clan c = Clans.First(x => x.DBID == id);

                            c.Name = jobj["data"][c.DBID]["name"].ToString();
                            c.Tag = jobj["data"][c.DBID]["tag"].ToString();
                            c.MemberCount = GetInt(jobj["data"][c.DBID]["members_count"]);
                            c.ColorHex = jobj["data"][c.DBID]["color"].ToString();
                            c.UpdatedAt = jobj["data"][c.DBID]["updated_at"].ToString();
                            c.ClanIcon24pxUrl = jobj["data"][c.DBID]["emblems"]["x24"]["portal"].ToString();
                            c.ClanIcon32pxUrl = jobj["data"][c.DBID]["emblems"]["x32"]["portal"].ToString();
                            c.ClanIcon64pxUrl = jobj["data"][c.DBID]["emblems"]["x64"]["portal"].ToString();
                            c.ClanIcon195pxUrl = jobj["data"][c.DBID]["emblems"]["x195"]["portal"].ToString();
                            c.ClanIcon256pxUrl = jobj["data"][c.DBID]["emblems"]["x256"]["wowp"].ToString();
                        }

                        Log.AddInfo($"Processed request for clan sublist from {ids.First()} to {ids.Last()}");

                        currentRequests++;
                        #endregion
                    }
                    else
                    {
                        Log.AddError("String returned from request is not a valid json string");
                        ids.ToList().ForEach(x => failedIDs.Add(x));
                    }
                }
                catch (Exception excp)
                {
                    Log.AddError("Error processing list of clans", excp);
                    Log.AddInfo("Json: " + e.Result);
                    ids.ToList().ForEach(x => failedIDs.Add(x));
                }
            }
            else
            {
                Log.AddError("Request returned error", e.Error);
                ids.ToList().ForEach(x => failedIDs.Add(x));
            }

            foreach(string id in ids)
            {
                if (currentlyCheckingIDs.Contains(id))
                {
                    currentlyCheckingIDs.Remove(id);
                }
                else
                    Log.AddWarning($"Clan {id} not found in currently checking id list");
            }

            processingRequests--;
            ReportRequestProgress("Processed request for clan sublist");
            //Log.AddInfo($"Finished processing clan sublist request: {currentRequests:N0}/{totalRequests:N0}");
        }
        private void Client_PlayerNames_DownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            Log.AddInfo("Received request for sublist");
            runningAsyncs--;
            processingRequests++;

            string[] ids = e.UserState.ToString().Split(',');

            if (e.Error == null)
            {
                //try
                //{
                if (!String.IsNullOrEmpty(e.UserState.ToString()) && !String.IsNullOrEmpty(e.Result) && e.Result != "{}")
                {
                    #region parse data
                    string jsonstring = e.Result;
                    //requestresponses.Remove(id);

                    JObject jobj = JObject.Parse(jsonstring);

                    foreach (string id in ids)
                    {
                        if (jobj["data"][id] != null && jobj["data"][id].HasValues)
                        {
                            Player p = Players.First(x => x.AccountDBID == id);

                            p.Name = jobj["data"][p.AccountDBID]["nickname"].ToString();
                            p.Battles = GetInt(jobj["data"][p.AccountDBID]["statistics"]["all"]["battles"]);
                            p.WinRate = GetDouble(jobj["data"][p.AccountDBID]["statistics"]["all"]["wins"]) / p.Battles;
                            p.ClientLanguage = jobj["data"][p.AccountDBID]["client_language"].ToString();
                            p.WGRating = GetInt(jobj["data"][p.AccountDBID]["global_rating"]);
                            p.LastLogout = jobj["data"][p.AccountDBID]["logout_at"].ToString();
                            p.AccountCreated = jobj["data"][p.AccountDBID]["created_at"].ToString();
                            p.LastBattle = jobj["data"][p.AccountDBID]["last_battle_time"].ToString();
                            p.UpdatedAt = jobj["data"][p.AccountDBID]["updated_at"].ToString();

                            p.WN8Data.Damage = Convert.ToDouble(jobj["data"][p.AccountDBID]["statistics"]["all"]["damage_dealt"].ToString());
                            p.WN8Data.Decap = Convert.ToDouble(jobj["data"][p.AccountDBID]["statistics"]["all"]["dropped_capture_points"].ToString());
                            p.WN8Data.Kills = Convert.ToDouble(jobj["data"][p.AccountDBID]["statistics"]["all"]["frags"].ToString());
                            p.WN8Data.Spots = Convert.ToDouble(jobj["data"][p.AccountDBID]["statistics"]["all"]["spotted"].ToString());
                            p.WN8Data.Wins = Math.Round(p.WinRate * p.Battles);

                            if (jobj["data"][p.AccountDBID]["clan_id"] != null)
                            {
                                p.ClanDBID = jobj["data"][p.AccountDBID]["clan_id"].ToString();

                                if (!String.IsNullOrWhiteSpace(p.ClanDBID))
                                {
                                    Clans.Add(new Clan(p.ClanDBID));
                                }
                            }
                        }
                        else
                        {
                            Player p = new Player();

                            if (Players.Any(x => x.AccountDBID == id))
                            {
                                p = Players.First(x => x.AccountDBID == id);
                                p.Name = "#MysteryPlayerByAPI#";
                            }
                            else
                            {
                                p.AccountDBID = id;
                                p.Name = "#MysteryPlayerByAPI#";
                                Players.Add(p);
                            }

                            Log.AddError($"Value for player with id {id} is null");
                            failedIDs.Add(id);
                        }
                    }

                    Log.AddInfo($"Checked players from {ids.First()} to {ids.Last()}");

                    currentRequests++;
                    #endregion
                }
                else
                {
                    Log.AddError("String returned from request is not a valid json string");
                    ids.ToList().ForEach(x => failedIDs.Add(x));
                }
                //}
                //catch (Exception excp)
                //{
                //    Log.AddError("Error processing list of players", excp);
                //    Log.AddInfo("Json: " + e.Result);
                //    ids.ToList().ForEach(x => failedIDs.Add(x));
                //}
            }
            else
            {
                Log.AddError("Request returned error", e.Error);
                ids.ToList().ForEach(x => failedIDs.Add(x));
            }

            foreach (string id in ids)
            {
                if (currentlyCheckingIDs.Contains(id))
                {
                    currentlyCheckingIDs.Remove(id);
                }
                else
                    Log.AddWarning($"Player {id} not found in currently checking id list");
            }

            processingRequests--;
            ReportRequestProgress("Processed request for player sublist");
        }
        private void Client_WN8Stuff_DownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            Log.AddInfo("Received request for sublist wn8 calculations");
            runningAsyncs--;
            processingRequests++;

            string[] ids = e.UserState.ToString().Split(',');

            if (e.Error == null)
            {
                try
                {
                    if (!String.IsNullOrEmpty(e.UserState.ToString()) && !String.IsNullOrEmpty(e.Result) && e.Result != "{}")
                    {
                        #region parse data
                        string jsonstring = e.Result;
                        //requestresponses.Remove(id);

                        JObject jobj = JObject.Parse(jsonstring);

                        foreach (string id in ids)
                        {
                            //Log.AddInfo(String.Format("parsing wn8 data for player {0}", id));
                            Player p = Players.First(x => x.AccountDBID == id);

                            WN8Data expecteddata = new WN8Data();

                            foreach (JObject jtank in jobj["data"][id].Children<JObject>())
                            {
                                if (ExpectedValues.Any(x => x.NumericID == jtank["tank_id"].ToString()))
                                {
                                    ExpectedValueItem item = ExpectedValues.First(x => x.NumericID == jtank["tank_id"].ToString());
                                    double battles = Convert.ToDouble(jtank["statistics"]["battles"]);

                                    expecteddata.Damage += item.Damage * battles;
                                    expecteddata.Decap += item.DecapPoints * battles;
                                    expecteddata.Kills += item.Kills * battles;
                                    expecteddata.Spots += item.Spots * battles;
                                    expecteddata.Wins += item.WinRate * battles;
                                }
                                else
                                {
                                    if (!WN8SkippedTanksIDs.Contains(jtank.GetString("tank_id")))
                                        WN8SkippedTanksIDs.Add(jtank.GetString("tank_id"));
                                }
                            }

                            p.WN8Data.WN8 = p.WN8Data.CalculateWN8(expecteddata);
                        }

                        Log.AddInfo($"Calculated wn8 for players in sublist, from {ids.First()} to {ids.Last()}");
                        currentRequests++;
                        #endregion
                    }
                    else
                    {
                        Log.AddError("String returned from request is not a valid json string");
                        ids.ToList().ForEach(x => failedIDs.Add(x));
                    }
                }
                catch (Exception excp)
                {
                    Log.AddError("Error processing list of players for wn8", excp);
                    Log.AddInfo("Json: " + e.Result);
                    ids.ToList().ForEach(x => failedIDs.Add(x));
                }
            }
            else
            {
                Log.AddError("Request returned error", e.Error);
                ids.ToList().ForEach(x => failedIDs.Add(x));
            }

            foreach (string id in ids)
            {
                if (currentlyCheckingIDs.Contains(id))
                {
                    currentlyCheckingIDs.Remove(id);
                }
                else
                    Log.AddWarning($"Player {id} not found in currently checking id list");
            }

            processingRequests--;
            ReportRequestProgress("Finished processing wn8 request");
            //Log.AddInfo($"Finished processing wn8 request: {currentRequests:N0}/{totalRequests:N0}");
        }

        private void ReportRequestProgress(string message)
        {
            if (currentRequests > 0)
            {
                TimeSpan ts = new TimeSpan(0, 0, Convert.ToInt32(Math.Ceiling((totalRequests - currentRequests) * Log.Stopwatch.Elapsed.TotalSeconds / currentRequests)));
                Log.AddResult($"{message}: {currentRequests:N0}/{totalRequests:N0} ({currentRequests / Convert.ToDouble(totalRequests):P2}) eta: {ts.Days:00}d:{ts.Hours:00}h:{ts.Minutes:00}m:{ts.Seconds:00}s");
            }
        }

        private List<List<Player>> splitList(List<Player> sourcelist, int nSize = 100)
        {
            List<List<Player>> list = new List<List<Player>>();

            for (int i = 0; i < sourcelist.Count; i += nSize)
            {
                list.Add(sourcelist.GetRange(i, Math.Min(nSize, sourcelist.Count - i)));
            }

            return list;
        }

        private List<List<Clan>> splitList(List<Clan> sourcelist, int nSize = 100)
        {
            List<List<Clan>> list = new List<List<Clan>>();

            for (int i = 0; i < sourcelist.Count; i += nSize)
            {
                list.Add(sourcelist.GetRange(i, Math.Min(nSize, sourcelist.Count - i)));
            }

            return list;
        }

        private void SavePlayerAndClanData(string serverID)
        {
            Log.AddInfo(String.Format("Saving player and clan lists to json files: {0} and {1}", GetPlayerListFileName(serverID), GetClanListFileName(serverID)));
            SaveObjectToJsonFile(Players, GetPlayerListFileName(serverID));
            SaveObjectToJsonFile(Clans, GetClanListFileName(serverID));
        }
        private List<ExpectedValueItem> GetExpectedValues()
        {
            WebClient client = new WebClient();

            List<ExpectedValueItem> ExpectedValues = new List<ExpectedValueItem>();

            try
            {
                Log.AddInfo("Downloading expected values json file");
                string wn8Data = client.DownloadString(@"http://www.wnefficiency.net/exp/expected_tank_values_latest.json");
                Log.AddInfo("Creating JObject for expected values");
                JObject baseobj = JObject.Parse(wn8Data);


                foreach (JObject jo in baseobj["data"].Children())
                {
                    ExpectedValueItem item = new ExpectedValueItem();

                    //long typedescr = Convert.ToInt64(jo.GetString("IDNum"));

                    //double id_tank = typedescr >> 8 & 65535;
                    //double id_nation = typedescr >> 4 & 15;

                    //item.NumID_Tank = id_tank.ToString();
                    //item.NumID_Nation = id_nation.ToString();
                    item.NumericID = jo.GetString("IDNum");
                    Log.AddInfo("Adding expected values item for tank id " + item.NumericID);

                    item.Kills = jo.GetDouble("expFrag");
                    item.Damage = jo.GetDouble("expDamage");
                    item.Spots = jo.GetDouble("expSpot");
                    item.DecapPoints = jo.GetDouble("expDef");
                    item.WinRate = jo.GetDouble("expWinRate") / 100;

                    ExpectedValues.Add(item);
                }
            }
            catch(Exception excp)
            {
                Log.AddError("Error while getting wn8 expected values", excp);
            }

            return ExpectedValues;
        }
        #endregion

        #region get player id list
        private void HandlePlayerIDListGathering(string serverID, string appID, double endID)
        {
            HandlePlayerIDListGathering(serverID, appID, GetStartIDByServer(serverID), endID);
        }
        private void HandlePlayerIDListGathering(string serverID, string appID)
        {
            HandlePlayerIDListGathering(serverID, appID, GetStartIDByServer(serverID), GetEndIDByServer(serverID));
        }
        private void HandlePlayerIDListGathering(string serverID, string appID, double startID, double endID)
        {
            bool retry = false;

            currentRequests = 0;
            totalRequests = Convert.ToInt32(Math.Ceiling((endID - startID) / 100));

            currentlyCheckingIDLists = new ConcurrentDictionary<string, List<double>>();
            failedIDLists = new Dictionary<string, List<double>>();
            failedIDs = new List<string>();

            currentServerID = serverID;
            Stopwatch stopWatch = new Stopwatch();
            WebClientDict = new Dictionary<string, WebClientInfoItem>();

            List<double> currentList = new List<double>();
            string requestUrl = @"https://api.worldoftanks.{1}/wot/account/info/?application_id={0}&fields=last_battle_time&account_id={2}";
            
            runningAsyncs = 0;
            playerIDsToCheck = new List<string>();
            double currentStartID = startID;

            for (double d = startID; d <= endID; d++)
            {
                currentList.Add(d);

                if (currentList.Count == 100 || d == endID) // 100
                {
                    string id = $"{currentStartID}-{d}";
                    currentStartID = d;

                    stopWatch.Restart();
                    while (runningAsyncs >= runningAsyncsMax)
                    {
                        Thread.Sleep(20);

                        if (stopWatch.ElapsedMilliseconds > MoEStatic.MaxMillisecondRunTimeOfRequest)
                            CheckWebClientsMaxRuntime();


                        Log.AddInfo($"Currently waiting for {stopWatch.ElapsedMilliseconds} ms for a request to finish");
                    }
                    stopWatch.Stop();

                    if (stopWatch.ElapsedMilliseconds > 0)
                        Log.AddInfo(String.Format("Waited {0} ms for an request to get back", stopWatch.ElapsedMilliseconds));

                    if (currentRequests % MoEStatic.ClearInterval == 0 && currentRequests > 0)
                        ClearWebClientsDictFromFinishedItems();

                    WebClient client = new WebClient();
                    client.Encoding = Encoding.UTF8;
                    client.Proxy = null;
                    client.DownloadStringCompleted += Client_PlayerIDs_DownloadStringCompleted;

                    AddIDListToCurrentlyCheckingIDs(currentList);
                    WebClientDict.Add(id, new WebClientInfoItem(client));
                    Log.AddInfo("Sending request to API: " + String.Format(requestUrl, appID, GetAPISuffix(serverID), String.Join("%2C", currentList)));
                    client.DownloadStringAsync(new Uri(String.Format(requestUrl, appID, GetAPISuffix(serverID), String.Join("%2C", currentList))), currentList);
                    runningAsyncs++;

                    currentList = new List<double>();
                }
            }

            #region wait for stuff to finish
            Stopwatch waitingWatch = Stopwatch.StartNew();

            while (runningAsyncs > 0)
            {
                Log.AddInfo($"Waiting for {runningAsyncs} requests to finish");
                Thread.Sleep(5000);

                if (waitingWatch.Elapsed.TotalMinutes >= 10)
                {
                    Log.AddInfo($"Waited long enough, won't wait any longer on {runningAsyncs} running requests");
                    break;
                }
            }

            waitingWatch = Stopwatch.StartNew();

            while (processingRequests > 0)
            {
                Log.AddInfo($"Waiting for {processingRequests} requests to be processed");
                Thread.Sleep(5000);

                if (waitingWatch.Elapsed.TotalMinutes >= 10)
                {
                    Log.AddResult($"Waited long enough, won't wait any longer on {processingRequests} requests still to be processed");
                    break;
                }
            }

            if (currentlyCheckingIDs.Count > 0)
            {
                Log.AddResult("Did not get requests and/or processed them for these IDs:");
                Log.AddResult(String.Join(";", currentlyCheckingIDs));
            }
            #endregion

            Log.AddResult("Writing final results to log");
            LogResultsPlayerIDs();

            Log.AddResult("Making all lists disinct where needed (MoETanks' players, player ids to check)");

            SavePlayerIDData(serverID, retry);
        }

        private DateTime GetDateTimeFromTimeStamp(double timestamp)
        {
            DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            return dtDateTime.AddSeconds(timestamp);
        }

        private void Client_PlayerIDs_DownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            runningAsyncs--;
            List<double> idList = (List<double>)e.UserState;

            if (idList.Count == 0)
            {
                Log.AddError("Encountered empty idList in async string download completed event");
            }
            else
            {
                string listIdentifier = GetIdentifier(idList);

                if (e.Cancelled)
                {
                    Log.AddError($"Request for IDs in {listIdentifier} was cancelled, adding ID to list with failed IDs");
                    AddIDListToFailedIDs(idList);
                    RemoveIDListFromCurrentlyCheckingIDs(idList);
                }
                else
                {
                    Log.AddInfo($"Request answer received for IDlist {listIdentifier}");

                    processingRequests++;

                    if (e.Error == null)
                    {
                        try
                        {
                            if (!String.IsNullOrEmpty(e.Result) && e.Result != "{}")
                            {
                                currentRequests++;

                                #region parse data                    
                                string jsonstring = e.Result;

                                JObject jobj = JObject.Parse(jsonstring);

                                foreach (double d in idList)
                                {
                                    string id = d.ToString();

                                    if (jobj["data"] != null && jobj["data"][id] != null)
                                    {
                                        if (jobj["data"][id].GetType() == typeof(JObject))
                                        {
                                            double timeStamp = Convert.ToDouble(jobj["data"][id]["last_battle_time"]);
                                            DateTime lastBattleDateTime = GetDateTimeFromTimeStamp(timeStamp);

                                            if (PlayedAfterMoEIntroduction(lastBattleDateTime, currentServerID))
                                            {
                                                playerIDsToCheck.Add(id);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        failedIDs.Add(id);
                                        Log.AddError($"Failed to get/parse data for player {id}, json is null");

                                        if (jobj["error"] != null)
                                            Log.AddError($"Error trying to get player's account data: {jobj["error"]}");
                                    }
                                }
                                
                                //Log.AddInfo(String.Format("Checked player IDs from {0} to {1}", idList.First(), idList.Last()));
                                #endregion

                                ReportRequestProgress($"Checked IDs in {listIdentifier}");
                            }
                            else
                            {
                                Log.AddError($"Invalid Result for IDlist {listIdentifier}");
                                AddIDListToFailedIDs(idList);
                            }
                        }
                        catch (Exception excp)
                        {
                            AddIDListToFailedIDs(idList);
                            Log.AddError($"Failed to get/parse data for idList {listIdentifier}", excp);
                        }
                    }
                    else
                    {
                        Log.AddError($"An error occuring in the request for idList {listIdentifier}", e.Error);
                        AddIDListToFailedIDs(idList);
                    }

                    RemoveIDListFromCurrentlyCheckingIDs(idList);

                    processingRequests--;
                    Log.AddInfo($"Finished string download event for idList {listIdentifier}");
                }
            }
        }

        private string GetIdentifier(List<double> idList)
        {
            if (idList == null)
            {
                Log.AddError("Trying to get identifier of null ID list");
                return "nullList";
            }
            else
            {
                if (idList.Count > 0)
                    return $"{idList.First()}-{idList.Last()}";
                else
                {
                    Log.AddWarning("Getting identifier for empty double list");
                    return "emptyList";
                }
            }
        }

        private void AddIDListToFailedIDs(List<double> idList)
        {
            if (idList == null)
            {
                Log.AddError("Trying to add null ID list to failed IDs");
            }
            else
            {
                if (idList.Count > 0)
                {
                    string id = GetIdentifier(idList);

                    if (failedIDLists == null)
                    {
                        Log.AddError("Failed ID Lists dict is null");
                    }
                    else
                    {
                        if (failedIDLists.ContainsKey(id))
                        {
                            Log.AddWarning($"Already found a list with identifier {id} in failed ID lists, not adding anything");
                        }
                        else
                        {
                            failedIDLists.Add(id, idList);
                        }
                    }
                    //if (failedIDs == null)
                    //{
                    //    Log.AddError("currently failed IDs is null, cannot add items to it");
                    //}
                    //else
                    //{
                    //    idList.ForEach(x => failedIDs.Add(x.ToString()));
                    //}
                }
                else
                {
                    Log.AddError("Tried to add empty ID list to failed IDs");
                }
            }
        }
        private void AddIDListToCurrentlyCheckingIDs(List<double> idList)
        {
            if (idList == null)
            {
                Log.AddError("Trying to add null ID list to currently checking IDs");
            }
            else
            {
                if (idList.Count > 0)
                {
                    string id = GetIdentifier(idList);

                    if (currentlyCheckingIDLists == null)
                    {
                        Log.AddError("Failed ID Lists dict is null");
                    }
                    else
                    {
                        if (!currentlyCheckingIDLists.TryAdd(id, idList))
                            Log.AddWarning($"Could not add IDlist {id} to currently checking ID lists");

                        //if (currentlyCheckingIDLists.ContainsKey(id))
                        //{
                        //    Log.AddWarning($"Already found a list with identifier {id} in currently checking ID lists, not adding anything");
                        //}
                        //else
                        //{
                        //    currentlyCheckingIDLists.TryAdd(id, idList);
                        //}
                    }
                    //if (currentlyCheckingIDs == null)
                    //{
                    //    Log.AddError("currently checking IDs is null, cannot add items to it");
                    //}
                    //else
                    //{
                    //    idList.ForEach(x => currentlyCheckingIDs.Add(x.ToString()));
                    //}
                }
                else
                {
                    Log.AddError("Tried to add empty ID list to currently checking IDs");
                }
            }
        }
        private void RemoveIDListFromCurrentlyCheckingIDs(List<double> idList)
        {
            if (idList == null)
            {
                Log.AddError("Trying to remove null ID list from currently checking IDs");
            }
            else
            {
                if (idList.Count > 0)
                {
                    string id = GetIdentifier(idList);

                    if (currentlyCheckingIDLists == null)
                    {
                        Log.AddError("Failed ID Lists dict is null");
                    }
                    else
                    {
                        List<double> outList;
                        if (!currentlyCheckingIDLists.TryRemove(id, out outList))
                            Log.AddWarning($"Could not remove idList {id} form currently checking IDs");
                        //if (currentlyCheckingIDLists.ContainsKey(id))
                        //{
                        //    currentlyCheckingIDLists.Remove(id);
                        //}
                        //else
                        //{
                        //    Log.AddWarning($"Did not find a list with identifier {id} in currently checking ID lists, not removing anything");
                        //}
                    }
                    //foreach (double d in idList)
                    //{
                    //    if (currentlyCheckingIDs == null)
                    //    {
                    //        Log.AddError("currently checking IDs is null, cannot remove items from it");
                    //    }
                    //    else
                    //    {
                    //        if (currentlyCheckingIDs.Contains(d.ToString()))
                    //        {
                    //            currentlyCheckingIDs.Remove(d.ToString());
                    //        }
                    //        else
                    //        {
                    //            Log.AddWarning($"Did not find id {d} in currently checking IDs");
                    //        }
                    //    }
                    //}

                    Log.AddInfo($"Removed idList {GetIdentifier(idList)} from currently checking IDs");
                }
                else
                {
                    Log.AddError("Tried to remove empty ID list from currently checking IDs");
                }
            }
        }

        private bool PlayedAfterMoEIntroduction(DateTime lastBattleDateTime, string serverID)
        {
            DateTime moeIntroductionDateTime = MoEStatic.GetMoEIntroductionDateTimeFromServerID(serverID);

            return DateTime.Compare(lastBattleDateTime, moeIntroductionDateTime) >= 0;
        }

        private void SavePlayerIDData(string serverID, bool retry)
        {
            //Log.AddInfo(String.Format("Saving player IDs to check ({0}) and IDs failed to be checked ({1})", GetIDListToCheckFileName(serverID), GetFailedIDListFileName(serverID)));
            Log.AddResult("Saving new ID list, failed IDs and IDs without answer to file");
            //SaveObjectToJsonFile(playerIDsToCheck, GetIDListToCheckFileName(serverID));
            SaveIDListToTxtFile(playerIDsToCheck, GetNewIDListFileName(serverID));

            List<string> completeFailedIDs = new List<string>();
            List<string> completeCheckedIDs = new List<string>();

            foreach (KeyValuePair<string, List<double>> kvp in failedIDLists)
            {
                if (kvp.Value != null)
                    kvp.Value.ForEach(x => completeFailedIDs.Add(x.ToString()));
            }

            completeFailedIDs.AddRange(failedIDs);
            foreach (KeyValuePair<string, List<double>> kvp in currentlyCheckingIDLists)
            {
                if (kvp.Value != null)
                    kvp.Value.ForEach(x => completeCheckedIDs.Add(x.ToString()));
            }

            completeCheckedIDs = completeCheckedIDs.Distinct().ToList();
            completeFailedIDs = completeFailedIDs.Distinct().ToList();

            SaveObjectToJsonFile(completeFailedIDs, GetFailedIDListFileName(serverID));
            SaveObjectToJsonFile(completeCheckedIDs, GetIDListWithoutAnswerFileName(serverID));
        }
        #endregion

        #region build tank list
        private List<Tank> GetManualTankList(string serverID, string appID, string languageID)
        {
            JObject jTankopedia = GetJObjectFromAPI("wot/encyclopedia/info/", serverID, appID, languageID);
            Dictionary<string, string> nationDict = GetNationDictionary(jTankopedia);
            Dictionary<string, string> tankDict = GetTankTypeDictionary(jTankopedia);

            return GetManualTankList(nationDict, tankDict);
        }
        private List<Tank> GetManualTankList(Dictionary<string,string> nationDict, Dictionary<string,string> tankDict)
        {
            List<Tank> tanks = new List<Tank>();

            //tanks.Add(new Tank("14353", "Aufklärungspanzer Panther", "Aufkl.Panther", MoEStatic.NationGermany, MoEStatic.TankTypeLightTank, 7, false, "Auf_Panther"));
            //tanks.Add(new Tank("57121", "M46 Patton KR", "M46 KR", MoEStatic.NationUSA, MoEStatic.TankTypeMediumTank, 8, false, "A63_M46_Patton_KR"));
            //tanks.Add(new Tank("50961", "leKpz M 41 90 mm [GF]", "M 41 90 GF", MoEStatic.NationGermany, MoEStatic.TankTypeLightTank, 8, true, "G120_M41_90_GrandFinal"));
            tanks.Add(new Tank("64017", "leKpz M 41 90 mm", "M 41 90", MoEStatic.NationGermany, MoEStatic.TankTypeLightTank, 8, true, "G120_M41_90", "http://static-ptl-eu.gcdn.co/static/2.50.0/encyclopedia/tankopedia/vehicle/contour/germany-g120_m41_90.png", "http://static-ptl-eu.gcdn.co/static/2.50.0/encyclopedia/tankopedia/vehicle/small/germany-g120_m41_90.png", "http://static-ptl-eu.gcdn.co/static/2.50.0/encyclopedia/tankopedia/vehicle/germany-g120_m41_90.png"));
            tanks.Add(new Tank("62977", "T-44-100 (R)", "T-44-100 (R)", MoEStatic.NationUSSR, MoEStatic.TankTypeMediumTank, 8, true, "R127_T44_100_P", "http://static-ptl-eu.gcdn.co/static/2.50.0/encyclopedia/tankopedia/vehicle/contour/ussr-r127_t44_100_p.png", "http://static-ptl-eu.gcdn.co/static/2.50.0/encyclopedia/tankopedia/vehicle/small/ussr-r127_t44_100_p.png", "http://static-ptl-eu.gcdn.co/static/2.50.0/encyclopedia/tankopedia/vehicle/ussr-r127_t44_100_p.png"));
            //tanks.Add(new Tank("19217", "Grille 15", "Grille 15", MoEStatic.NationGermany, MoEStatic.TankTypeTD, 10, false, ""));
            tanks.Add(new Tank("62721", "Kirovets-1", "Kirovets-1", MoEStatic.NationUSSR, MoEStatic.TankTypeHeavyTank, 8, true, "R123_Kirovets_1", "http://static-ptl-eu.gcdn.co/static/2.50.0/encyclopedia/tankopedia/vehicle/contour/ussr-r123_kirovets_1.png", "http://static-ptl-eu.gcdn.co/static/2.50.0/encyclopedia/tankopedia/vehicle/small/ussr-r123_kirovets_1.png", "http://static-ptl-eu.gcdn.co/static/2.50.0/encyclopedia/tankopedia/vehicle/ussr-r123_kirovets_1.png"));
            tanks.Add(new Tank("57377", "T25 Pilot Number 1", "T25 Pilot 1", MoEStatic.NationUSA, MoEStatic.TankTypeMediumTank, 8, true, "A111_T25_Pilot", "http://static-ptl-eu.gcdn.co/static/2.50.0/encyclopedia/tankopedia/vehicle/contour/usa-a111_t25_pilot.png", "http://static-ptl-eu.gcdn.co/static/2.50.0/encyclopedia/tankopedia/vehicle/small/usa-a111_t25_pilot.png", "http://static-ptl-eu.gcdn.co/static/2.50.0/encyclopedia/tankopedia/vehicle/usa-a111_t25_pilot.png"));
            //tanks.Add(new Tank("16913", "Waffenträger auf E 100", "WT auf E 100", MoEStatic.NationGermany, MoEStatic.TankTypeTD, 10, false, ""));
            //tanks.Add(new Tank("62993", "VK 45.03", "VK 45.03", MoEStatic.NationGermany, MoEStatic.TankTypeHeavyTank, 7, true, ""));
            tanks.Add(new Tank("62017", "AMX M4 mle. 49 Liberté", "AMX M4 49 L", MoEStatic.NationFrance, MoEStatic.TankTypeHeavyTank, 8, true, "F74_AMX_M4_1949_Liberte", "http://static-ptl-eu.gcdn.co/static/2.50.0/encyclopedia/tankopedia/vehicle/contour/france-f74_amx_m4_1949_liberte.png", "http://static-ptl-eu.gcdn.co/static/2.50.0/encyclopedia/tankopedia/vehicle/small/france-f74_amx_m4_1949_liberte.png", "http://static-ptl-eu.gcdn.co/static/2.50.0/encyclopedia/tankopedia/vehicle/france-f74_amx_m4_1949_liberte.png"));
            tanks.Add(new Tank("63233", "KV-4 Kreslavskiy", "KV-4 Kresl.", MoEStatic.NationUSSR, MoEStatic.TankTypeHeavyTank, 8, true, "R128_KV4_Kreslavskiy", "http://static-ptl-eu.gcdn.co/static/2.50.0/encyclopedia/tankopedia/vehicle/contour/ussr-r128_kv4_kreslavskiy.png", "http://static-ptl-eu.gcdn.co/static/2.50.0/encyclopedia/tankopedia/vehicle/small/ussr-r128_kv4_kreslavskiy.png", "http://static-ptl-eu.gcdn.co/static/2.50.0/encyclopedia/tankopedia/vehicle/ussr-r128_kv4_kreslavskiy.png"));
            tanks.Add(new Tank("56401", "T95/Chieftain", "T95/Chieftain", MoEStatic.NationUK, MoEStatic.TankTypeHeavyTank, 10, true, "GB88_T95_Chieftain_turret", "http://static-ptl-eu.gcdn.co/static/2.50.0/encyclopedia/tankopedia/vehicle/contour/uk-gb88_t95_chieftain_turret.png", "http://static-ptl-eu.gcdn.co/static/2.50.0/encyclopedia/tankopedia/vehicle/small/uk-gb88_t95_chieftain_turret.png", "http://static-ptl-eu.gcdn.co/static/2.50.0/encyclopedia/tankopedia/vehicle/uk-gb88_t95_chieftain_turret.png"));

            tanks.ForEach(x => x.Nation = nationDict.ContainsKey(x.NationID) ? nationDict[x.NationID] : "#Unknown");
            tanks.ForEach(x => x.TankType = tankDict.ContainsKey(x.TankTypeID) ? tankDict[x.TankTypeID] : "#Unknown");

            return tanks;
        }

        private List<Tank> GetTankListFromAPI(string serverID, string appID, string languageID)
        {
            List<Tank> tanks = new List<Tank>();

            try
            {
                JObject jTankData = GetJObjectFromAPI("wot/encyclopedia/vehicles/", serverID, appID, languageID);
                JObject jTankopedia = GetJObjectFromAPI("wot/encyclopedia/info/", serverID, appID, languageID);

                Dictionary<string, string> nationDict = GetNationDictionary(jTankopedia);
                Dictionary<string, string> tankDict = GetTankTypeDictionary(jTankopedia);

                if (jTankData != null)
                {
                    if (jTankData["data"] != null)
                    {
                        foreach (JProperty jTank in jTankData["data"].Children<JProperty>())
                        {
                            Log.AddInfo(String.Format("Adding Tank {0}", jTank.Value["name"].ToString()));
                            tanks.Add(new Tank(jTank, nationDict, tankDict));
                        }
                    }
                    else
                    {
                        Log.AddWarning("Cannot get tank data");
                        if (jTankData["error"] != null)
                            Log.AddError("Error trying to get tank data: " + jTankData["error"].ToString());
                    }
                }
                else
                {
                    Log.AddWarning("Trying to get tank list from null jobject");
                }

                // adding missing tanks from tenkopedia cause wuck gargaming
                Log.AddInfo("Manually adding tanks missing from the api");
                foreach(Tank tank in GetManualTankList(serverID,appID,languageID).Where(x=>!tanks.Any(z=>z.TankIDNumeric==x.TankIDNumeric)))
                {
                    tanks.Add(tank);
                }
                //GetManualTankList(serverID, appID, languageID).ForEach(x => tanks.Add(x));
            }
            catch (Exception excp)
            {
                Log.AddError("Cannot get tank list", excp);
            }

            return tanks;
        }

        private Dictionary<string, string> GetNationDictionary(JObject jTankopedia)
        {
            Log.AddInfo("Getting nation dictionary from the API");

            Dictionary<string, string> nationDict = new Dictionary<string, string>();

            try
            {
                if (jTankopedia != null && jTankopedia["data"]["vehicle_nations"] != null)
                {
                    foreach (JProperty jNation in jTankopedia["data"]["vehicle_nations"].Children<JProperty>())
                    {
                        Log.AddInfo(String.Format("Adding nation {0}", jNation.Name));
                        nationDict.Add(jNation.Name, jNation.Value.ToString());
                    }
                }
                else
                {
                    Log.AddWarning("Trying to get nation dict from null JObject, returning empty dict");
                }
            }
            catch(Exception excp)
            {
                Log.AddError("Error getting nation dictionary", excp);
            }

            return nationDict;
        }

        private Dictionary<string, string> GetTankTypeDictionary(JObject jTankopedia)
        {
            Log.AddInfo("Getting tank type dictionary from the API");

            Dictionary<string, string> tankTypeDict = new Dictionary<string, string>();

            try
            {
                if (jTankopedia != null && jTankopedia["data"]["vehicle_nations"] != null)
                {
                    foreach (JProperty jTankType in jTankopedia["data"]["vehicle_types"].Children<JProperty>())
                    {
                        Log.AddInfo(String.Format("Adding tank type {0}", jTankType.Name));
                        tankTypeDict.Add(jTankType.Name, jTankType.Value.ToString());
                    }
                }
                else
                {
                    Log.AddWarning("Trying to get tankType dict from null JObject, returning empty dict");
                }
            }
            catch (Exception excp)
            {
                Log.AddError("Error getting tank type dictionary", excp);
            }

            return tankTypeDict;
        }
        #endregion

        #region get string from API
        private string GetStringFromAPI(string requestString, string serverID, string appID, string languageID)
        {
            try
            {
                string requestUrl = String.Format("https://api.worldoftanks.{0}/{1}", GetAPISuffix(serverID), requestString);

                if (requestString.Contains("?"))
                    requestUrl += String.Format("&application_id={0}", appID);
                else
                    requestUrl += String.Format("?application_id={0}", appID);

                requestUrl += "&language="+ languageID;

                Log.AddInfo("Sending request to API: " + requestUrl);

                WebClient client = new WebClient();
                client.Encoding = Encoding.UTF8;
                client.Proxy = null;

                string data = client.DownloadString(requestUrl);

                return data;
            }
            catch(Exception excp)
            {
                Log.AddError("Error getting the string fromt the API", excp);
                return null;
            }
        }
        #endregion

        #region helper
        private string GetAPISuffix(string serverID)
        {
            if (serverID == MoEStatic.ServerIDEU)
                return "eu";
            else if (serverID == MoEStatic.ServerIDASIA)
                return "asia";
            else if (serverID == MoEStatic.ServerIDUS)
                return "com";
            else if (serverID == MoEStatic.ServerIDRU)
                return "ru";
            else
            {
                Log.AddWarning("Invalid serverID provided for API suffix: " + serverID);
                return "";
            }
        }
        private string GetAPIKeyFromSuffix(string serverID)
        {
            if (serverID == MoEStatic.ServerIDEU)
                return MoEStatic.appIDEU;
            else if (serverID == MoEStatic.ServerIDASIA)
                return MoEStatic.appIDASIA;
            else if (serverID == MoEStatic.ServerIDUS)
                return MoEStatic.appIDUS;
            else if (serverID == MoEStatic.ServerIDRU)
                return MoEStatic.appIDRU;
            else
            {
                Log.AddWarning("Invalid serverID provided for API key: " + serverID);
                // invalid server id
                return "";
            }
        }

        private JObject GetJObjectFromString(string data)
        {
            if (data != null)
            {
                //try
                //{
                    return JObject.Parse(data);
                //}
                //catch (Exception excp)
                //{
                //    Log.AddError("Error getting JObject from string", excp);
                //    Log.AddInfo("Json: " + data);
                //    return null;
                //}
            }
            else
            {
                Log.AddWarning("Trying to get JObject from null string");
                return null;
            }
        }
        private JObject GetJObjectFromAPI(string requestString, string serverID, string appID, string languageID)
        {
            return GetJObjectFromString(GetStringFromAPI(requestString, serverID, appID, languageID));
        }

        private string GetString(object o)
        {
            if (o != null)
                return o.ToString();
            else
            {
                Log.AddWarning("Trying to convert null object to string");
                return String.Empty;
            }
        }
        private double GetDouble(object o)
        {
            if (o != null)
                return Convert.ToDouble(GetString(o));
            else
            {
                Log.AddWarning("Trying to convert null object to double");
                return 0;
            }
        }
        private int GetInt(object o)
        {
            if (o != null)
                return Convert.ToInt32(GetString(o));
            else
            {
                Log.AddWarning("Trying to convert null object to int");
                return 0;
            }
        }
        #endregion
    }

    public class WebClientInfoItem
    {
        public WebClient WebClient { get; set; }
        public Stopwatch Stopwatch { get; set; }

        public WebClientInfoItem(WebClient webclient)
        {
            WebClient = webclient;
            Stopwatch = Stopwatch.StartNew();
        }
    }

    public static class MoEExtensions
    {
        #region log
        public static bool PassesLogLevel(this MoELog log, string type)
        {
            return MoELog.LogLevelMapping[type] <= log.LogLevel;                    
        }
        public static void WriteLog(this MoELog log)
        {
            if (!String.IsNullOrEmpty(log.LogText))
                File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Log.txt"), log.LogText);
        }
        public static void AddLine(this MoELog log, string line, string type)
        {
            log.AddLine(line, type, log.AdditionalOutputToConsole);
        }
        public static void AddLine(this MoELog log, string line, string type, bool additionalOutputToConsole)
        {
            if (PassesLogLevel(log, type))
            {
                if (additionalOutputToConsole)
                    Console.WriteLine(String.Format("{0}\t{1}\t{2}", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss.fff"), type, line));

                if (!log.OnlyOutputToConsole)
                    log.LogText += String.Format("{0}\t{1}\t{2}\n", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss.fff"), type, line);
            }
        }
        public static void AddError(this MoELog log, string line, Exception excp)
        {
            log.AddError(line);

            if (excp != null)
            {
                if (excp.Source != null)
                    log.AddError(String.Format("Source: {0}", excp.Source));

                int i = 1;


                if (excp.Message != null)
                {
                    if (excp.Message.Split('\n').Length > 1)
                    {
                        log.AddError("Message:");

                        foreach (string s in excp.Message.Split('\n'))
                        {
                            log.AddError(String.Format("{0:00}: {1}", i, s));
                            i++;
                        }
                    }
                    else
                        log.AddError(String.Format("Message: {0}", excp.Message));
                }

                if (excp.StackTrace != null)
                {
                    if (excp.StackTrace.Split('\n').Length > 1)
                    {
                        log.AddError("StackTrace:");

                        i = 1;

                        foreach (string s in excp.StackTrace.Split('\n'))
                        {
                            log.AddError(String.Format("{0:00}: {1}", i, s));
                            i++;
                        }
                    }
                    else
                        log.AddError(String.Format("Message: {0}", excp.StackTrace));
                }

                if (excp.TargetSite != null)
                {
                    log.AddError("Target Site:");
                    log.AddError(excp.TargetSite.ToString());
                }

                if (excp.InnerException != null)
                {
                    log.AddError("Inner Exception:", excp.InnerException);
                }
            }
        }
        public static void AddError(this MoELog log, string line)
        {
            log.AddLine(line, MoELog.TypeError);
        }
        public static void AddInfo(this MoELog log, string line)
        {
            log.AddLine(line, MoELog.TypeInfo);
        }
        public static void AddResult(this MoELog log, string line)
        {
            log.AddLine(line, MoELog.TypeResult);
        }
        public static void AddWarning(this MoELog log, string line)
        {
            log.AddLine(line, MoELog.TypeWarning);
        }
        #endregion
        #region json
        public static string GetString(this JObject jObj, string property)
        {
            return jObj[property].ToString();
        }
        public static double GetDouble(this JObject jObj, string property)
        {
            return Convert.ToDouble(jObj[property]);
        }
        public static bool GetBool(this JObject jObj, string property)
        {
            return jObj.GetString(property) == "true" || jObj.GetString(property) == "1";
        }

        public static string GetString(this JToken jToken, string property)
        {
            return jToken[property].ToString();
        }
        public static double GetDouble(this JToken jToken, string property)
        {
            return Convert.ToDouble(jToken[property]);
        }
        public static bool GetBool(this JToken jToken, string property)
        {
            return jToken.GetString(property) == "true" || jToken.GetString(property) == "1";
        }

        public static string GetString(this JProperty jProperty, string property)
        {
            return jProperty.Value.GetString(property);
        }
        public static double GetDouble(this JProperty jProperty, string property)
        {
            return Convert.ToDouble(jProperty.Value[property]);
        }
        public static bool GetBool(this JProperty jProperty, string property)
        {
            return jProperty.GetString(property) == "true" || jProperty.GetString(property) == "1";
        }
        #endregion
        #region wn8
        public static double CalculateWN8(this WN8Data wn8Data, WN8Data expecteddata)
        {
            double rDAMAGE = wn8Data.Damage / expecteddata.Damage;
            double rDEF = wn8Data.Decap / expecteddata.Decap;
            double rFRAG = wn8Data.Kills / expecteddata.Kills;
            double rSPOT = wn8Data.Spots / expecteddata.Spots;
            double rWIN = wn8Data.Wins / expecteddata.Wins;

            double rWINc = Math.Max(0, (rWIN - 0.71) / (1 - 0.71));
            double rDAMAGEc = Math.Max(0, (rDAMAGE - 0.22) / (1 - 0.22));
            double rFRAGc = Math.Max(0, Math.Min(rDAMAGEc + 0.2, (rFRAG - 0.12) / (1 - 0.12)));
            double rSPOTc = Math.Max(0, Math.Min(rDAMAGEc + 0.1, (rSPOT - 0.38) / (1 - 0.38)));
            double rDEFc = Math.Max(0, Math.Min(rDAMAGEc + 0.1, (rDEF - 0.10) / (1 - 0.10)));

            return Math.Ceiling(980 * rDAMAGEc + 210 * rDAMAGEc * rFRAGc + 155 * rFRAGc * rSPOTc + 75 * rDEFc * rFRAGc + 145 * Math.Min(1.8, rWINc));
        }
        #endregion

        public static string GetClanDBID(this Player player)
        {
            if (String.IsNullOrEmpty(player.ClanDBID))
                return "0";
            else
                return player.ClanDBID;
        }

        public static int GetWins(this Player player, MoELog log)
        {
            int wins = 0;

            if (int.TryParse(Math.Ceiling(player.Battles * player.WinRate).ToString(), out wins))
                return int.Parse(Math.Ceiling(player.Battles * player.WinRate).ToString());
            else
            {
                log.AddError($"Cannot convert wn8 of player {player.Name} to int");
                return -1;
            }
        }

        private static string GetTimeStamp(string unixTimeStamp)
        {
            DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dateTime = dateTime.AddSeconds(Convert.ToDouble(unixTimeStamp));

            return $"{dateTime.Year}-{dateTime.Month}-{dateTime.Day} {dateTime.Hour}:{dateTime.Minute}:{dateTime.Second}";
        }

        public static string GetLastBattleTimeStamp(this Player player)
        {
            return GetTimeStamp(player.LastBattle);
        }

        public static string GetAccountCreatedTimeStamp(this Player player)
        {
            return GetTimeStamp(player.AccountCreated);
        }

        public static int GetWN8(this Player player, MoELog log)
        {
            int wn8 = 0;
            if (int.TryParse(player.WN8Data.WN8.ToString(), out wn8))
                return int.Parse(player.WN8Data.WN8.ToString());
            else
            {
                log.AddError($"Cannot convert wn8 of player {player.Name} to int");
                return -1;
            }
        }
    }

    public class MoELog
    {
        public Stopwatch Stopwatch { get; set; }
        public string LogText { get; set; }
        public bool AdditionalOutputToConsole { get; set; }
        public bool OnlyOutputToConsole { get; set; }
        public int LogLevel { get; set; }

        public static string TypeError = "Error";
        public static string TypeWarning = "Warning";
        public static string TypeInfo = "Info";
        public static string TypeResult = "Result";
        public static Dictionary<string, int> LogLevelMapping = new Dictionary<string, int>() { { TypeError, 0 }, { TypeWarning, 1 }, { TypeResult, 2 }, { TypeInfo, 3 } };

        public MoELog()
        {
            AdditionalOutputToConsole = true;
            Stopwatch = Stopwatch.StartNew();
        }

        public MoELog(bool onlyOutput, int logLevel)
        {
            AdditionalOutputToConsole = true;
            OnlyOutputToConsole = onlyOutput;
            LogLevel = logLevel;
            Stopwatch = Stopwatch.StartNew();
        }
    }

    public class Tank
    {
        public string Name { get; set; }
        public string NameShort { get; set; }
        public string TankIDNumeric { get; set; }
        public string Nation { get; set; }
        public string NationID { get; set; }
        public double Tier { get; set; }
        public string TankType { get; set; }
        public string TankTypeID { get; set; }
        public string Tag { get; set; }
        public bool IsPremium { get; set; }

        public string IconSmallUrl { get; set; }
        public string IconBigUrl { get; set; }
        public string IconContourUrl { get; set; }

        public Tank() { }

        public Tank(JProperty jTank, Dictionary<string,string> nationDict, Dictionary<string,string> tankDict)
        {
            TankIDNumeric = jTank.Name;

            Name = jTank.GetString("name");
            NameShort = jTank.GetString("short_name");

            NationID = jTank.GetString("nation");
            Nation = nationDict[NationID];

            Tag = jTank.GetString("tag");

            TankTypeID = jTank.GetString("type");
            TankType = tankDict[TankTypeID];

            Tier = jTank.GetDouble("tier");
            IsPremium = jTank.GetBool("is_premium");
            IconBigUrl = jTank.Value["images"]["big_icon"].ToString();
            IconContourUrl = jTank.Value["images"]["contour_icon"].ToString();
            IconSmallUrl = jTank.Value["images"]["small_icon"].ToString();
        }

        public Tank(string numericID, string name, string nameShort, string nation, string nationID, string tankType, string tankTypeID, double tier, bool isPremium, string tag)
        {
            TankIDNumeric = numericID;
            Name = name;
            NameShort = nameShort;
            Nation = nation;
            NationID = nationID;
            TankType = tankType;
            TankTypeID = tankTypeID;
            Tier = tier;
            IsPremium = isPremium;
            Tag = tag;
        }

        public Tank(string numericID, string name, string nameShort, string nationID, string tankTypeID, double tier, bool isPremium, string tag)
        {
            TankIDNumeric = numericID;
            Name = name;
            NameShort = nameShort;
            NationID = nationID;
            TankTypeID = tankTypeID;
            Tier = tier;
            IsPremium = isPremium;
            Tag = tag;
        }

        public Tank(string numericID, string name, string nameShort, string nationID, string tankTypeID, double tier, bool isPremium, string tag, string contourIconUrl, string smallIconUrl, string bigIconUrl)
        {
            TankIDNumeric = numericID;
            Name = name;
            NameShort = nameShort;
            NationID = nationID;
            TankTypeID = tankTypeID;
            Tier = tier;
            IsPremium = isPremium;
            Tag = tag;
            IconContourUrl = contourIconUrl;
            IconBigUrl = bigIconUrl;
            IconSmallUrl = smallIconUrl;
        }
    }

    public class MoETank
    {
        public Tank Tank { get; set; }

        private List<string> _Players_3MoE = new List<string>();
        public List<string> Players_3MoE
        {
            get { return _Players_3MoE; }
            set { _Players_3MoE = value; }
        }

        private List<string> _Players_2MoE = new List<string>();
        public List<string> Players_2MoE
        {
            get { return _Players_2MoE; }
            set { _Players_2MoE = value; }
        }

        //private Dictionary<double,double> _OwnerCount = new Dictionary<double, double>();
        //public Dictionary<double, double> OwnerCount
        //{
        //    get { return _OwnerCount; }
        //    set { _OwnerCount = value; }
        //}

        public MoETank()
        {
            Tank = new Tank();
            Players_3MoE = new List<string>();
            Players_2MoE = new List<string>();

            //OwnerCount = new Dictionary<double, double>();

            //OwnerCount.Add(0, 0);
            //OwnerCount.Add(1, 0);

            //for (double val = MoEStatic.OwnerCountStep; val <= MoEStatic.OwnerCountEnd; val += MoEStatic.OwnerCountStep)
            //{
            //    OwnerCount.Add(val, 0);
            //}
        }

        public MoETank(double maxOwnerCount, double ownerCountStep)
        {
            Tank = new Tank();
            Players_3MoE = new List<string>();
            Players_2MoE = new List<string>();

            //OwnerCount = new Dictionary<double, double>();

            //OwnerCount.Add(0, 0);
            //OwnerCount.Add(1, 0);

            //for (double val = ownerCountStep; val <= maxOwnerCount; val += ownerCountStep)
            //{
            //    OwnerCount.Add(val, 0);
            //}
        }
    }

    public class Player
    {
        [XmlAttribute("name")]
        public string Name { get; set; }
        [XmlAttribute("accountDBID")]
        public string AccountDBID { get; set; }
        [XmlAttribute("clientLang")]
        public string ClientLang { get; set; }
        [XmlAttribute("lastLogout")]
        public string LastLogout { get; set; }
        [XmlAttribute("b")]
        public double Battles { get; set; }
        [XmlAttribute("wr")]
        public double WinRate { get; set; }
        [XmlAttribute("wgrating")]
        public int WGRating { get; set; }
        public string LastBattle { get; set; }
        public string AccountCreated { get; set; }
        public string UpdatedAt { get; set; }
        public string ClanDBID { get; set; }
        public string ClientLanguage { get; set; }

        //public Clan Clan { get; set; }
        public WN8Data WN8Data { get; set; }

        public Player()
        {
            WN8Data = new WN8Data();
        }

        public Player(string accountDBID)
        {
            AccountDBID = accountDBID;
            WN8Data = new WN8Data();
        }
    }

    public class Clan
    {
        [XmlAttribute("name")]
        public string Name { get; set; }
        [XmlAttribute("id")]
        public string DBID { get; set; }
        [XmlAttribute("tag")]
        public string Tag { get; set; }
        [XmlAttribute("cHex")]
        public string ColorHex { get; set; }
        [XmlAttribute("members")]
        public double MemberCount { get; set; }
        public string UpdatedAt { get; set; }
        public string ClanIcon24pxUrl { get; set; }
        public string ClanIcon32pxUrl { get; set; }
        public string ClanIcon64pxUrl { get; set; }
        public string ClanIcon195pxUrl { get; set; }
        public string ClanIcon256pxUrl { get; set; }

        public Clan() { }

        public Clan(string dbID)
        {
            DBID = dbID;
        }
    }

    public class ExpectedValueItem
    {
        public string NumID_Tank { get; set; }
        public string NumID_Nation { get; set; }
        public string NumericID { get; set; }
        
        public double Kills { get; set; }
        public double Spots { get; set; }
        public double Damage { get; set; }
        public double WinRate { get; set; }
        public double DecapPoints { get; set; }

        public ExpectedValueItem()
        { }
    }

    public class WN8Data
    {
        [XmlAttribute("dmg")]
        public double Damage { get; set; }
        [XmlAttribute("k")]
        public double Kills { get; set; }
        [XmlAttribute("s")]
        public double Spots { get; set; }
        [XmlAttribute("d")]
        public double Decap { get; set; }
        [XmlAttribute("w")]
        public double Wins { get; set; }
        [XmlAttribute("wn8")]
        public double WN8 { get; set; }

        public WN8Data()
        { }
    }
}
