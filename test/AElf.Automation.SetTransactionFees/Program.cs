using System;
using System.IO;
using System.Linq;
using AElfChain.Common;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using log4net;
using McMaster.Extensions.CommandLineUtils;
using Prompt = Sharprompt.Prompt;

namespace AElf.Automation.SetTransactionFees
{
    internal class Program
    {
        private static readonly ILog Logger = Log4NetHelper.GetLogger();

        [Option("-e|--endpoint", Description = "Node service endpoint info")]
        public string Endpoint { get; set; } = "http://192.168.197.43:8100";

        [Option("-c|--config", Description = "Config file about bp node setting")]
        private static string ConfigFile { get; set; }

        [Option("-a|--amount", Description = "Transaction method fee balance")]
        public long Amount { get; set; } = 1000_0000L;

        public static int Main(string[] args)
        {
            try
            {
                return CommandLineApplication.Execute<Program>(args);
            }
            catch (Exception ex)
            {
                Logger.Error($"Execute failed: {ex.Message}");
            }

            return 0;
        }

        private void OnExecute()
        {
            //Init Logger
            Log4NetHelper.LogInit("ContractFee");

            if (ConfigFile == null)
            {
                var configPath = CommonHelper.MapPath("config");
                var configFiles = Directory.GetFiles(configPath, "nodes*.json")
                    .Select(o => o.Split("/").Last()).ToList();
                ConfigFile = Prompt.Select("Select env config", configFiles);
            }

            NodeInfoHelper.SetConfig(ConfigFile);

            if (Endpoint == null)
            {
                var nodes = NodeInfoHelper.Config.Nodes.Select(o => $"{o.Name} [{o.Endpoint}]").ToList();
                var command = Prompt.Select("Select Endpoint", nodes);
                Endpoint = command.Split("[").Last().Replace("]", "");
            }

            var nm = new NodeManager(Endpoint);
            var contractsFee = new ContractsFee(nm);
            //before
            contractsFee.QueryAllContractsMethodFee();
            while (true)
                try
                {
                    "Begin set transaction fee".WriteSuccessLine();
                    contractsFee.SetAllContractsMethodFee(Amount);
                    break;
                }
                catch (Exception e)
                {
                    e.Message.WriteErrorLine();
                }

            contractsFee.QueryAllContractsMethodFee();
            Logger.Info("All contract methods fee set completed.");
        }
    }
}