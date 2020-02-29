using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.Types;
using AElfChain.Common;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using log4net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using Tokenswap;

namespace AElf.Automation.Contracts.ScenarioTest
{
    [TestClass]
    public class TokenSwapContractTest
    {
        private ILog Logger { get; set; }
        private INodeManager NodeManager { get; set; }
        private AuthorityManager AuthorityManager { get; set; }

        private TokenContract _tokenContract;
        private GenesisContract _genesisContract;
        private TokenSwapContract _tokenSwapContract;
        private TokenSwapContractContainer.TokenSwapContractStub _tokenSwapContractStub;


        private string InitAccount { get; } = "28Y8JA1i2cN6oHvdv7EraXJr9a1gY6D1PpJXw9QtRMRwKcBQMK";
        private string TestAccount { get; } = "2RCLmZQ2291xDwSbDEJR6nLhFJcMkyfrVTq1i1YxWC4SdY49a6";
        private static string RpcUrl { get; } = "192.168.197.14:8000";
        private string Symbol { get; } = "TEST";

        [TestInitialize]
        public void Initialize()
        {
            Log4NetHelper.LogInit("ContractTest");
            Logger = Log4NetHelper.GetLogger();
            NodeInfoHelper.SetConfig("nodes-env1-main");

            NodeManager = new NodeManager(RpcUrl);
            _genesisContract = GenesisContract.GetGenesisContract(NodeManager, InitAccount);
            _tokenContract = _genesisContract.GetTokenContract(InitAccount);
//            _tokenSwapContract = new TokenSwapContract(NodeManager, InitAccount);
//            Logger.Info($"TokenSwap contract : {_tokenSwapContract}");
            _tokenSwapContract = new TokenSwapContract(NodeManager,InitAccount,"uSXxaGWKDBPV6Z8EG8Et9sjaXhH1uMWEpVvmo2KzKEaueWzSe");
            _tokenSwapContractStub =
                _tokenSwapContract.GetTestStub<TokenSwapContractContainer.TokenSwapContractStub>(InitAccount);
            if (!_tokenContract.GetTokenInfo(Symbol).Symbol.Equals(Symbol))
                CreateTokenAndIssue();
        }

        [TestMethod]
        public async Task AddSwapPair()
        {
            var originTokenSizeInByte = 32;
            var swapRatio = new SwapRatio
            {
                OriginShare = 100_00000000,
                TargetShare = 1,
            };
            var depositAmount = 10000_000000000;
            _tokenContract.ApproveToken(InitAccount, _tokenSwapContract.ContractAddress, depositAmount, Symbol);
            var result = await _tokenSwapContractStub.AddSwapPair.SendAsync(new AddSwapPairInput
            {
                DepositAmount = depositAmount,
                OriginTokenSizeInByte = originTokenSizeInByte,
                TargetTokenSymbol = Symbol,
                SwapRatio = swapRatio,
                OriginTokenNumericBigEndian = true
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var pairId = result.Output;
            Logger.Info($"{pairId }");
            var swapPair = await _tokenSwapContractStub.GetSwapPair.CallAsync(pairId);
            swapPair.Controller.ShouldBe(InitAccount.ConvertAddress());
            swapPair.CurrentRound.ShouldBeNull();
            swapPair.SwappedAmount.ShouldBe(0);
            swapPair.SwappedTimes.ShouldBe(0);
            swapPair.SwapRatio.ShouldBe(swapRatio);
            swapPair.TargetTokenSymbol.ShouldBe(Symbol);
            swapPair.PairId.ShouldBe(pairId);
            swapPair.OriginTokenSizeInByte.ShouldBe(originTokenSizeInByte);
        }

        [TestMethod]
        [DataRow("f627326f95b6815a0d51d43c23c41728b8221713e3a285de109b19a296b578ae")]
        public async Task AddSwapRound(string pairId)
        {
            var pId = HashHelper.HexStringToHash(pairId);
            var result = await _tokenSwapContractStub.AddSwapRound.SendAsync(new AddSwapRoundInput
            {
                PairId = pId,
                MerkleTreeRoot = HashHelper.HexStringToHash("0x16c87a2137353f89e9174c3009f173b0d41f738aeca1c735d262188d52a30f6d")
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var swapPair = await _tokenSwapContractStub.GetSwapPair.CallAsync(pId);
            swapPair.Controller.ShouldBe(InitAccount.ConvertAddress());
            swapPair.CurrentRound.PairId.ShouldBe(pId);
//            swapPair.SwappedAmount.ShouldBe(0);
//            swapPair.SwappedTimes.ShouldBe(0);
            swapPair.TargetTokenSymbol.ShouldBe(Symbol);
            swapPair.PairId.ShouldBe(pId);
        }

        [TestMethod]
        [DataRow("f627326f95b6815a0d51d43c23c41728b8221713e3a285de109b19a296b578ae")]
        public async Task Deposit(string sPairId)
        {
            var depositAmount = 10000_000000000;
            _tokenContract.ApproveToken(InitAccount, _tokenSwapContract.ContractAddress, depositAmount, Symbol);
            var beforeBalance = _tokenContract.GetUserBalance(_tokenSwapContract.ContractAddress,Symbol);
            var pairId = HashHelper.HexStringToHash(sPairId);
            var result = await _tokenSwapContractStub.Deposit.SendAsync(new DepositInput
            {
                PairId = pairId,
                Amount = depositAmount
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var afterBalance = _tokenContract.GetUserBalance(_tokenSwapContract.ContractAddress,Symbol);
            afterBalance.ShouldBe(beforeBalance+depositAmount);
        }

        [TestMethod]
        [DataRow("f627326f95b6815a0d51d43c23c41728b8221713e3a285de109b19a296b578ae","0x4303ef0796bae63d9f52f7bf61ae2d37b57889452f2ad07bba769d90d354fe37")]
        public async Task SwapToken(string sPairId,string sUniqueId)
        {
            var beforeBalance = _tokenContract.GetUserBalance(TestAccount, Symbol);
            var originAmount = "14000000000000000000";
            var pairId = HashHelper.HexStringToHash(sPairId);
            var uniqueId = HashHelper.HexStringToHash(sUniqueId);
            var merklePath = new MerklePath
            {
                MerklePathNodes =
                {
                    new MerklePathNode
                    {
                        Hash = HashHelper.HexStringToHash(
                            "0x33eabfff93ba964820ad1e20b4eca0b8daf092343f7b987689f22e4462c504e6"),
                        IsLeftChildNode = false
                    },
                    new MerklePathNode
                    {
                        Hash = HashHelper.HexStringToHash(
                            "0xa59d054ab8ddd10785757cd14b7318035e4cbd010bc5356faf5390cc6d1c025b"),
                        IsLeftChildNode = false
                    },
                    new MerklePathNode
                    {
                        Hash = HashHelper.HexStringToHash(
                            "0xc0ba1fd92628e2191919c0758171514b46a431a7291f8bfbfd49254b23c694ef"),
                        IsLeftChildNode = true
                    }
                }
            };
            var result = await _tokenSwapContractStub.SwapToken.SendAsync(new SwapTokenInput
            {
                PairId = pairId,
                OriginAmount = originAmount,
                UniqueId = uniqueId,
                ReceiverAddress = TestAccount.ConvertAddress(),
                MerklePath = merklePath
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var tokenTransferredEvent = result.TransactionResult.Logs
                .First(l => l.Name == nameof(Transferred));
            var nonIndexed = Transferred.Parser.ParseFrom(tokenTransferredEvent.NonIndexed);
            var expectedAmount = 1400000000;
            nonIndexed.Amount.ShouldBe(expectedAmount);
            Transferred.Parser.ParseFrom(tokenTransferredEvent.Indexed[1]).To.ShouldBe(TestAccount.ConvertAddress());
            Transferred.Parser.ParseFrom(tokenTransferredEvent.Indexed[2]).Symbol.ShouldBe(Symbol);
            var balance = _tokenContract.GetUserBalance(TestAccount, Symbol);
            balance.ShouldBe(beforeBalance+expectedAmount);
        }

        [TestMethod]
        [DataRow("f627326f95b6815a0d51d43c23c41728b8221713e3a285de109b19a296b578ae")]
        public async Task GetSwapInfo(string sPairId)
        {
            var pairId = HashHelper.HexStringToHash(sPairId);
            var swapPair = await _tokenSwapContractStub.GetSwapPair.CallAsync(pairId);
            var swapRound = await _tokenSwapContractStub.GetCurrentSwapRound.CallAsync(pairId);
            Logger.Info($"All the amount is {swapPair.SwappedAmount}");
            Logger.Info($"times is {swapPair.SwappedTimes}");
            Logger.Info($"Current amount is {swapRound.SwappedAmount}");
            Logger.Info($"Current times is {swapRound.SwappedTimes}");
        }

        private void CreateTokenAndIssue()
        {
            var result = _tokenContract.ExecuteMethodWithResult(TokenMethod.Create, new CreateInput
            {
                Symbol = Symbol,
                TotalSupply = 10_00000000_00000000,
                Decimals = 8,
                Issuer = InitAccount.ConvertAddress(),
                IsBurnable = true,
                IsProfitable = true,
                TokenName = "TEST"
            });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            _tokenContract.IssueBalance(InitAccount, InitAccount, 5_00000000_00000000, Symbol);
        }
    }
}