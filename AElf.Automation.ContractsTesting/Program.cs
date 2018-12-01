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
            contract.ExecuteContractMethod(out var txId1, "Initialize", "elfToken", "ELF", "2000000", "2");
            contract.CheckTransactionResult(out var initCi, txId1);

            //Transfer to Account A, B, C
            contract.ExecuteContractMethod(out var txIdA, "Transfer", accList[1], "5000");
            contract.ExecuteContractMethod(out var txIdB, "Transfer", accList[2], "10000");
            contract.ExecuteContractMethod(out var txIdC, "Transfer", accList[3], "15000");

            //check result
            contract.CheckTransactionResult(out var aCi, txIdA);
            contract.CheckTransactionResult(out var bCi, txIdB);
            contract.CheckTransactionResult(out var cCi, txIdC);

            //Get balance
            contract.ExecuteContractMethod(out var txOwner, "BalanceOf", accList[0]);
            contract.ExecuteContractMethod(out var txBA, "BalanceOf", accList[1]);
            contract.ExecuteContractMethod(out var txBB, "BalanceOf", accList[2]);
            contract.ExecuteContractMethod(out var txBC, "BalanceOf", accList[3]);

            //Query Result
            contract.GetTransactionResult(txOwner, out var ciOwner);
            contract.CheckTransactionResult(out var ciA, txBA);
            contract.CheckTransactionResult(out var ciB, txBB);
            contract.CheckTransactionResult(out var ciC, txBC);

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