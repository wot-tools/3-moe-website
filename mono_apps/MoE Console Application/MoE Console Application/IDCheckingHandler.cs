using MySql.Data.MySqlClient;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MoE_Console_Application
{
    class IDCheckingHandler
    {
        private string serverID;
        private string appID;
        private double startID;
        private double endID;
        private MoELog Log;

        private MySqlConnection dbConnection;

        private readonly int maxRunningAsyncs = 20;
        private readonly int maxListLength = 100;
        private readonly int webClientTimeOut = 60000;
        private readonly int maxRetryCount = 10;

        private int currentRequests;
        private int processingsRequestCount;
        private int totalRequests;
        private int runningAsyncs;

        public IDCheckingHandler(string _serverID, string _appID, double _startID, double _endID, MoELog _log)
        {
            serverID = _serverID;
            appID = _appID;
            startID = _startID;
            endID = _endID;
            Log = _log;
            dbConnection = MoEMySqlWrapper.GetMySqlConnection();
        }

        public void CheckAndWriteToDatabase()
        {
            currentRequests = 0;
            totalRequests = Convert.ToInt32(Math.Ceiling((endID - startID) / 100));

            Stopwatch stopWatch = new Stopwatch();

            List<double> currentList = new List<double>();
            //string requestUrl = @"https://api.worldoftanks.{1}/wot/account/info/?application_id={0}&fields=last_battle_time&account_id={2}";

            runningAsyncs = 0;
            double currentStartID = startID;

            for (double d = startID; d <= endID; d++)
            {
                currentList.Add(d);

                if (currentList.Count == 100 || d == endID) // 100
                {
                    string id = $"{currentStartID}-{d}";
                    currentStartID = d;

                    stopWatch.Restart();
                    while (runningAsyncs >= maxRunningAsyncs)
                    {
                        Thread.Sleep(20);

                        Log.AddInfo($"Currently waiting for {stopWatch.ElapsedMilliseconds} ms for a request to finish");
                    }
                    stopWatch.Stop();

                    if (stopWatch.ElapsedMilliseconds > 0)
                        Log.AddInfo(String.Format("Waited {0} ms for an request to get back", stopWatch.ElapsedMilliseconds));


                    CheckListOfPossiblePlayerIDs(currentList, 0);

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

            while (processingsRequestCount > 0)
            {
                Log.AddInfo($"Waiting for {processingsRequestCount} requests to be processed");
                Thread.Sleep(5000);

                if (waitingWatch.Elapsed.TotalMinutes >= 10)
                {
                    Log.AddResult($"Waited long enough, won't wait any longer on {processingsRequestCount} requests still to be processed");
                    break;
                }
            }
            #endregion

            Log.AddInfo($"Finished checking player IDs from {startID} to {endID} and writing them to the DB");
        }

        private void CheckListOfPossiblePlayerIDs(List<double> idList, int retryCount)
        {
            runningAsyncs++;

            WebClient client = new WebClient();
            client.Encoding = Encoding.UTF8;
            client.Proxy = null;
            client.DownloadStringCompleted += Client_PlayerIDs_DownloadStringCompleted;


            string requestURL = $@"https://api.worldoftanks.{MoEStatic.GetAPISuffix(serverID)}/wot/account/info/?application_id={appID}&fields=last_battle_time&account_id={String.Join("%2C", idList)}";
            Log.AddInfo($"Sending request to API: {requestURL}");
            client.DownloadStringAsync(new Uri(requestURL), new object[] { idList, retryCount });
        }

        private void Client_PlayerIDs_DownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            runningAsyncs--;
            object[] paramsArray = (object[])e.UserState;
            List<double> idList = (List<double>)paramsArray[0];
            int retryCount = (int)paramsArray[1];

            bool hasToRetryIDList = false;

            if (idList == null) { Log.AddError("Encountered null idList in async string download completed event"); return; }
            if (idList.Count == 0) { Log.AddError("Encountered empty idList in async string download completed event"); return; }

            string listIdentifier = GetIdentifier(idList);

            if (e.Cancelled) { Log.AddError($"Request for IDs in {listIdentifier} was cancelled!"); return; }
            
            Log.AddInfo($"Request answer received for IDlist {listIdentifier}");
            
            if (e.Error != null)
            {
                Log.AddError($"An error occuring in the request for idList {listIdentifier}", e.Error);
                hasToRetryIDList = true;
            }
            else
            {
                try
                {
                    if (!String.IsNullOrEmpty(e.Result) && e.Result != "{}")
                    {
                        currentRequests++;
                        processingsRequestCount++;

                        #region parse data                    
                        string jsonstring = e.Result;

                        JObject jobj = JObject.Parse(jsonstring);
                        List<string> playerIDsToCheck = new List<string>();

                        foreach (double d in idList)
                        {
                            string id = d.ToString();

                            if (jobj["data"] != null && jobj["data"][id] != null)
                            {
                                if (jobj["data"][id].GetType() == typeof(JObject))
                                {
                                    double timeStamp = Convert.ToDouble(jobj["data"][id]["last_battle_time"]);
                                    DateTime lastBattleDateTime = GetDateTimeFromTimeStamp(timeStamp);

                                    if (MoEStatic.PlayedAfterMoEIntroduction(lastBattleDateTime, serverID))
                                    {
                                        playerIDsToCheck.Add(id);
                                    }
                                }
                            }
                            else
                            {
                                //failedIDs.Add(id);
                                Log.AddError($"Failed to get/parse data for player {id}, json is null");

                                if (jobj["error"] != null)
                                    Log.AddError($"Error trying to get player's account data: {jobj["error"]}");
                            }
                        }

                        InsertPlayerIDsToDB(playerIDsToCheck);
                        #endregion

                        ReportRequestProgress($"Checked IDs in {listIdentifier}");
                        processingsRequestCount--;
                    }
                    else
                    {
                        Log.AddError($"Invalid Result for IDlist {listIdentifier}");
                        hasToRetryIDList = true;
                    }
                }
                catch (Exception excp)
                {
                    Log.AddError($"Failed to get/parse data for idList {listIdentifier}", excp);
                    hasToRetryIDList = true;
                }
            }
            
            Log.AddInfo($"Finished string download event for idList {listIdentifier}");

            if (hasToRetryIDList && retryCount < maxRetryCount)
            {
                Log.AddWarning($"Checking {listIdentifier} again, retry {retryCount++}/{maxRetryCount}");
                CheckListOfPossiblePlayerIDs(idList, retryCount++);
            }
        }

        private void InsertPlayerIDsToDB(List<string> playerIDs)
        {
            if (playerIDs.Count > 0)
            {
                MySqlCommand command = dbConnection.CreateCommand();

                command.CommandText = "INSERT IGNORE INTO players (id) VALUES ";
                command.CommandText += String.Join(",", playerIDs.Select(x => $"('{x}')"));
                command.CommandText += ";";
                Log.AddInfo($"Executing MySqlQuery: {command.CommandText}");
                command.ExecuteNonQuery();
            }
        }

        private DateTime GetDateTimeFromTimeStamp(double timestamp)
        {
            DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            return dtDateTime.AddSeconds(timestamp);
        }

        private void ReportRequestProgress(string message)
        {
            if (currentRequests > 0)
            {
                TimeSpan ts = new TimeSpan(0, 0, Convert.ToInt32(Math.Ceiling((totalRequests - currentRequests) * Log.Stopwatch.Elapsed.TotalSeconds / currentRequests)));
                Log.AddResult($"{message}: {currentRequests:N0}/{totalRequests:N0} ({currentRequests / Convert.ToDouble(totalRequests):P2}) eta: {ts.Days:00}d:{ts.Hours:00}h:{ts.Minutes:00}m:{ts.Seconds:00}s");
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
    }
}
