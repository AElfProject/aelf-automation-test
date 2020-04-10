using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Acs1;
using AElf.Contracts.MultiToken;
using AElf.Contracts.TokenConverter;
using AElf.Types;
using AElfChain.Common;
using AElfChain.Common.Contracts;
using AElfChain.Common.Contracts.Serializer;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using log4net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Shouldly;

namespace AElf.Automation.Contracts.ScenarioTest
{
    [TestClass]
    public class TestChainTests
    {
        public string TestSymbol = "STA";

        public TestChainTests()
        {
            Log4NetHelper.LogInit();
            Logger = Log4NetHelper.GetLogger();

            MainNode = new NodeManager("192.168.197.43:8100");
            SideNode1 = new NodeManager("3.84.143.239:8000");
            SideNode2 = new NodeManager("34.224.27.242:8000");

            BpAccount = "mPxf7UnKAGqkKRcwHTHv8Y9eTCG4vfbJpAfV1FLgMDS7wJGzt";
        }

        public INodeManager MainNode { get; set; }
        public INodeManager SideNode1 { get; set; }
        public INodeManager SideNode2 { get; set; }

        public string BpAccount { get; set; }
        public ILog Logger { get; set; }

        [TestMethod]
        [DataRow("", "TELF", 100_00000000)]
        public void TransferToken_Main(string to, string symbol, long amount)
        {
            var gensis = GenesisContract.GetGenesisContract(MainNode);
            var token = gensis.GetTokenContract();

            var beforeBalance = token.GetUserBalance(to, symbol);
            Logger.Info($"Before balance: {beforeBalance}");

            token.TransferBalance(BpAccount, to, amount, symbol);

            var afterBalance = token.GetUserBalance(to, symbol);
            Logger.Info($"After balance: {afterBalance}");
        }

        [TestMethod]
        [DataRow("TELF")]
        public async Task GetTokenConnector(string symbol)
        {
            var gensis = GenesisContract.GetGenesisContract(MainNode);
            var tokenConverter = gensis.GetTokenConverterStub();

            var result = await tokenConverter.GetPairConnector.CallAsync(new TokenSymbol
            {
                Symbol = symbol
            });

            Logger.Info($"Connector: {JsonConvert.SerializeObject(result)}");
        }

        [TestMethod]
        public async Task CreateConnector()
        {
            const long supply = 100_000_00000000;

            var gensis = GenesisContract.GetGenesisContract(MainNode);
            var tokenConverter = gensis.GetTokenConverterContract();

            var authority = new AuthorityManager(MainNode, BpAccount);
            var orgAddress = authority.GetGenesisOwnerAddress();
            var miners = authority.GetCurrentMiners();
            var connector = new Connector
            {
                Symbol = TestSymbol,
                IsPurchaseEnabled = true,
                IsVirtualBalanceEnabled = true,
                Weight = "0.5",
                VirtualBalance = supply
            };
            var transactionResult = authority.ExecuteTransactionWithAuthority(tokenConverter.ContractAddress,
                "SetConnector", connector, orgAddress, miners, BpAccount);
            transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            await GetTokenConnector(TestSymbol);
        }

        [TestMethod]
        [DataRow(5000_00000000, "CPU")]
        [DataRow(5000_00000000, "DISK")]
        [DataRow(5000_00000000, "NET")]
        [DataRow(5000_00000000, "RAM")]
        public async Task BuyResource(long amount, string symbol)
        {
            var gensis = GenesisContract.GetGenesisContract(MainNode, BpAccount);
            var tokenConverterAddress = gensis.GetTokenConverterContract().ContractAddress;
            var tokenConverter = gensis.GetTokenConverterStub();
            var token = gensis.GetTokenContract();
            Logger.Info($"Token converter token balance: {token.GetUserBalance(tokenConverterAddress, symbol)}");
            Logger.Info(
                $"Account: {BpAccount}, Before {NodeOption.NativeTokenSymbol}: {token.GetUserBalance(BpAccount)}");
            Logger.Info($"Account: {BpAccount}, Before {symbol}: {token.GetUserBalance(BpAccount, symbol)}");

            var transactionResult = await tokenConverter.Buy.SendAsync(new BuyInput
            {
                Symbol = symbol,
                Amount = amount,
                PayLimit = 0
            });
            CheckTransactionResult(transactionResult.TransactionResult);

            Logger.Info($"After {NodeOption.NativeTokenSymbol}: {token.GetUserBalance(BpAccount)}");
            Logger.Info($"After {TestSymbol}: {token.GetUserBalance(BpAccount, symbol)}");
        }

        [TestMethod]
        [DataRow(500)]
        public async Task SellResource(long amount)
        {
            var gensis = GenesisContract.GetGenesisContract(MainNode, BpAccount);
            var tokenConverter = gensis.GetTokenConverterStub();
            var token = gensis.GetTokenContract();
            var tokenStub = gensis.GetTokenStub();
            var tokenConverterAddress = gensis.GetTokenConverterContract().ContractAddress;

            Logger.Info($"Token converter token balance: {token.GetUserBalance(tokenConverterAddress, TestSymbol)}");
            Logger.Info($"Before {NodeOption.NativeTokenSymbol}: {token.GetUserBalance(BpAccount)}");
            Logger.Info($"Before {TestSymbol}: {token.GetUserBalance(BpAccount, TestSymbol)}");


            var allowanceResult = await tokenStub.Approve.SendAsync(new ApproveInput
            {
                Spender = tokenConverterAddress.ConvertAddress(),
                Symbol = TestSymbol,
                Amount = amount
            });
            CheckTransactionResult(allowanceResult.TransactionResult);

            var transactionResult = await tokenConverter.Sell.SendAsync(new SellInput
            {
                Symbol = TestSymbol,
                Amount = amount
            });
            CheckTransactionResult(transactionResult.TransactionResult);

            Logger.Info($"After {NodeOption.NativeTokenSymbol}: {token.GetUserBalance(BpAccount)}");
            Logger.Info($"After {TestSymbol}: {token.GetUserBalance(BpAccount, TestSymbol)}");
            Logger.Info($"Token converter token balance: {token.GetUserBalance(tokenConverterAddress, TestSymbol)}");
        }

        [TestMethod]
        public void CheckAllBpAccounts()
        {
            var bps = NodeInfoHelper.Config.Nodes;
            var genesis = GenesisContract.GetGenesisContract(MainNode);
            var token = genesis.GetTokenContract();

            foreach (var bp in bps)
            {
                var balance = token.GetUserBalance(bp.Account);
                Logger.Info($"Account: {bp.Account}, balance = {balance}");
            }

            var tokenConverterAddress = genesis.GetTokenConverterContract().ContractAddress;
            var tokenConverterTELF = token.GetUserBalance(tokenConverterAddress);
            var tokenConverterSTA = token.GetUserBalance(tokenConverterAddress, TestSymbol);
            Logger.Info($"TokenConverter: TELF={tokenConverterTELF}, STA={tokenConverterSTA}");
        }

        [TestMethod]
        public void SetTransactionFee_Main()
        {
            var authority = new AuthorityManager(MainNode, BpAccount);
            var miners = authority.GetCurrentMiners();
            var genesisOwner = authority.GetGenesisOwnerAddress();

            var genesis = MainNode.GetGenesisContract(BpAccount);
            var token = genesis.GetTokenContract();

            var input = new MethodFees
            {
                MethodName = "Approve",
                Fees =
                {
                    new MethodFee
                    {
                        Symbol = NodeOption.NativeTokenSymbol,
                        BasicFee = 1000
                    }
                }
            };
            var setTransactionFeeResult = authority.ExecuteTransactionWithAuthority(token.ContractAddress,
                "SetMethodFee", input, genesisOwner,
                miners, BpAccount);
            CheckTransactionResult(setTransactionFeeResult);
        }

        [TestMethod]
        public async Task VerifyTransactionFee_Main()
        {
            var genesis = MainNode.GetGenesisContract(BpAccount);
            var token = genesis.GetTokenContract();
            var tokenStub = genesis.GetTokenStub();

            var beforeBalance = token.GetUserBalance(BpAccount);

            var transactionResult = await tokenStub.Approve.SendAsync(new ApproveInput
            {
                Symbol = NodeOption.NativeTokenSymbol,
                Amount = 5000,
                Spender = genesis.Contract
            });
            CheckTransactionResult(transactionResult.TransactionResult);

            var afterBalance = token.GetUserBalance(BpAccount);
            Logger.Info($"Bp token: before={beforeBalance}, after={afterBalance}");
        }

        [TestMethod]
        public void SetTransactionFee_Side()
        {
        }

        [TestMethod]
        [DataRow("KDSkLLtkvKcAmFppPfRUGWdgtrPYVPRzYmCSA56tTaNcjgF7n")]
        public void TransferResource(string contract)
        {
            var symbols = new List<string> {"CPU", "NET", "DISK", "RAM"};
            var genesis = MainNode.GetGenesisContract(BpAccount);
            var token = genesis.GetTokenContract();
            foreach (var symbol in symbols) token.TransferBalance(BpAccount, contract, 5000_00000000, symbol);
        }

        [TestMethod]
        [DataRow("KDSkLLtkvKcAmFppPfRUGWdgtrPYVPRzYmCSA56tTaNcjgF7n")]
        public void GetContractResource(string contract)
        {
            var symbols = new List<string> {"CPU", "NET", "DISK", "RAM"};
            var genesis = MainNode.GetGenesisContract(BpAccount);
            var token = genesis.GetTokenContract();

            foreach (var symbol in symbols)
            {
                var balance = token.GetUserBalance(contract, symbol);
                Logger.Info($"Contract: {symbol}={balance}");
            }
        }

        [TestMethod]
        public void CheckTransaction_Fee()
        {
            var nodeUrls = new List<string>
            {
                "18.212.240.254:8000",
                "54.183.221.226:8000",
                "13.230.195.6:8000",
                "35.183.35.159:8000",
                "34.255.1.143:8000",
                "18.163.40.216:8000",
                "3.1.211.78:8000",
                "13.210.243.191:8000",
                "18.231.115.220:8000",
                "35.177.181.31:8000"
            };
            foreach (var url in nodeUrls)
            {
                Logger.Info($"Test endpoint: {url}");

                var nodeManager = new NodeManager(url);
                var genesis = nodeManager.GetGenesisContract();
                var token = genesis.GetTokenContract();

                var tokenAmount = token.CallViewMethod<MethodFees>(TokenMethod.GetMethodFee, new StringValue
                {
                    Value = "Transfer"
                });
                Logger.Info(tokenAmount);
            }
        }

        [TestMethod]
        public void QueryTransaction_Fee()
        {
            var genesis = MainNode.GetGenesisContract(BpAccount);
            var token = genesis.GetTokenContract();
            var transactionResultDto = token.ExecuteMethodWithResult("GetMethodFee", new StringValue
            {
                Value = "Transfer"
            });
            Logger.Info(JsonConvert.SerializeObject(transactionResultDto, Formatting.Indented));
        }

        [TestMethod]
        public void ContractMethodTest()
        {
            var contractHandler = new ContractSerializer();
            var tokenInfo = contractHandler.GetContractInfo(NameProvider.Genesis);
            var method = tokenInfo.GetContractMethod("GetContractInfo");

            var address = "2gaQh4uxg6tzyH1ADLoDxvHA14FMpzEiMqsQ6sDG5iHT8cmjp8".ConvertAddress();
            var output = JsonFormatter.Default.Format(address);
        }

        [TestMethod]
        public void TransferToken_Test()
        {
            var token = MainNode.GetGenesisContract().GetTokenContract();
            var accounts = NodeInfoHelper.Config.Nodes.Select(o => o.Account).ToList();

            for (var i = 0; i < 200; i++)
            {
                var fromId = CommonHelper.GenerateRandomNumber(0, accounts.Count - 1);
                var toId = CommonHelper.GenerateRandomNumber(0, accounts.Count - 1);
                if (fromId == toId)
                    continue;
                var tester = token.GetNewTester(accounts[fromId]);
                var amount = CommonHelper.GenerateRandomNumber(5000, 10000);
                var transactionId = tester.ExecuteMethodWithTxId(TokenMethod.Transfer, new TransferInput
                {
                    Symbol = "TELF",
                    To = accounts[toId].ConvertAddress(),
                    Amount = amount,
                    Memo = $"T-{Guid.NewGuid()}"
                });
                Logger.Info(
                    $"From: account{fromId}, To: account{toId}, Amount: {amount}, TransactionId: {transactionId}");
            }
        }

        [TestMethod]
        public void SendTwoTransaction_Test()
        {
            var accounts = NodeInfoHelper.Config.Nodes.Select(o => o.Account).ToList();
            var otherAccounts = MainNode.ListAccounts();
            var token = MainNode.GetGenesisContract().GetTokenContract();

            var rawInfo1 = MainNode.GenerateRawTransaction(accounts[0], token.ContractAddress, "Transfer",
                new TransferInput
                {
                    Symbol = "ELF",
                    To = otherAccounts[1].ConvertAddress(),
                    Amount = 1000,
                    Memo = $"T-{Guid.NewGuid()}"
                });

            var rawInfo2 = MainNode.GenerateRawTransaction(accounts[1], token.ContractAddress, "Transfer",
                new TransferInput
                {
                    Symbol = "ELF",
                    To = otherAccounts[2].ConvertAddress(),
                    Amount = 1000,
                    Memo = $"T-{Guid.NewGuid()}"
                });

            var rawTransactions = $"{rawInfo1},{rawInfo2}";
            var transactions = MainNode.SendTransactions(rawTransactions);
            foreach (var transaction in transactions) Logger.Info($"TransactionId: {transaction}");

            MainNode.CheckTransactionListResult(transactions);
        }

        private void CheckTransactionResult(TransactionResult result)
        {
            if (!result.Status.Equals(TransactionResultStatus.Mined))
                Logger.Error(result.Error);
        }
    }
}