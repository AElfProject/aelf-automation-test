using System;
using System.Collections.Generic;
using System.IO;
using AElf.Automation.Common.Contracts;
using AElf.Automation.Common.Extensions;
using AElf.Automation.Common.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AElf.Automation.TransactionExecution
{
    class Program
    {
        #region Private Properties

        private static readonly ILogHelper Logger = LogHelper.GetLogHelper();
        private static string TokenAddress { get; set; }
        private static string ResourceAddress { get; set; }
        private static List<string> Users { get; set; }

        private static CliHelper CH { get; set; }

        private static TokenExecutor Executor { get; set; }

        #endregion

        public static string Endpoint { get; set; } = "http://192.168.197.44:8000/chain";

        static void Main(string[] args)
        {
            #region Basic Preparation
            //Init Logger
            string logName = "ContractTest_" + DateTime.Now.ToString("MMddHHmmss") + ".log";
            string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", logName);
            Logger.InitLogHelper(dir);

            CH = new CliHelper(Endpoint, AccountManager.GetDefaultDataDir());

            //Connect Chain
            var ci = new CommandInfo("ConnectChain");
            CH.ExecuteCommand(ci);
            Assert.IsTrue(ci.Result, "Connect chain got exception.");

            //Get AElf.Contracts.Token ABI
            ci.GetJsonInfo();
            TokenAddress = ci.JsonInfo["AElf.Contracts.Token"].ToObject<string>();
            ResourceAddress = ci.JsonInfo["AElf.Contracts.Resource"].ToObject<string>();

            //Load default Contract Abi
            ci = new CommandInfo("LoadContractAbi");
            CH.RpcLoadContractAbi(ci);
            Assert.IsTrue(ci.Result, "Load contract abi got exception.");

            //Account preparation
            Users = new List<string>();

            for (int i = 0; i < 5; i++)
            {
                ci = new CommandInfo("account new", "account") {Parameter = "123"};
                ci = CH.NewAccount(ci);
                if(ci.Result)
                    Users.Add(ci.InfoMsg?[0].Replace("Account address:", "").Trim());

                //unlock
                var uc = new CommandInfo("account unlock", "account");
                uc.Parameter = string.Format("{0} {1} {2}", Users[i], "123", "notimeout");
                CH.UnlockAccount(uc);
            }
            #endregion

            #region Transaction Execution
            Executor = new TokenExecutor(CH, Users[0]);
            TokenAddress = Executor.Token.ContractAbi;

            //Transfer and check
            for (int i = 1; i < Users.Count; i++)
            {
                //Execute Transfer
                Executor.Token.CallContractMethod(TokenMethod.Transfer, Users[i], (i * 100).ToString());
                //Query Balance
                var balanceResult = Executor.Token.CallReadOnlyMethod(TokenMethod.BalanceOf, Users[i]);
                var balance = Executor.Token.ConvertViewResult(balanceResult, true);
                Console.WriteLine($"User: {Users[i]}, Balance: {balance}");
            }

            #endregion

            Console.ReadLine();
        }
    }
}