using MoeFetcher.WgApi;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MoeFetcher
{
    class App
    {
        private const int SettingsLoadTryCount = 25;
        private const int TimeoutAfterFailedLoadTry = 200; //ms

        private WGApiClient Client;
        private Setting[] Settings;
        private Setting ActiveSetting;
        private ILogger Logger;

        public App(ILogger logger)
        {
            Logger = logger;
        }

        public int Run(string[] args)
        {
            if (false == InitClient())
                return (int)ExitCodes.Error;

            return (int)ExitCodes.Success;
        }

        private bool InitClient()
        {
            int remainingLoadTries = SettingsLoadTryCount;
            string basePath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().CodeBase);
            string settingsPath = Path.Combine(basePath, "settings.json");

            while (false == Setting.TryLoadSettings(settingsPath, out Settings))
            {
                if (remainingLoadTries-- > 0)
                    Thread.Sleep(TimeoutAfterFailedLoadTry);
                else
                {
                    Logger.CriticalError("could not open settings at {0}", settingsPath);
                    return false;
                }
            }
            if (Settings == null)
            {
                Logger.CriticalError("could not find settings at {0}. see settings.json.example", settingsPath);
                return false;
            }

            Logger.Info("settings loaded from {0}", settingsPath);

            //TODO: determine active setting

            Client = new WGApiClient(ActiveSetting.BaseUri, ActiveSetting.Region, ActiveSetting.ApplicationID);
            return true;
        }
    }
}
