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
            var depositAmount = 100000_000000000;
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
        [DataRow("1d5461213d84bbc5076f3599fc90fd12d51d0bfaa13dc021981a69ffa48caf78")]
        public async Task AddSwapRound(string pairId)
        {
            var pId = HashHelper.HexStringToHash(pairId);
            var result = await _tokenSwapContractStub.AddSwapRound.SendAsync(new AddSwapRoundInput
            {
                PairId = pId,
                MerkleTreeRoot = HashHelper.HexStringToHash("717ff88b74be72704cf6ebd2b04944531fd428032ddee51b7d3c86e16ee11c07")
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
        [DataRow("f627326f95b6815a0d51d43c23c41728b8221713e3a285de109b19a296b578ae")]
        public async Task ChangeSwapRatio(string sPairId)
        {
            var pairId = HashHelper.HexStringToHash(sPairId);
            var result = await _tokenSwapContractStub.ChangeSwapRatio.SendAsync(new ChainSwapRatioInput()
            {
                PairId = pairId,
                SwapRatio = new SwapRatio
                {
                    OriginShare = 100_0000000,
                    TargetShare = 1,              
                }
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }
        

        [TestMethod]
        [DataRow("1d5461213d84bbc5076f3599fc90fd12d51d0bfaa13dc021981a69ffa48caf78","0xc09322c415a5ac9ffb1a6cde7e927f480cc1d8afaf22b39a47797966c08e9c4b")]
        public async Task SwapToken(string sPairId,string sUniqueId)
        {
            var receiveAccount = "28Y8JA1i2cN6oHvdv7EraXJr9a1gY6D1PpJXw9QtRMRwKcBQMK";
            var beforeBalance = _tokenContract.GetUserBalance(receiveAccount, Symbol);
            var originAmount = "13000000000000000000";
            var pairId = HashHelper.HexStringToHash(sPairId);
            var uniqueId = HashHelper.HexStringToHash(sUniqueId);
            var merklePath = new MerklePath
            {
                MerklePathNodes =
                {
                    new MerklePathNode
                    {
                        Hash = HashHelper.HexStringToHash(
                            "0xfc19ef2b8c423b51d1d542fc89eeeaa64f5eead96a6c5fcf51cae568e3233659"),
                        IsLeftChildNode = true
                    },
                    new MerklePathNode
                    {
                        Hash = HashHelper.HexStringToHash(
                            "0xe666b6218a52bfe5f5d33692fc442c16d9d24ce1f788c355223a86aca0c8a6b3"),
                        IsLeftChildNode = true
                    },
                    new MerklePathNode
                    {
                        Hash = HashHelper.HexStringToHash(
                            "0xef25f8d577585328513447fc29b8818bc3fdb0dd81744f8f2bbc05626697e223"),
                        IsLeftChildNode = false
                    },
                    new MerklePathNode
                    {
                        Hash = HashHelper.HexStringToHash(
                            "0x7142925859b69a57650c847a08c1ee0df571acbb6a0493309848c80b0b9719c4"),
                        IsLeftChildNode = true
                    },
                    new MerklePathNode
                    {
                        Hash = HashHelper.HexStringToHash(
                            "0xf30c892f311cac0510294f3365ac9f6ce330786c7aa7aac3f30a94dbfed236e6"),
                        IsLeftChildNode = false
                    },
                    new MerklePathNode
                    {
                        Hash = HashHelper.HexStringToHash(
                            "0xddf3904913e17d96db2a14282314dc826a859b31082b7d33eceb9a3a68d7079f"),
                        IsLeftChildNode = false
                    }
                }
            };
            var result = await _tokenSwapContractStub.SwapToken.SendAsync(new SwapTokenInput
            {
                PairId = pairId,
                OriginAmount = originAmount,
                UniqueId = uniqueId,
                ReceiverAddress = receiveAccount.ConvertAddress(),
                MerklePath = merklePath
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var tokenTransferredEvent = result.TransactionResult.Logs
                .First(l => l.Name == nameof(Transferred));
            var nonIndexed = Transferred.Parser.ParseFrom(tokenTransferredEvent.NonIndexed);
            var expectedAmount = 1300000000;
            nonIndexed.Amount.ShouldBe(expectedAmount);
//            Transferred.Parser.ParseFrom(tokenTransferredEvent.Indexed[1]).To.ShouldBe(TestAccount.ConvertAddress());
//            Transferred.Parser.ParseFrom(tokenTransferredEvent.Indexed[2]).Symbol.ShouldBe(Symbol);
            var balance = _tokenContract.GetUserBalance(receiveAccount, Symbol);
            balance.ShouldBe(beforeBalance+expectedAmount);
        }

        [TestMethod]
        [DataRow("1d5461213d84bbc5076f3599fc90fd12d51d0bfaa13dc021981a69ffa48caf78")]
        public async Task GetSwapInfo(string sPairId)
        {
            var pairId = HashHelper.HexStringToHash(sPairId);
            var swapPair = await _tokenSwapContractStub.GetSwapPair.CallAsync(pairId);
            var swapRound = await _tokenSwapContractStub.GetCurrentSwapRound.CallAsync(pairId);
            Logger.Info($"All the amount is {swapPair.SwappedAmount}");
            Logger.Info($"times is {swapPair.SwappedTimes}");
            Logger.Info($"Current amount is {swapRound.SwappedAmount}");
            Logger.Info($"Current times is {swapRound.SwappedTimes}");
            Logger.Info($"Merkle root is {swapRound.MerkleTreeRoot}");
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