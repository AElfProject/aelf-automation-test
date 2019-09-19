using System;
using System.Threading.Tasks;
using AElf.Automation.Common.Helpers;
using AElf.Automation.Common.Managers;
using AElfChain.SDK;
using log4net;
using Newtonsoft.Json;

namespace AElfChain.Console
{
    class Program
    {
        private static string Endpoint;
        private static INodeManager NodeManager;
        private static IApiService ApiService => NodeManager.ApiService;
        private static ILog Logger { get; set; }
        
        static async Task Main(string[] args)
        {
            Log4NetHelper.LogInit();
            Logger = Log4NetHelper.GetLogger();
            
            "Please input endpoint address(eg: 127.0.0.1:8000):".WriteSuccessLine();
            Endpoint = System.Console.ReadLine();
            NodeManager = new NodeManager(Endpoint);
            try
            {
                var chainStatusDto = await ApiService.GetChainStatusAsync();
                Logger.Info(JsonConvert.SerializeObject(chainStatusDto));

                var scripts = new TransactionScripts(NodeManager);
                await scripts.ExecuteTransactionCommand();
            }
            catch (Exception e)
            {
                Logger.Error(e.Message);
            }
        }
    }
}