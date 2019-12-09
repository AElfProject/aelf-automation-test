using System;
using System.IO;
using System.Linq;
using AElfChain.Common.Helpers;

namespace AElf.Automation.NodesConfigGen
{
    internal class Program
    {
        private static readonly ILogHelper Logger = LogHelper.GetLogger();

        private static void Main(string[] args)
        {
            //Init Logger
            var logName = "GenerateConfig" +
                          DateTime.Now.ToString("MMddHHmmss") + ".log";
            var dir = Path.Combine(CommonHelper.AppRoot, "logs", logName);
            Logger.InitLogHelper(dir);
            Log4NetHelper.LogInit();
            var logger = Log4NetHelper.GetLogger();

            //check config
            logger.Info("Check configuration setting parameters.");
            var check = new ConfigCheck();
            check.CheckNodeName();
            check.CheckOtherNumbers();

            //delete old config files
            logger.Info("Delete old config files.");
            CommonHelper.DeleteDirectoryFiles(Path.Combine(CommonHelper.AppRoot, "results"));

            //gen all accounts
            logger.Info("Generate all accounts for configuration.");
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