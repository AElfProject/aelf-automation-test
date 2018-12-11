using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices.ComTypes;
using System.Threading;
using AElf.Automation.Common.Extensions;
using AElf.Automation.Common.Helpers;
using AElf.Automation.ContractsTesting.Contracts;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using QuickGraph;

namespace AElf.Automation.ContractsTesting
{
    class Program
    {
        public static ILogHelper Logger = LogHelper.GetLogHelper();
        public static string TokenAbi { get; set; }

        static void Main(string[] args)
        {
            #region Basic Preparation
            //Init Logger
            string logName = "ContractTest_" + DateTime.Now.ToString("MMddHHmmss") + ".log";
            string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", logName);
            Logger.InitLogHelper(dir);

            string url = "http://192.168.199.222:8000/chain";
            var ch = new CliHelper(url, AccountManager.GetDefaultDataDir());

            //Account preparation
            List<string> accList = new List<string>();
            var ci = new CommandInfo("account new", "account");
            for (int i = 0; i < 50; i++)
            {
                ci.Parameter = "123";
                ci = ch.ExecuteCommand(ci);
                if(ci.Result)
                    accList.Add(ci.InfoMsg?[0].Replace("Account address:", "").Trim());
            }

            //Unlock
            ci = new CommandInfo("account unlock", "account");
            ci.Parameter = String.Format("{0} {1} {2}", accList[0], "123", "notimeout");
            ci = ch.ExecuteCommand(ci);

            //Connect Chain
            ci = new CommandInfo("connect_chain");
            ch.ExecuteCommand(ci);
            Assert.IsTrue(ci.Result, "Connect chain got exception.");

            //Get AElf.Contracts.Token ABI
            ci.GetJsonInfo();
            TokenAbi = ci.JsonInfo["AElf.Contracts.Token"].ToObject<string>();

            //Load default Contract Abi
            ci = new CommandInfo("load_contract_abi");
            ch.RpcLoadContractAbi(ci);
            Assert.IsTrue(ci.Result, "Load contract abi got exception.");

            #endregion

            #region AElf.Token operation
            var contract = new ContractBase(ch, TokenAbi);
            contract.Account = accList[0];

            //Deploy
            //contract.DeployContract(out var txId);

            //Load
            contract.LoadContractAbi();

            //Init
            var txId1 = contract.ExecuteContractMethod("Initialize", "elfToken", "ELF", "2000000", "2");
            var initCi = contract.CheckTransactionResult(txId1);

            //Transfer to Account A, B, C
            var txIdA = contract.ExecuteContractMethod("Transfer", accList[1], "5000");
            var txIdB = contract.ExecuteContractMethod("Transfer", accList[2], "10000");
            var txIdC = contract.ExecuteContractMethod("Transfer", accList[3], "15000");

            //check result
            var aCi = contract.CheckTransactionResult(txIdA);
            var bCi = contract.CheckTransactionResult(txIdB);
            var cCi = contract.CheckTransactionResult(txIdC);

            //Get balance
            var txOwner = contract.ExecuteContractMethod("BalanceOf", accList[0]);
            var txBA = contract.ExecuteContractMethod("BalanceOf", accList[1]);
            var txBB = contract.ExecuteContractMethod("BalanceOf", accList[2]);
            var txBC = contract.ExecuteContractMethod("BalanceOf", accList[3]);

            //Query Result
            contract.GetTransactionResult(txOwner, out var ciOwner);
            var ciA = contract.CheckTransactionResult(txBA);
            var ciB = contract.CheckTransactionResult(txBB);
            var ciC = contract.CheckTransactionResult(txBC);

            //Convert to Value
            ciOwner.GetJsonInfo();
            string valueStr1 = ciOwner.JsonInfo["result"]["result"]["return"].ToString();
            Logger.WriteInfo($"Owner current balance: {Convert.ToInt32(valueStr1, 16)}");

            ciA.GetJsonInfo();
            string valueStrA = ciA.JsonInfo["result"]["result"]["return"].ToString();
            Logger.WriteInfo($"A current balance: {Convert.ToInt32(valueStrA, 16)}");

            ciB.GetJsonInfo();
            string valueStrB = ciB.JsonInfo["result"]["result"]["return"].ToString();
            Logger.WriteInfo($"B current balance: {Convert.ToInt32(valueStrB, 16)}");

            ciC.GetJsonInfo();
            string valueStrC = ciC.JsonInfo["result"]["result"]["return"].ToString();
            Logger.WriteInfo($"C current balance: {Convert.ToInt32(valueStrC, 16)}");

            #endregion

            #region AElf.Contract.Resource

            #endregion
        }
    }
}