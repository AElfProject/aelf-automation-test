using AElf.Automation.Common.Contracts;
using AElf.Automation.Common.Extensions;
using AElf.Automation.Common.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using AElf.Contracts.MultiToken.Messages;
using AElf.Contracts.TokenConverter;

namespace AElf.Automation.Contracts.ScenarioTest
{
    [TestClass]
    public class ResourceContractsTest
    {
        private static readonly ILogHelper Logger = LogHelper.GetLogHelper();
        public string TokenContract { get; set; }
        public WebApiHelper ApiHelper { get; set; }
        public string RpcUrl { get; } = "http://192.168.197.13:8100/chain";
        public List<string> AccList { get; set; }
        public string TokenSymbol { get; } = "ELF";
        public string InitAccount { get; } = "ELF_64V9T3sYjDGBhjrKDc18baH2BQRjFyJifXqHaDZ83Z5ZQ7d";
        public string FeeReceiverAccount { get; } = "";
        public string ManagerAccount { get; } = "";
        
        //Contract service List
        public TokenContract tokenService { get; set; }
        public FeeReceiverContract feeReceiverService { get; set; }
        public TokenConverterContract tokenConverterService { get; set; }

        [TestInitialize]
        public void Initialize()
        {
            #region Basic Preparation
            //Init Logger
            string logName = "ContractTest_" + DateTime.Now.ToString("MMddHHmmss") + ".log";
            string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", logName);
            Logger.InitLogHelper(dir);

            ApiHelper = new WebApiHelper(RpcUrl, AccountManager.GetDefaultDataDir());

            //Connect Chain
            var ci = new CommandInfo(ApiMethods.GetChainInformation);
            ApiHelper.GetChainInformation(ci);
            Assert.IsTrue(ci.Result, "Connect chain got exception.");

            //Get MultiToken and TokenConverter contract address 
           

            //Account preparation
            AccList = new List<string>();
            ci = new CommandInfo(ApiMethods.AccountNew);
            for (int i = 0; i < 5; i++)
            {
                ci.Parameter = "123";
                ci = ApiHelper.NewAccount(ci);
                if (ci.Result)
                    AccList.Add(ci.InfoMsg.ToString());

                //unlock
                var ic = new CommandInfo(ApiMethods.AccountUnlock)
                {
                    Parameter = $"{AccList[i]} 123 notimeout"
                };
                ApiHelper.UnlockAccount(ic);
            }
            var uc = new CommandInfo(ApiMethods.AccountUnlock)
            {
                Parameter = $"{InitAccount} 123 notimeout"
            };
            ApiHelper.ExecuteCommand(uc);

            //Init services
            PrepareUserTokens();
            
            PrepareFeeReceiverContract();
            
            tokenConverterService = new TokenConverterContract(ApiHelper, InitAccount);
            
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
            tokenConverterService.SetAccount(AccList[3]);
//            tokenConverterService.ExecuteMethodWithResult(ResourceMethod.BuyResource, "Cpu", "100000");
//            tokenConverterService.ExecuteMethodWithResult(ResourceMethod.BuyResource, "Ram", "500");
//            tokenConverterService.ExecuteMethodWithResult(ResourceMethod.BuyResource, "Net", "1000");
            QueryResourceInfo();
        }

        public void BuyResource2()
        {
            tokenConverterService.SetAccount(AccList[4]);
//            tokenConverterService.ExecuteMethodWithResult(ResourceMethod.BuyResource, "Cpu", "500");
//            tokenConverterService.ExecuteMethodWithResult(ResourceMethod.BuyResource, "Ram", "500");
//            tokenConverterService.ExecuteMethodWithResult(ResourceMethod.BuyResource, "Net", "1000");
            QueryResourceInfo();
        }

        [TestMethod]
        public void SellResource()
        {
        }

        private void PrepareUserTokens()
        {
            tokenService = new TokenContract(ApiHelper, InitAccount, TokenContract);
            tokenService.ExecuteMethodWithResult(TokenMethod.Create, new CreateInput
            {
                Decimals = 2,
                IsBurnable = true,
                Issuer = Address.Parse(InitAccount),
                Symbol = TokenSymbol,
                TokenName = "AElf token",
                TotalSupply = 1000_000L
            });
            tokenService.ExecuteMethodWithResult(TokenMethod.Issue, new IssueInput
            {  
                Symbol = TokenSymbol,
                Amount = 1000_000L,
                To = Address.Parse(InitAccount),
                Memo = "Issue token to init account"
            });
            
            foreach (var acc in AccList)
            {
                tokenService.ExecuteMethodWithTxId(TokenMethod.Transfer, new TransferInput
                {
                    Symbol = TokenSymbol,
                    Amount = 10_000,
                    Memo = "transfer for resource trade.",
                    To = Address.Parse(acc)
                });
            }
            tokenService.CheckTransactionResultList();
            
            foreach (var acc in AccList)
            {
                var queryResult = tokenService.CallViewMethod<GetBalanceOutput>(TokenMethod.GetBalance, new GetBalanceInput
                {
                    Symbol = TokenSymbol,
                    Owner = Address.Parse(acc)
                });
                Logger.WriteInfo($"Account: {acc}, Balance: {queryResult.Balance}");
            }
        }

        private void PrepareFeeReceiverContract()
        {
            feeReceiverService = new FeeReceiverContract(ApiHelper, InitAccount);
            feeReceiverService.ExecuteMethodWithResult(FeeReceiverMethod.Initialize, new InitializeInput
            {
                TokenContractAddress = Address.Parse(TokenContract),
                BaseTokenSymbol = TokenSymbol,
                FeeRate = "0.05",
                FeeReceiverAddress = Address.Parse(FeeReceiverAccount),
                ManagerAddress = Address.Parse(ManagerAccount)
            });
        }
        
        private void PrepareResourceToken()
        {
            //Init
            var ramConnector = new Connector
            {
                Symbol = "RAM",
                VirtualBalance = 0,
                Weight = "0.5",
                IsPurchaseEnabled = true,
                IsVirtualBalanceEnabled = false
            };
            
            var netConnector = new Connector
            {
                Symbol = "NET",
                VirtualBalance = 0,
                Weight = "0.5",
                IsPurchaseEnabled = true,
                IsVirtualBalanceEnabled = false 
            };
            
            var cpuConnector = new Connector
            {
                Symbol = "CPU",
                VirtualBalance = 0,
                Weight = "0.5",
                IsPurchaseEnabled = true,
                IsVirtualBalanceEnabled = false 
            };
            
            tokenConverterService.ExecuteMethodWithResult(TokenConverterMethod.Initialize, new InitializeInput
            {
                BaseTokenSymbol = TokenSymbol,
                FeeRate = "0.005",
                ManagerAddress = Address.Parse(ManagerAccount),
                TokenContractAddress = Address.Parse(TokenContract),
                FeeReceiverAddress = Address.Parse(FeeReceiverAccount),
                Connectors = {ramConnector, netConnector, cpuConnector }
            });

            //Issue
            tokenConverterService.SetAccount(InitAccount);
//            tokenConverterService.ExecuteMethodWithResult(ResourceMethod.IssueResource, "Cpu", "1000000");
//            tokenConverterService.ExecuteMethodWithResult(ResourceMethod.IssueResource, "Net", "1000000");
//            tokenConverterService.ExecuteMethodWithResult(ResourceMethod.IssueResource, "Ram", "1000000");

            //Query address
//            var tokenAddress = tokenConverterService.CallReadOnlyMethod(ResourceMethod.GetElfTokenAddress);
//            Logger.WriteInfo(String.Format("Token address: {0}", tokenConverterService.ConvertViewResult(tokenAddress, true)));
//
//            var feeAddress = tokenConverterService.CallReadOnlyMethod(ResourceMethod.GetFeeAddress);
//            Logger.WriteInfo(String.Format("Fee address: {0}", tokenConverterService.ConvertViewResult(feeAddress, true)));
//
//            var controllerAddress = tokenConverterService.CallReadOnlyMethod(ResourceMethod.GetResourceControllerAddress);
//            Logger.WriteInfo(String.Format("Controller address: {0}", tokenConverterService.ConvertViewResult(controllerAddress, true)));
        }

        private void QueryResourceInfo()
        {
            //Converter message
//            var cpuConverter = tokenConverterService.CallReadOnlyMethod(ResourceMethod.GetConverter, "Cpu");
//            var ramConverter = tokenConverterService.CallReadOnlyMethod(ResourceMethod.GetConverter, "Ram");
//            var netConverter = tokenConverterService.CallReadOnlyMethod(ResourceMethod.GetConverter, "Net");
//            Logger.WriteInfo(String.Format("GetConverter info: Cpu-{0}, Ram-{1}, Net-{2}",
//                tokenConverterService.ConvertViewResult(cpuConverter),
//                tokenConverterService.ConvertViewResult(ramConverter),
//                tokenConverterService.ConvertViewResult(netConverter)));

            //User Balance
//            var cpuBalance = tokenConverterService.CallReadOnlyMethod(ResourceMethod.GetUserBalance, AccList[2], "Cpu");
//            var ramBalance = tokenConverterService.CallReadOnlyMethod(ResourceMethod.GetUserBalance, AccList[2], "Ram");
//            var netBalance = tokenConverterService.CallReadOnlyMethod(ResourceMethod.GetUserBalance, AccList[2], "Net");
//            Logger.WriteInfo(String.Format("GetUserBalance info: Cpu-{0}, Ram-{1}, Net-{2}",
//                tokenConverterService.ConvertViewResult(cpuBalance, true),
//                tokenConverterService.ConvertViewResult(ramBalance, true),
//                tokenConverterService.ConvertViewResult(netBalance, true)));

            //Exchange Balance
//            var cpuExchange = tokenConverterService.CallReadOnlyMethod(ResourceMethod.GetExchangeBalance, "Cpu");
//            var ramExchange = tokenConverterService.CallReadOnlyMethod(ResourceMethod.GetExchangeBalance, "Ram");
//            var netExchange = tokenConverterService.CallReadOnlyMethod(ResourceMethod.GetExchangeBalance, "Net");
//            Logger.WriteInfo(String.Format("GetExchangeBalance info: Cpu-{0}, Ram-{1}, Net-{2}",
//                tokenConverterService.ConvertViewResult(cpuExchange, true),
//                tokenConverterService.ConvertViewResult(ramExchange, true),
//                tokenConverterService.ConvertViewResult(netExchange, true)));

            //Elf Balance
//            var cpuElf = tokenConverterService.CallReadOnlyMethod(ResourceMethod.GetElfBalance, "Cpu");
//            var ramElf = tokenConverterService.CallReadOnlyMethod(ResourceMethod.GetElfBalance, "Ram");
//            var netElf = tokenConverterService.CallReadOnlyMethod(ResourceMethod.GetElfBalance, "Net");
//            Logger.WriteInfo(String.Format("GetElfBalance info: Cpu-{0}, Ram-{1}, Net-{2}",
//                tokenConverterService.ConvertViewResult(cpuElf, true),
//                tokenConverterService.ConvertViewResult(ramElf, true),
//                tokenConverterService.ConvertViewResult(netElf, true)));
        }
    }
}