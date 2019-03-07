using AElf.Automation.Common.Contracts;
using AElf.Automation.Common.Extensions;
using AElf.Automation.Common.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;

namespace AElf.Automation.Contracts.ScenarioTest
{
    [TestClass]
    public class ResourceContractsTest
    {
        private static readonly ILogHelper Logger = LogHelper.GetLogHelper();
        public string TokenAbi { get; set; }
        public CliHelper CH { get; set; }
        public string RpcUrl { get; } = "http://192.168.197.44:8000/chain";
        public List<string> AccList { get; set; }
        public string InitAccount { get; } = "ELF_64V9T3sYjDGBhjrKDc18baH2BQRjFyJifXqHaDZ83Z5ZQ7d";
        //Contract service List

        public TokenContract tokenService { get; set; }
        public ResourceContract resourceService { get; set; }

        [TestInitialize]
        public void Initlize()
        {
            #region Basic Preparation
            //Init Logger
            string logName = "ContractTest_" + DateTime.Now.ToString("MMddHHmmss") + ".log";
            string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", logName);
            Logger.InitLogHelper(dir);

            CH = new CliHelper(RpcUrl, AccountManager.GetDefaultDataDir());

            //Connect Chain
            var ci = new CommandInfo("ConnectChain");
            CH.RpcConnectChain(ci);
            Assert.IsTrue(ci.Result, "Connect chain got exception.");

            //Get AElf.Contracts.Token ABI
            ci.GetJsonInfo();
            TokenAbi = ci.JsonInfo["AElf.Contracts.Token"].ToObject<string>();

            //Load default Contract Abi
            ci = new CommandInfo("LoadContractAbi");
            CH.RpcLoadContractAbi(ci);
            Assert.IsTrue(ci.Result, "Load contract abi got exception.");

            //Account preparation
            AccList = new List<string>();
            ci = new CommandInfo("AccountNew", "account");
            for (int i = 0; i < 5; i++)
            {
                ci.Parameter = "123";
                ci = CH.NewAccount(ci);
                if (ci.Result)
                    AccList.Add(ci.InfoMsg?[0].Replace("Account address:", "").Trim());

                //unlock
                var ic = new CommandInfo("AccountUnlock", "account")
                {
                    Parameter = String.Format("{0} {1} {2}", AccList[i], "123", "notimeout")
                };
                CH.UnlockAccount(ic);
            }
            var uc = new CommandInfo("AccountUnlock", "account");
            uc.Parameter = string.Format("{0} {1} {2}", InitAccount, "123", "notimeout");
            CH.ExecuteCommand(uc);

            //Init token service
            PrepareUserTokens();
      
            //Init resource service
            resourceService = new ResourceContract(CH, AccList[2]);
            PrepareResourceToken();
            #endregion
        }

        [TestMethod]
        public void IssueResourceTest()
        {
            PrepareResourceToken();
        }

        [TestMethod]
        public void BuyResource1()
        {
            resourceService.Account = AccList[3];
            resourceService.CallContractMethod(ResourceMethod.BuyResource, "Cpu", "100000");
            resourceService.CallContractMethod(ResourceMethod.BuyResource, "Ram", "500");
            resourceService.CallContractMethod(ResourceMethod.BuyResource, "Net", "1000");
            QueryResourceInfo();
        }

        public void BuyResource2()
        {
            resourceService.Account = AccList[4];
            resourceService.CallContractMethod(ResourceMethod.BuyResource, "Cpu", "500");
            resourceService.CallContractMethod(ResourceMethod.BuyResource, "Ram", "500");
            resourceService.CallContractMethod(ResourceMethod.BuyResource, "Net", "1000");
            QueryResourceInfo();
        }

        [TestMethod]
        public void SellResource()
        {
        }

        private void PrepareUserTokens()
        {
            tokenService = new TokenContract(CH, InitAccount, TokenAbi);
            tokenService.CallContractMethod(TokenMethod.Initialize, "aelfToken", "ELF", "500000", "2");
            foreach (var acc in AccList)
            {
                tokenService.CallContractWithoutResult(TokenMethod.Transfer, acc, "10000");
            }

            tokenService.CheckTransactionResultList();
            foreach (var acc in AccList)
            {
                var queryResult = tokenService.CallReadOnlyMethod(TokenMethod.BalanceOf, acc);
                Logger.WriteInfo($"Account: {acc}, Balance: {tokenService.ConvertViewResult(queryResult, true)}");
            }
        }
        private void PrepareResourceToken()
        {
            //Init
            resourceService.CallContractMethod(ResourceMethod.Initialize, TokenAbi, InitAccount, InitAccount);

            //Issue
            resourceService.SetAccount(InitAccount);
            resourceService.CallContractMethod(ResourceMethod.IssueResource, "Cpu", "1000000");
            resourceService.CallContractMethod(ResourceMethod.IssueResource, "Net", "1000000");
            resourceService.CallContractMethod(ResourceMethod.IssueResource, "Ram", "1000000");

            //Query address
            var tokenAddress = resourceService.CallReadOnlyMethod(ResourceMethod.GetElfTokenAddress);
            Logger.WriteInfo(String.Format("Token address: {0}", resourceService.ConvertViewResult(tokenAddress, true)));

            var feeAddress = resourceService.CallReadOnlyMethod(ResourceMethod.GetFeeAddress);
            Logger.WriteInfo(String.Format("Fee address: {0}", resourceService.ConvertViewResult(feeAddress, true)));

            var controllerAddress = resourceService.CallReadOnlyMethod(ResourceMethod.GetResourceControllerAddress);
            Logger.WriteInfo(String.Format("Controller address: {0}", resourceService.ConvertViewResult(controllerAddress, true)));
        }

        private void QueryResourceInfo()
        {
            //Converter message
            var cpuConverter = resourceService.CallReadOnlyMethod(ResourceMethod.GetConverter, "Cpu");
            var ramConverter = resourceService.CallReadOnlyMethod(ResourceMethod.GetConverter, "Ram");
            var netConverter = resourceService.CallReadOnlyMethod(ResourceMethod.GetConverter, "Net");
            Logger.WriteInfo(String.Format("GetConverter info: Cpu-{0}, Ram-{1}, Net-{2}",
                resourceService.ConvertViewResult(cpuConverter),
                resourceService.ConvertViewResult(ramConverter),
                resourceService.ConvertViewResult(netConverter)));

            //User Balance
            var cpuBalance = resourceService.CallReadOnlyMethod(ResourceMethod.GetUserBalance, AccList[2], "Cpu");
            var ramBalance = resourceService.CallReadOnlyMethod(ResourceMethod.GetUserBalance, AccList[2], "Ram");
            var netBalance = resourceService.CallReadOnlyMethod(ResourceMethod.GetUserBalance, AccList[2], "Net");
            Logger.WriteInfo(String.Format("GetUserBalance info: Cpu-{0}, Ram-{1}, Net-{2}",
                resourceService.ConvertViewResult(cpuBalance, true),
                resourceService.ConvertViewResult(ramBalance, true),
                resourceService.ConvertViewResult(netBalance, true)));

            //Exchange Balance
            var cpuExchange = resourceService.CallReadOnlyMethod(ResourceMethod.GetExchangeBalance, "Cpu");
            var ramExchange = resourceService.CallReadOnlyMethod(ResourceMethod.GetExchangeBalance, "Ram");
            var netExchange = resourceService.CallReadOnlyMethod(ResourceMethod.GetExchangeBalance, "Net");
            Logger.WriteInfo(String.Format("GetExchangeBalance info: Cpu-{0}, Ram-{1}, Net-{2}",
                resourceService.ConvertViewResult(cpuExchange, true),
                resourceService.ConvertViewResult(ramExchange, true),
                resourceService.ConvertViewResult(netExchange, true)));

            //Elf Balance
            var cpuElf = resourceService.CallReadOnlyMethod(ResourceMethod.GetElfBalance, "Cpu");
            var ramElf = resourceService.CallReadOnlyMethod(ResourceMethod.GetElfBalance, "Ram");
            var netElf = resourceService.CallReadOnlyMethod(ResourceMethod.GetElfBalance, "Net");
            Logger.WriteInfo(String.Format("GetElfBalance info: Cpu-{0}, Ram-{1}, Net-{2}",
                resourceService.ConvertViewResult(cpuElf, true),
                resourceService.ConvertViewResult(ramElf, true),
                resourceService.ConvertViewResult(netElf, true)));
        }
    }
}