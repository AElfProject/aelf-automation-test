﻿using System;
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
        public static string ConsesusAbi { get; set; }
        public static string RpcUrl { get; } = "http://192.168.197.44:8000/chain";
        public static string BpAccount { get; } = "ELF_64V9T3sYjDGBhjrKDc18baH2BQRjFyJifXqHaDZ83Z5ZQ7d";
        public static string FeeAccount { get; } = "ELF_54xku6ywapZEpuV7mcRVoGaYNS4uSPRRYQ5p2K8zWprPo5C";

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
            ConsesusAbi = ci.JsonInfo["AElf.Contracts.Consensus"].ToObject<string>();

            //Load default Contract Abi
            ci = new CommandInfo("load_contract_abi");
            ch.RpcLoadContractAbi(ci);
            Assert.IsTrue(ci.Result, "Load contract abi got exception.");

            //Account preparation
            List<string> accList = new List<string>();

            for (int i = 0; i < 5; i++)
            {
                ci = new CommandInfo("account new", "account") {Parameter = "123"};
                ci = ch.NewAccount(ci);
                if(ci.Result)
                    accList.Add(ci.InfoMsg?[0].Replace("Account address:", "").Trim());

                //unlock
                var uc = new CommandInfo("account unlock", "account");
                uc.Parameter = string.Format("{0} {1} {2}", accList[i], "123", "notimeout");
                ch.UnlockAccount(uc);
            }
            #endregion

            #region AElf.Token operation
            //Deploy and Load ABI
            var tokenContract = new TokenContract(ch, BpAccount, TokenAbi);
            var consesusContract = new ConsensusContract(ch, BpAccount, ConsesusAbi);
            consesusContract.CallContractMethod(ConsensusMethod.InitialBalance, BpAccount, "100000");

            //Set token fee
            tokenContract.CallContractMethod(TokenMethod.SetFeePoolAddress, FeeAccount);

            //Approve Test
            tokenContract.SetAccount(accList[1]);
            tokenContract.CallContractMethod(TokenMethod.Approve, accList[1], "1000");

            //Transfer to Account A, B, C
            tokenContract.CallContractWithoutResult(TokenMethod.Transfer, accList[1], "5000");
            tokenContract.CallContractWithoutResult(TokenMethod.Transfer, accList[2], "10000");
            tokenContract.CallContractWithoutResult(TokenMethod.Transfer, accList[3], "15000");

            tokenContract.CheckTransactionResultList();

            //Get balance
            var txOwner = tokenContract.CallReadOnlyMethod(TokenMethod.BalanceOf, accList[0]);
            var txBa = tokenContract.CallReadOnlyMethod(TokenMethod.BalanceOf, accList[1]);
            var txBb = tokenContract.CallReadOnlyMethod(TokenMethod.BalanceOf, accList[2]);
            var txBc = tokenContract.CallReadOnlyMethod(TokenMethod.BalanceOf, accList[3]);

            //Convert to Value
            Logger.WriteInfo($"Owner current balance: {tokenContract.ConvertViewResult(txOwner, true)}");

            Logger.WriteInfo($"A current balance: {tokenContract.ConvertViewResult(txBa, true)}");

            Logger.WriteInfo($"B current balance: {tokenContract.ConvertViewResult(txBb, true)}");

            Logger.WriteInfo($"C current balance: {tokenContract.ConvertViewResult(txBc, true)}");

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
            resourceContract.SetAccount(accList[4]);
            resourceContract.CallContractMethod(ResourceMethod.BuyResource, "Net", "1000");
            resourceContract.CallContractMethod(ResourceMethod.BuyResource, "NET", "10000");

            //Query user resource
            var urResult = resourceContract.CallReadOnlyMethod(ResourceMethod.GetUserBalance, accList[1], "Cpu");
            var ucResult = resourceContract.CallReadOnlyMethod(ResourceMethod.GetUserBalance, accList[4], "Cpu");
            var unResult = resourceContract.CallReadOnlyMethod(ResourceMethod.GetUserBalance, accList[4], "Net");

            //Query user token
            var balanceResult = tokenContract.ExecuteContractMethod("BalanceOf", accList[0]);

            //Sell resource
            resourceContract.SetAccount(accList[1]);
            var sc1Result = resourceContract.CallContractMethod(ResourceMethod.SellResource, "CPU", "100");
            var sc2Result = resourceContract.CallContractMethod(ResourceMethod.SellResource, "cpu", "500");
            var sc3Result = resourceContract.CallContractMethod(ResourceMethod.SellResource, "Cpu", "1000");

            resourceContract.SetAccount(accList[4]);
            var sr1Result = resourceContract.CallContractMethod(ResourceMethod.SellResource, "Ram", "100");
            var sr2Result = resourceContract.CallContractMethod(ResourceMethod.SellResource, "Ram", "500");
            var ramBalance = resourceContract.CallContractMethod(ResourceMethod.GetUserBalance, accList[0], "Ram");
            var sr3Result = resourceContract.CallContractMethod(ResourceMethod.SellResource, "Ram", "1000");

            #endregion
        }
    }
}
