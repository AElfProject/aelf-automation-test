using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.Contracts.TestContract.DApp;
using AElf.Contracts.TokenConverter;
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
        private readonly string AppSymbol = "MAPP";
        private readonly string ConfigFile = "nodes-env2-side1";
        private readonly string ContractAddress = "rCCKJDg8STNqYtwbUC23SEyYNv6mYvKuEnXJPj8ZoGkgQFWqh";
        private INodeManager NodeManager { get; set; }
        public DAppContainer.DAppStub DAppStub { get; set; }
        public DAppContract DAppContract { get; set; }
        public ContractManager ContractManager { get; set; }
        public List<string> NodesAccounts { get; set; }
        public List<string> Investors { get; set; }

        public string PrimarySymbol { get; set; }

        [TestInitialize]
        public void TestInitialize()
        {
            Log4NetHelper.LogInit();
            NodeInfoHelper.SetConfig(ConfigFile);
            NodesAccounts = NodeInfoHelper.Config.Nodes.Select(o => o.Account).ToList();

            NodeManager = new NodeManager(NodeInfoHelper.Config.Nodes.First().Endpoint);
            ContractManager = new ContractManager(NodeManager, NodesAccounts[0]);
            DAppContract = new DAppContract(NodeManager, NodesAccounts[0], ContractAddress);
            DAppStub = DAppContract.GetTestStub<DAppContainer.DAppStub>(NodesAccounts[0]);

            PrimarySymbol = NodeManager.GetPrimaryTokenSymbol();
            PrepareInvestors();
        }

        [TestMethod]
        public async Task InitializeDAppContract_Test()
        {
            Logger.Info("=>InitializeDAppContract_Test");
            var result = await DAppStub.Initialize.SendAsync(new InitializeInput
            {
                ProfitReceiver = NodesAccounts[0].ConvertAddress(),
                Symbol = AppSymbol
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined, result.TransactionResult.Error);
            GetDAppContractBalance();
        }

        [TestMethod]
        public async Task SignUp_Deposit_RegistProfit_Test()
        {
            Logger.Info("=>SignUp_Deposit_RegistProfit_Test");
            const long amount = 100_00000000L;
            var beforeDappBalance = ContractManager.Token.GetUserBalance(DAppContract.ContractAddress);
            for (var i = 0; i < 4; i++)
            {
                var j = i;
                var dappStub = DAppContract.GetDAppStub(Investors[j]);
                var transactionResult = await dappStub.SignUp.SendAsync(new Empty());
                transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

                //allowance
                var approveResult =
                    ContractManager.Token.ApproveToken(Investors[j], DAppContract.ContractAddress, amount,
                        PrimarySymbol);
                approveResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

                //deposit
                transactionResult = await dappStub.Deposit.SendAsync(new DepositInput
                {
                    Amount = amount
                });
                transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

                //regist profit
                var tokenHolder = ContractManager.Genesis.GetTokenHolderStub(Investors[j]);
                var registResult = await tokenHolder.RegisterForProfits.SendAsync(new RegisterForProfitsInput
                {
                    SchemeManager = DAppContract.Contract,
                    Amount = 10_00000000L * (j + 1)
                });
                registResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

                //Query app balance
                var balance = ContractManager.Token.GetUserBalance(Investors[j], AppSymbol);
                Logger.Info($"Account: {Investors[j]}, Balance: {AppSymbol}={balance}");
            }

            var afterDappBalance = ContractManager.Token.GetUserBalance(DAppContract.ContractAddress);
            Logger.Info($"DApp contract balance: {beforeDappBalance} => {afterDappBalance}");
        }

        [TestMethod]
        public async Task NodesSignUp_Test()
        {
            Logger.Info("=>NodesSignUp_Test");
            const long amount = 50_00000000;
            foreach (var acc in NodesAccounts.Take(4))
            {
                var dappStub = DAppContract.GetDAppStub(acc);
                var transactionResult = await dappStub.SignUp.SendAsync(new Empty());
                transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

                //allowance
                var approveResult =
                    ContractManager.Token.ApproveToken(acc, DAppContract.ContractAddress, amount, PrimarySymbol);
                approveResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

                //deposit
                transactionResult = await dappStub.Deposit.SendAsync(new DepositInput
                {
                    Amount = amount
                });
                transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            }

            GetDAppContractBalance();
        }

        [TestMethod]
        [DataRow(new[] {0, 1, 2, 3})]
        public async Task Use_Test(int[] idArray)
        {
            Logger.Info("=>Use_Test");
            const long amount = 100_00000000L;
            foreach (var id in idArray)
            {
                var acc = NodesAccounts[id];
                //allowance
                var approveResult =
                    ContractManager.Token.ApproveToken(acc, DAppContract.ContractAddress, amount, AppSymbol);
                approveResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

                approveResult =
                    ContractManager.Token.ApproveToken(acc, ContractManager.TokenHolder.CallAddress, amount,
                        PrimarySymbol);
                approveResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

                //use
                var dappStub = DAppContract.GetDAppStub(acc);
                for (var i = 0; i < 2; i++)
                {
                    var transactionResult = await dappStub.Use.SendAsync(new Record
                    {
                        Type = RecordType.Use,
                        Description = $"Use test-{Guid.NewGuid()}"
                    });
                    transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
                }
            }

            GetDAppContractBalance();
        }

        [TestMethod]
        [DataRow(0, "SAPP", 6)]
        public async Task Use_ResourceToken_Test(int countId, string symbol, int executeTimes)
        {
            Logger.Info("=>Use_ResourceToken_Test");
            const long amount = 50_00000000L;
            var acc = NodesAccounts[countId];
            //prepare resource
            await DAppPrepareResourceBalance(symbol);
            //await NodesPrepareResourceBalance(acc, symbol);
            //allowance
            var approveResult =
                ContractManager.Token.ApproveToken(acc, DAppContract.ContractAddress, amount, AppSymbol);
            approveResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            approveResult =
                ContractManager.Token.ApproveToken(acc, DAppContract.ContractAddress, amount, symbol);
            approveResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            //use
            var dappStub = DAppContract.GetDAppStub(acc);
            for (var i = 0; i < executeTimes; i++)
            {
                var transactionResult = await dappStub.Use.SendAsync(new Record
                {
                    Type = RecordType.Use,
                    Description = $"Use test-{Guid.NewGuid()}",
                    Symbol = symbol
                });
                transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            }

            GetDAppContractBalance();
        }

        [TestMethod]
        [DataRow("SAPP")]
        public async Task DeveloperReceiveProfit_Test(string symbol)
        {
            Logger.Info("=>DeveloperReceiveProfit_Test");
            var beforeBalance = ContractManager.Token.GetUserBalance(NodesAccounts[0], symbol);

            var tokenStub = ContractManager.Genesis.GetTokenImplStub(NodesAccounts[0]);
            var profits = ContractManager.Token.GetUserBalance(DAppContract.ContractAddress, symbol);
            var receiveResult = await tokenStub.ReceiveProfits.SendAsync(new ReceiveProfitsInput
            {
                ContractAddress = DAppContract.Contract,
                Symbol = symbol,
                Amount = profits / 2
            });
            receiveResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var afterBalance = ContractManager.Token.GetUserBalance(NodesAccounts[0], symbol);
            Logger.Info($"Contract Profit: {profits}, Developer account balance: {beforeBalance} => {afterBalance}");
            GetDAppContractBalance();
        }

        [TestMethod]
        [DataRow(new[] {0, 1, 2, 3})]
        public async Task InvestorReceiveResourceProfit_Test(int[] idArray)
        {
            Logger.Info("=>InvestorReceiveProfit_Test");
            foreach (var id in idArray)
            {
                Logger.Info($"Claim Profit Id = {id}");
                var beforeBalance = ContractManager.Token.GetUserBalance(Investors[id], AppSymbol);

                var claimProfitsResult = await ContractManager.TokenHolderStub.ClaimProfits.SendAsync(
                    new ClaimProfitsInput
                    {
                        Beneficiary = Investors[id].ConvertAddress(),
                        SchemeManager = DAppContract.Contract
                    });
                claimProfitsResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

                var afterBalance = ContractManager.Token.GetUserBalance(Investors[id], AppSymbol);
                Logger.Info(
                    $"Investor profit balance change: {beforeBalance} => {afterBalance}, profit: {afterBalance - beforeBalance}");
                GetDAppContractBalance();
            }
        }

        [TestMethod]
        [DataRow(new[] {0, 1, 2, 3})]
        public async Task InvestorReceiveProfit_Test(int[] idArray)
        {
            Logger.Info("=>InvestorReceiveProfit_Test");
            foreach (var id in idArray)
            {
                Logger.Info($"Claim Profit Id = {id}");
                var beforeBalance = ContractManager.Token.GetUserBalance(Investors[id], PrimarySymbol);

                var claimProfitsResult = await ContractManager.TokenHolderStub.ClaimProfits.SendAsync(
                    new ClaimProfitsInput
                    {
                        Beneficiary = Investors[id].ConvertAddress(),
                        SchemeManager = DAppContract.Contract
                    });
                claimProfitsResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

                var afterBalance = ContractManager.Token.GetUserBalance(Investors[id], PrimarySymbol);
                Logger.Info(
                    $"Investor profit balance change: {beforeBalance} => {afterBalance}, profit: {afterBalance - beforeBalance}");
                GetDAppContractBalance();
            }
        }

        [TestMethod]
        public async Task WithDrawAndClaimProfit()
        {
            var investor = Investors[3];
            //WithDraw
            //allowance
            var tokenStub = ContractManager.Genesis.GetTokenStub(investor);
            var approveResult = await tokenStub.Approve.SendAsync(new ApproveInput
            {
                Spender = DAppContract.Contract,
                Symbol = AppSymbol,
                Amount = 40_00000000L
            });
            approveResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var beforeBalance = ContractManager.Token.GetUserBalance(investor, PrimarySymbol);
            var dappStub = DAppContract.GetDAppStub(investor);
            var withdrawResult = await dappStub.Withdraw.SendAsync(new WithdrawInput
            {
                Symbol = PrimarySymbol,
                Amount = 40_00000000L
            });
            withdrawResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var txFee = withdrawResult.TransactionResult.GetDefaultTransactionFee();
            var afterBalance = ContractManager.Token.GetUserBalance(investor, PrimarySymbol);
            beforeBalance.ShouldBe(afterBalance + txFee - 40_00000000);

            //Claim  profit
            beforeBalance = ContractManager.Token.GetUserBalance(investor, PrimarySymbol);
            var claimProfitsResult = await ContractManager.TokenHolderStub.ClaimProfits.SendAsync(
                new ClaimProfitsInput
                {
                    Beneficiary = investor.ConvertAddress(),
                    SchemeManager = DAppContract.Contract
                });
            claimProfitsResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            afterBalance = ContractManager.Token.GetUserBalance(investor, PrimarySymbol);
            Logger.Info(
                $"Investor profit balance change: {beforeBalance} => {afterBalance}, profit: {afterBalance - beforeBalance}");
        }

        [TestMethod]
        public async Task InvestorWithDraw_Test()
        {
            var investor = Investors[2];
            //allowance
            var tokenStub = ContractManager.Genesis.GetTokenStub(investor);
            var approveResult = await tokenStub.Approve.SendAsync(new ApproveInput
            {
                Spender = DAppContract.Contract,
                Symbol = AppSymbol,
                Amount = 10_00000000L
            });
            approveResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var beforeBalance = ContractManager.Token.GetUserBalance(investor, PrimarySymbol);
            var dappStub = DAppContract.GetDAppStub(investor);
            var withdrawResult = await dappStub.Withdraw.SendAsync(new WithdrawInput
            {
                Symbol = PrimarySymbol,
                Amount = 10_00000000L
            });
            withdrawResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var txFee = withdrawResult.TransactionResult.GetDefaultTransactionFee();
            var afterBalance = ContractManager.Token.GetUserBalance(investor, PrimarySymbol);
            beforeBalance.ShouldBe(afterBalance + txFee - 10_00000000);
        }

        [TestMethod]
        public async Task Summary_Prepare_Test()
        {
            await InitializeDAppContract_Test();
            await SignUp_Deposit_RegistProfit_Test();
            await NodesSignUp_Test();
        }

        [TestMethod]
        public async Task Summary_Case_Test()
        {
            await InitializeDAppContract_Test();
            await SignUp_Deposit_RegistProfit_Test();
            await NodesSignUp_Test();
            await Use_Test(new[] {0, 1, 2, 3});
            await DeveloperReceiveProfit_Test(PrimarySymbol);
            await InvestorReceiveProfit_Test(new[] {0, 1, 2, 3});
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
                var balance = ContractManager.Token.GetUserBalance(acc, PrimarySymbol);
                if (balance > 100_00000000) continue;
                //transfer token for test
                ContractManager.Token.TransferBalance(NodesAccounts[0], acc, 200_00000000, PrimarySymbol);
            }
        }

        private void GetDAppContractBalance()
        {
            var elfBalance = ContractManager.Token.GetUserBalance(DAppContract.ContractAddress, PrimarySymbol);
            var appBalance = ContractManager.Token.GetUserBalance(DAppContract.ContractAddress, AppSymbol);
            Logger.Info($"DApp balance: {PrimarySymbol}={elfBalance}/{AppSymbol}={appBalance}");
        }

        private async Task NodesPrepareResourceBalance(string account, string symbol)
        {
            var balance = ContractManager.Token.GetUserBalance(account, symbol);
            if (balance < 100_00000000)
            {
                var converter = ContractManager.Genesis.GetTokenConverterStub(account);
                var buyResult = await converter.Buy.SendAsync(new BuyInput
                {
                    Symbol = symbol,
                    Amount = 100_00000000
                });
                buyResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            }
        }

        private async Task DAppPrepareResourceBalance(string symbol)
        {
            var account = DAppContract.ContractAddress;
            var balance = ContractManager.Token.GetUserBalance(account, symbol);
            if (balance < 50_00000000)
            {
//                var converter = ContractManager.Genesis.GetTokenConverterStub(NodesAccounts[0]);
//                var buyResult = await converter.Buy.SendAsync(new BuyInput
//                {
//                    Symbol = symbol,
//                    Amount = 1000_00000000
//                });
//                buyResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

                var sendResult =
                    ContractManager.Token.TransferBalance(NodesAccounts[0], account, 50_00000000, symbol);
                sendResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            }

            await Task.CompletedTask;
        }
    }
}