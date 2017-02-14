using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MoE_Console_Application
{
    public class MoEStatic
    {
        public static string appIDEU = @"f11400be09f2e5521774f24b5ab2dd32";
        public static string appIDUS = @"e3ff37de3ea9ed633994672f63dc423d";
        public static string appIDRU = @"f11400be09f2e5521774f24b5ab2dd32"; //@"ce394b94113b380cca816a9a5750d04b";
        public static string appIDASIA = @"476a5a90b386ce1454e655cf549a90c0";

        public static string appModeMoE = "MOE";
        public static string appModeMoERetry = "MOERETRY";
        public static string appModePlayer = "PLAYERS";
        public static string appModePlayerRetry = "PLAYERSRETRY";
        public static string appModePlayerFailed = "PLAYERSFAILED";
        public static string appModePlayerIDs = "IDS";
        public static string appModeUpdateTankInformation = "UPDATETANKS";
        public static string appModeDataBase = "DB";
        public static string appModeFixing = "FIX";
        public static string appModeIDDoubleChecking = "DOUBLECHECK";
        public static string appModeDockerDataBase = "DOCKERDB";

        public static string TankTypeLightTank = "lightTank";
        public static string TankTypeMediumTank = "mediumTank";
        public static string TankTypeHeavyTank = "heavyTank";
        public static string TankTypeTD = "AT-SPG";
        public static string TankTypeArty = "SPG";

        public static string NationUSSR = "ussr";
        public static string NationUSA = "usa";
        public static string NationGermany = "germany";
        public static string NationJapan = "japan";
        public static string NationUK = "uk";
        public static string NationFrance = "france";
        public static string NationChina = "china";
        public static string NationCzech = "czech";

        public static string ServerIDEU = "EU";
        public static string ServerIDUS = "US";
        public static string ServerIDRU = "RU";
        public static string ServerIDASIA = "ASIA";

        public static double OwnerCountEnd = 500;
        public static double OwnerCountStep = 50;
        public static bool SaveInBetween = false;

        public static double SaveInterval = 1000;
        public static double SaveIntervalIDList = 10000;
        public static double ClearInterval = 50;
        public static double ReportInterval = 100;
        public static double MaxMillisecondRunTimeOfRequest = 600000; //600000;

        public static string GetAPISuffix(string serverID)
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
                throw new ArgumentException($"Invalid serverID: \"{serverID}\"");
            }
        }

        public static bool PlayedAfterMoEIntroduction(DateTime lastBattleDateTime, string serverID)
        {
            DateTime moeIntroductionDateTime = MoEStatic.GetMoEIntroductionDateTimeFromServerID(serverID);

            return DateTime.Compare(lastBattleDateTime, moeIntroductionDateTime) >= 0;
        }

        public static DateTime GetMoEIntroductionDateTimeFromServerID(string serverID)
        {
            if (serverID == MoEStatic.ServerIDEU)
                return new DateTime(2014, 6, 11);
            else if (serverID == MoEStatic.ServerIDASIA)
                return new DateTime(2014, 6, 11); // ???
            else if (serverID == MoEStatic.ServerIDUS)
                return new DateTime(2014, 6, 17);
            else if (serverID == MoEStatic.ServerIDRU)
                return new DateTime(2014, 6, 11); // ???
            else
            {
                throw new ArgumentException($"Invalid serverID provided for MoE introduction DateTime: \"{serverID}\"");
            }
        }
    }
}
