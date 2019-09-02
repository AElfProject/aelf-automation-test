using System;
using System.IO;
using System.Linq;
using AElf.Automation.Common.Helpers;

namespace AElf.Automation.GenerateNodesConfiguration
{
    class Program
    {
        private static readonly ILogHelper Logger = LogHelper.GetLogger();

        static void Main(string[] args)
        {
            //Init Logger
            var logName = "GenerateConfig" +
                          DateTime.Now.ToString("MMddHHmmss") + ".log";
            var dir = Path.Combine(CommonHelper.AppRoot, "logs", logName);
            Logger.InitLogHelper(dir);

            //check config
            var check = new ConfigCheck();
            check.CheckNodeName();
            check.CheckOtherNumbers();

            //delete old config files
            CommonHelper.DeleteDirectoryFiles(Path.Combine(CommonHelper.AppRoot, "results"));

            //gen all accounts
            var bps = ConfigInfoHelper.Config.BpNodes;
            var fulls = ConfigInfoHelper.Config.FullNodes;
            foreach (var node in bps.Concat(fulls))
            {
                //gen account
                var accountGen = new ConfigAccount(node);
                accountGen.GenerateAccount();
                accountGen.CopyAccount();
            }

            //get miners and boot config
            var info = new GenerateInformation();

            //gen bp configs
            foreach (var node in bps.Concat(fulls))
            {
                var configFile = new ConfigFiles(node);
                configFile.GenerateBasicConfigFile();
                configFile.GenerateSettingFile(info);
            }

            Logger.Info("Complete all config file generate.");
            Console.ReadLine();
        }
    }
}