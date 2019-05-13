using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using AElf.Automation.Common.Helpers;
using McMaster.Extensions.CommandLineUtils;

namespace AElf.Automation.SideChain.Verification.Test
{
    [Command(Name = "Transaction Client", Description = "Monitor contract transaction testing client.")]
    [HelpOption("-?")]
    class Program
    {
        #region Parameter Option

        [Option("-ruM|--rpc.url", Description = "Rpc service url of node. It's required parameter.")]
        public string MainUrl { get; }
        
        [Option("-ruS1|--rpc.url", Description = "Rpc service url of node. It's required parameter.")]
        public string SideUrl1 { get; }
        
        [Option("-ruS2|--rpc.url", Description = "Rpc service url of node. It's required parameter.")]
        public string SideUrl2 { get; }
        
        [Option("-ac|--chain.account", Description = "Main Chain account, It's required parameter.")]
        public static string InitAccount { get; }
        
        public int ThreadCount { get; } = 1;
        public int TransactionGroup { get; } = 10;

        #endregion
        
        public static List<string> SideUrls { get; set; }
        private static readonly ILogHelper Logger = LogHelper.GetLogHelper();

        public static int Main(string[] args)
        {
            if (args.Length != 5) return CommandLineApplication.Execute<Program>(args);
            
            var tc = args[0];
            var ruM = args[1];
            var ruS1 = args[2];
            var ruS2 = args[3];
            var ac = args[4];
            args = new[] {"-tc", tc, "-ruM", ruM,"-ruS1",ruS1,"-ruS2",ruS2, "-ac",ac};

            return CommandLineApplication.Execute<Program>(args);
        }
        
        private void OnExecute(CommandLineApplication app)
        {
            if (MainUrl == null)
            {
                app.ShowHelp();
                return;
            }

            var operationSet = new OperationSet(ThreadCount,TransactionGroup,InitAccount,MainUrl);
            
            //Init Logger
            var logName = "RpcTh_" + operationSet.ThreadCount + "_Tx_" + operationSet.ExeTimes +"_"+ DateTime.Now.ToString("MMddHHmmss") + ".log";
            var dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", logName);
            Logger.InitLogHelper(dir);

            //Init chains
            SideUrls.Add(SideUrl1);
            SideUrls.Add(SideUrl2);
            
            var mainChain = operationSet.InitMain(InitAccount);
            var SideChains = operationSet.InitSideNodes(InitAccount);
            
            //Execute transaction command
            try
            {
                operationSet.InitMainExecCommand();
                // transfer on main chain and verify on side chain
                
            }
            catch (Exception e)
            {
                Logger.WriteError("Message: " + e.Message);
                Logger.WriteError("Source: " + e.Source);
                Logger.WriteError("StackTrace: " + e.StackTrace);
            }
            finally
            {
                //Delete accounts
                operationSet.DeleteAccounts();
            }

            //Result summary
            var set = new CategoryInfoSet(operationSet.ApiHelper.CommandList);
            set.GetCategoryBasicInfo();
            set.GetCategorySummaryInfo();
            var xmlFile = set.SaveTestResultXml(operationSet.ThreadCount, operationSet.ExeTimes);
            Logger.WriteInfo("Log file: {0}", dir);
            Logger.WriteInfo("Xml file: {0}", xmlFile);
            Logger.WriteInfo("Complete performance testing.");
        }
        

    }
}