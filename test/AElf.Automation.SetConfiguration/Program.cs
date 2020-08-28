using System;
using System.Threading.Tasks;
using AElfChain.Common;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using log4net;
using McMaster.Extensions.CommandLineUtils;

namespace AElf.Automation.SetConfiguration
{
    class Program
    {
        private static ILog Logger { get; set; }

        [Option("-e|--endpoint", Description = "Node service endpoint info")]
        public string Endpoint { get; set; } = "http://192.168.197.21:8000";

        [Option("-k|--keys", Description =
            "Configuration keys: BlockTransactionLimit, StateLimitSize, ExecutionObserverThreshold")]
        private static string Keys { get; set; }

        [Option("-c|--config", Description = "Config file about bp node setting")]
        private static string ConfigFile { get; set; }

        [Option("-a|--amount", Description = "Transaction method fee balance")]
        public int Amount { get; set; } = 10000;

        static int Main(string[] args)
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

        private async Task OnExecute()
        {
            //Init Logger
            Log4NetHelper.LogInit("ConfigurationSet");
            Logger = Log4NetHelper.GetLogger();
            NodeInfoHelper.SetConfig(ConfigFile);
            var nm = new NodeManager(Endpoint);
            var setConfiguration = new SetConfiguration(nm);
            //before
            await setConfiguration.QueryAllConfiguration();
            try
            {
                "Begin set configuration".WriteSuccessLine();
                switch (Keys)
                {
                    case "BlockTransactionLimit":
                        setConfiguration.SetBlockTransactionLimit(Amount);
                        break;
                    case "StateLimitSize":
                        setConfiguration.SetStateSizeLimit(Amount);
                        break;
                    case "ExecutionObserverThreshold":
                        setConfiguration.SetExecutionObserverThreshold(Amount);
                        break;

                    case "All":
                        setConfiguration.SetAllConfiguration(Amount);
                        break;
                }

                await setConfiguration.QueryAllConfiguration();
            }
            catch (Exception e)
            {
                e.Message.WriteErrorLine();
            }
        }
    }
}