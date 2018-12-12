using System;
using System.Collections.Generic;
using System.IO;
using AElf.Automation.Common.Extensions;
using AElf.Automation.Common.Helpers;
using AElf.Automation.ContractsTesting.Contracts;
using Microsoft.VisualStudio.TestTools.UnitTesting;

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

            string url = "http://192.168.197.34:8000/chain";
            var ch = new CliHelper(url, AccountManager.GetDefaultDataDir());

            //Connect Chain
            var ci = new CommandInfo("connect_chain");
            ch.ExecuteCommand(ci);
            Assert.IsTrue(ci.Result, "Connect chain got exception.");

            //Get AElf.Contracts.Token ABI
            ci.GetJsonInfo();
            TokenAbi = ci.JsonInfo["AElf.Contracts.Token"].ToObject<string>();

            //Load default Contract Abi
            ci = new CommandInfo("load_contract_abi");
            ch.RpcLoadContractAbi(ci);
            Assert.IsTrue(ci.Result, "Load contract abi got exception.");

            //Account preparation
            List<string> accList = new List<string>();
            ci = new CommandInfo("account new", "account");
            for (int i = 0; i < 5; i++)
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

            #endregion

            #region AElf.Token operation
            var tokenContract = new ContractBase(ch, TokenAbi);
            tokenContract.Account = accList[0];

            //Deploy
            //contract.DeployContract(out var txId);

            //Load
            tokenContract.LoadContractAbi();

            //Init
            var txId1 = tokenContract.ExecuteContractMethod("Initialize", "elfToken", "ELF", "100000", "2");
            var initCi = tokenContract.CheckTransactionResult(txId1);

            //Transfer to Account A, B, C
            var txIdA = tokenContract.ExecuteContractMethod("Transfer", accList[1], "5000");
            var txIdB = tokenContract.ExecuteContractMethod("Transfer", accList[2], "10000");
            var txIdC = tokenContract.ExecuteContractMethod("Transfer", accList[3], "15000");

            //check result
            var aCi = tokenContract.CheckTransactionResult(txIdA);
            var bCi = tokenContract.CheckTransactionResult(txIdB);
            var cCi = tokenContract.CheckTransactionResult(txIdC);

            //Get balance
            var txOwner = tokenContract.ExecuteContractMethod("BalanceOf", accList[0]);
            var txBA = tokenContract.ExecuteContractMethod("BalanceOf", accList[1]);
            var txBB = tokenContract.ExecuteContractMethod("BalanceOf", accList[2]);
            var txBC = tokenContract.ExecuteContractMethod("BalanceOf", accList[3]);

            //Query Result
            tokenContract.GetTransactionResult(txOwner, out var ciOwner);
            var ciA = tokenContract.CheckTransactionResult(txBA);
            var ciB = tokenContract.CheckTransactionResult(txBB);
            var ciC = tokenContract.CheckTransactionResult(txBC);

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
            var resourceContract = new ContractBase(ch, "AElf.Contracts.Resource", accList[0]);

            resourceContract.LoadContractAbi();

            var initId = resourceContract.ExecuteContractMethod("Initialize", tokenContract.ContractAbi);
            var initResult = resourceContract.CheckTransactionResult(initId);
            Assert.IsTrue(initResult.Result, "Initialize executed failed.");

            var cpuId = resourceContract.ExecuteContractMethod("AdjustResourceCap", "Cpu", "1000000");
            var ramId = resourceContract.ExecuteContractMethod("AdjustResourceCap", "Ram", "1000000");
            var netId = resourceContract.ExecuteContractMethod("AdjustResourceCap", "Net", "1000000");

            var cpuResult = resourceContract.CheckTransactionResult(cpuId);
            Assert.IsTrue(cpuResult.Result, "Cpu resource adjust failed.");

            var ramResult = resourceContract.CheckTransactionResult(ramId);
            Assert.IsTrue(ramResult.Result, "Ram resource adjust failed.");

            var netResult = resourceContract.CheckTransactionResult(netId);
            Assert.IsTrue(netResult.Result, "Net resource adjust failed.");

            #endregion
        }
    }
}