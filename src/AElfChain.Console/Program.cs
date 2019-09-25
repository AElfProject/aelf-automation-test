using System;
using System.Threading.Tasks;
using AElf.Automation.Common.Helpers;
using AElf.Automation.Common.Managers;
using AElfChain.SDK;
using log4net;
using McMaster.Extensions.CommandLineUtils;
using Volo.Abp.Threading;

namespace AElfChain.Console
{
    [Command(Name = "Transaction Client", Description = "Transaction CLI client tool.")]
    [HelpOption("-?")]
    class Program
    {
        [Option("-e|--endpoint", Description = "Service endpoint url of node. It's required parameter.")]
        private static string Endpoint { get; set; }
        
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
            NodeManager = new NodeManager(Endpoint);
            try
            {
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