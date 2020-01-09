using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.Contracts.TestContract.DApp;
using AElf.Contracts.TokenHolder;
using AElf.Types;
using AElfChain.Common;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using Google.Protobuf.WellKnownTypes;
using log4net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using InitializeInput = AElf.Contracts.TestContract.DApp.InitializeInput;

namespace AElf.Automation.Contracts.ScenarioTest
{
    [TestClass]
    public class DAppContractTest
    {
        private static readonly ILog Logger = Log4NetHelper.GetLogger();
        private INodeManager NodeManager { get; set; }
        private string ContractAddress = "uSXxaGWKDBPV6Z8EG8Et9sjaXhH1uMWEpVvmo2KzKEaueWzSe";
        public DAppContainer.DAppStub DAppStub { get; set; }
        public DAppContract DAppContract { get; set; }
        public ContractManager ContractManager { get; set; }
        public List<string> NodesAccounts { get; set; }
        public List<string> Investors { get; set; }

        [TestInitialize]
        public void TestInitialize()
        {
            Log4NetHelper.LogInit();
            NodeInfoHelper.SetConfig("nodes-env2-main");
            NodesAccounts = NodeInfoHelper.Config.Nodes.Select(o => o.Account).ToList();

            NodeManager = new NodeManager("192.168.197.40:8000");
            ContractManager = new ContractManager(NodeManager, NodesAccounts[0]);
            DAppContract = new DAppContract(NodeManager, NodesAccounts[0], ContractAddress);
            DAppStub = DAppContract.GetTestStub<DAppContainer.DAppStub>(NodesAccounts[0]);

            PrepareInvestors();
        }

        [TestMethod]
        public async Task InitializeDAppContract_Test()
        {
            var result = await DAppStub.Initialize.SendAsync(new InitializeInput
            {
                ProfitReceiver = NodesAccounts[0].ConvertAddress()
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined, result.TransactionResult.Error);
        }

        [TestMethod]
        public async Task SignUp_Deposit_RegistProfit_Test()
        {
            const long amount = 80_00000000L;
            for (var i = 0; i < 4; i++)
            {
                var dappStub = DAppContract.GetDAppStub(Investors[i]);
                var transactionResult = await dappStub.SignUp.SendAsync(new Empty());
                transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

                //allowance
                var approveResult =
                    ContractManager.Token.ApproveToken(Investors[i], DAppContract.ContractAddress, amount);
                approveResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

                //deposit
                transactionResult = await dappStub.Deposit.SendAsync(new DepositInput
                {
                    Amount = amount
                });
                transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

                //regist profit
                var tokenHolder = ContractManager.Genesis.GetTokenHolderStub(Investors[i]);
                var registResult = await tokenHolder.RegisterForProfits.SendAsync(new RegisterForProfitsInput
                {
                    SchemeManager = DAppContract.Contract,
                    Amount = 10_00000000 * (i + 1)
                });
                registResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

                //Query app balance
                var balance = ContractManager.Token.GetUserBalance(Investors[i], "APP");
                Logger.Info($"Account: {Investors[i]}, Balance: APP={balance}");
            }
        }

        [TestMethod]
        public async Task Use_Test()
        {
            const long amount = 100_00000000L;
            foreach (var acc in NodesAccounts.Take(4))
            {
                //allowance
                var approveResult =
                    ContractManager.Token.ApproveToken(acc, DAppContract.ContractAddress, amount, "APP");
                approveResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

                //deposit
                var dappStub = DAppContract.GetDAppStub(acc);
                for (var i = 0; i < 5; i++)
                {
                    var transactionResult = await dappStub.Use.SendAsync(new Record
                    {
                        Type = RecordType.Use,
                        Description = $"Use test-{Guid.NewGuid()}",
                    });
                    transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
                }
            }
        }

        [TestMethod]
        public async Task DeveloperReceiveProfit_Test()
        {
            var beforeBalance = ContractManager.Token.GetUserBalance(NodesAccounts[0]);

            var tokenStub = ContractManager.Genesis.GetTokenStub(NodesAccounts[0]);
            var profits = ContractManager.Token.GetUserBalance(DAppContract.CallAddress);
            var receiveResult = await tokenStub.ReceiveProfits.SendAsync(new ReceiveProfitsInput
            {
                ContractAddress = DAppContract.Contract,
                Symbol = "ELF",
                Amount = profits / 2
            });
            receiveResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var afterBalance = ContractManager.Token.GetUserBalance(NodesAccounts[0]);
            Logger.Info($"Contract Profit: {profits}, Developer account balance: {beforeBalance} => {afterBalance}");
        }

        [TestMethod]
        public async Task InvestorReceiveProfit_Test()
        {
            for (var i = 0; i < 4; i++)
            {
                var beforeBalance = ContractManager.Token.GetUserBalance(Investors[i]);

                var claimProfitsResult = await ContractManager.TokenHolderStub.ClaimProfits.SendAsync(
                    new ClaimProfitsInput
                    {
                        Beneficiary = Investors[i].ConvertAddress(),
                        SchemeManager = DAppContract.Contract,
                        Symbol = "ELF"
                    });
                claimProfitsResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

                var afterBalance = ContractManager.Token.GetUserBalance(Investors[i]);
                Logger.Info(
                    $"Investor profit balance change: {beforeBalance} => {afterBalance}, profit: {afterBalance - beforeBalance}");
            }
        }

        private void PrepareInvestors()
        {
            Investors = new List<string>
            {
                "4SSmEgAHeDcXmgAHwyUxhZGFuEmc8qHRF7RHg7Z9Nmfmv4zpr",
                "NW5A451NmRSMYJF44gEQi9azMnoXaos7BQdu1R6h8QzJEFNxW",
                "2fsScXEEHmpncCUCL5iP8664zDZmZGzZQ62czNa27V38ShiPCY",
                "MF7iyYxqvCA5HiCT4Y4U4ZhwhCcfJDDh9B4iQSuvzJL3kQzqR"
            };

            foreach (var acc in Investors)
            {
                var balance = ContractManager.Token.GetUserBalance(acc);
                if (balance > 10_00000000) continue;
                //transfer token for test
                ContractManager.Token.TransferBalance(NodesAccounts[0], acc, 200_00000000);
            }
        }
    }
}