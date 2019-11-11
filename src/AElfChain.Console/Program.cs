using System;
using AElfChain.Common;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using AElfChain.Common;
using AElfChain.Common.Managers;
using AElfChain.SDK;
using log4net;
using McMaster.Extensions.CommandLineUtils;
using Volo.Abp.Threading;

namespace AElfChain.Console
{
    [Command(Name = "AElf CLI Tool", Description = "AElf console client test tool for transaction testing.")]
    [HelpOption("-?")]
    class Program
    {
        [Option("-e|--endpoint", Description = "Service endpoint url of node. It's required parameter.")]
        private static string Endpoint { get; set; }
        
        [Option("-c|--config", Description = "Config file about bp nodes setting")]
        private static string ConfigFile { get; set; }
        
        private static INodeManager NodeManager;
        private static IApiService ApiService => NodeManager.ApiService;
        private static ILog Logger { get; set; }
        
        public static int Main(string[] args)
        {
            return CommandLineApplication.Execute<Program>(args);
        }

        private void OnExecute(CommandLineApplication app)
        {
            Log4NetHelper.LogInit();
            Logger = Log4NetHelper.GetLogger("Program");

            if (Endpoint == null)
            {
                "Please input endpoint address(eg: 127.0.0.1:8000): ".WriteSuccessLine(changeLine: false);
                Endpoint = System.Console.ReadLine();
            }
            if (ConfigFile != null) NodeInfoHelper.SetConfig(ConfigFile);
            
            try
            {
                NodeManager = new NodeManager(Endpoint);
                var chainStatusDto = AsyncHelper.RunSync(ApiService.GetChainStatusAsync);
                Logger.Info($"ChainId: {chainStatusDto.ChainId}, LongestChainHeight: {chainStatusDto.LongestChainHeight}, LastIrreversibleBlockHeight: {chainStatusDto.LastIrreversibleBlockHeight}");

                var cliCommand = new CliCommand(NodeManager);
                cliCommand.ExecuteTransactionCommand();
            }
            catch (Exception e)
            {
                Logger.Error(e.Message);
            }
        }
    }
}