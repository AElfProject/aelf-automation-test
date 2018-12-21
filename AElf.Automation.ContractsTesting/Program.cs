using System;
using System.Collections.Generic;
using System.IO;
using AElf.Automation.Common.Contracts;
using AElf.Automation.Common.Extensions;
using AElf.Automation.Common.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AElf.Automation.ContractsTesting
{
    class Program
    {
        public static ILogHelper Logger = LogHelper.GetLogHelper();
        public static string TokenAbi { get; set; }
        public static string RpcUrl { get; } = "http://192.168.197.44:8000/chain";

        static void Main(string[] args)
        {
            #region Basic Preparation
            //Init Logger
            string logName = "ContractTest_" + DateTime.Now.ToString("MMddHHmmss") + ".log";
            string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", logName);
            Logger.InitLogHelper(dir);

            var ch = new CliHelper(RpcUrl, AccountManager.GetDefaultDataDir());

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

                //unlock
                var uc = new CommandInfo("account unlock", "account");
                uc.Parameter = String.Format("{0} {1} {2}", accList[i], "123", "notimeout");
                uc = ch.ExecuteCommand(uc);
            }
            #endregion

            #region AElf.Benchmark.TestContract
            var benchmarkContract = new BaseContract(ch, "AElf.Benchmark.TestContract", accList[0]);

            var txId = benchmarkContract.ExecuteContractMethod("InitBalance", accList[0]);
            var txResult = benchmarkContract.CheckTransactionResult(txId);
            var trId  = benchmarkContract.ExecuteContractMethod("Transfer", accList[0], accList[1], "1000");
            benchmarkContract.CheckTransactionResult(txId);
            var accId = benchmarkContract.ExecuteContractMethod("GetBalance", accList[1]);
            var accResult = benchmarkContract.CheckTransactionResult(accId);
            accResult.GetJsonInfo();
            string accValue = accResult.JsonInfo["result"]["result"]["return"].ToString();
            Logger.WriteInfo($"Owner current balance: {Convert.ToInt32(accValue, 16)}");
            #endregion

            #region AElf.Token operation
            //var tokenContract = new ContractBase(ch, TokenAbi);
            //tokenContract.Account = accList[0];

            //Deploy and Load ABI
            var tokenContract = new BaseContract(ch, "AElf.Contracts.Token", accList[0]);

            //Init
            var initResult = tokenContract.ExecuteContractMethodWithResult("Initialize", "elfToken", "ELF", "40000", "2");

            //Approve Test
            var approveResult= tokenContract.ExecuteContractMethodWithResult("Approve", accList[1], "1000");
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
            var ciOwner = tokenContract.CheckTransactionResult(txOwner);
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
            var resourceContract = new ResourceContract(ch, accList[0]);

            resourceContract.CallContractMethod(ResourceMethod.Initialize, tokenContract.ContractAbi, accList[0], accList[0]);

            resourceContract.CallContractMethod(ResourceMethod.IssueResource, "CPU", "1000000");
            resourceContract.CallContractMethod(ResourceMethod.IssueResource, "Ram", "1000000");
            resourceContract.CallContractMethod(ResourceMethod.IssueResource, "Net", "1000000");
            
            //Buy resource
            resourceContract.Account = accList[1];
            resourceContract.CallContractMethod(ResourceMethod.BuyResource, "Cpu", "1000");
            resourceContract.CallContractMethod(ResourceMethod.BuyResource, "Cpu", "6000");
            resourceContract.CallContractMethod(ResourceMethod.BuyResource, "Ram", "10000");

            //Account 4 have no money
            resourceContract.Account = accList[4];
            resourceContract.CallContractMethod(ResourceMethod.BuyResource, "Net", "1000");
            resourceContract.CallContractMethod(ResourceMethod.BuyResource, "NET", "10000");

            //Query user resource
            var urResult = resourceContract.CallContractMethod(ResourceMethod.GetUserBalance, accList[1], "Cpu");
            var ucResult = resourceContract.CallContractMethod(ResourceMethod.GetUserBalance, accList[4], "Cpu");
            var unResult = resourceContract.CallContractMethod(ResourceMethod.GetUserBalance, accList[4], "Net");

            //Query user token
            var balanceResult = tokenContract.ExecuteContractMethod("BalanceOf", accList[0]);

            //Sell resource
            resourceContract.Account = accList[1];
            var sc1Result = resourceContract.CallContractMethod(ResourceMethod.SellResource, "CPU", "100");
            var sc2Result = resourceContract.CallContractMethod(ResourceMethod.SellResource, "cpu", "500");
            var sc3Result = resourceContract.CallContractMethod(ResourceMethod.SellResource, "Cpu", "1000");

            resourceContract.Account = accList[4];
            var sr1Result = resourceContract.CallContractMethod(ResourceMethod.SellResource, "Ram", "100");
            var sr2Result = resourceContract.CallContractMethod(ResourceMethod.SellResource, "Ram", "500");
            var ramBalance = resourceContract.CallContractMethod(ResourceMethod.GetUserBalance, accList[0], "Ram");
            var sr3Result = resourceContract.CallContractMethod(ResourceMethod.SellResource, "Ram", "1000");

            #endregion
        }
    }
}
