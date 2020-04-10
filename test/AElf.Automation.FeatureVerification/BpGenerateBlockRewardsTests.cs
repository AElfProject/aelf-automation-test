using System.Linq;
using System.Threading.Tasks;
using Acs0;
using AElf.Contracts.Profit;
using AElf.Types;
using AElfChain.Common;
using AElfChain.Common.Contracts;
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
    public class BpGenerateBlockRewardsTests
    {
        private static readonly ILog Logger = Log4NetHelper.GetLogger();

        public BpGenerateBlockRewardsTests()
        {
            Log4NetHelper.LogInit();
            NodeInfoHelper.SetConfig("nodes-online-test-main");
            var firstNode = NodeInfoHelper.Config.Nodes.First();

            NodeManager = new NodeManager(firstNode.Endpoint);
            ContractManager = new ContractManager(NodeManager, firstNode.Account);
        }

        public INodeManager NodeManager { get; set; }
        public ContractManager ContractManager { get; set; }

        [TestMethod]
        public async Task GetCurrentTermNumberTest()
        {
            var term = await ContractManager.ConsensusStub.GetCurrentTermNumber.CallAsync(new Empty());
            Logger.Info($"Current term number: {term}");
        }

        [TestMethod]
        public async Task NodeGenerateBlockInfo()
        {
            var term = await ContractManager.ConsensusStub.GetCurrentTermNumber.CallAsync(new Empty());
            for (var i = 1; i < term.Value; i++)
            {
                var termInformation = await ContractManager.ConsensusStub.GetPreviousTermInformation.CallAsync(
                    new Int64Value
                    {
                        Value = i
                    });
                Logger.Info($"Term number: {termInformation.TermNumber}");
                foreach (var (key, value) in termInformation.RealTimeMinersInformation)
                    Logger.Info($"Pubkey: {key}, Count: {value.ProducedBlocks}");
            }
        }

        [TestMethod]
        public async Task QueryBpBasicProfit()
        {
            var accounts = NodeInfoHelper.Config.Nodes.Take(15).Select(o => o.Account);
            ContractManager.Profit.GetTreasurySchemes(ContractManager.Treasury.ContractAddress);
            var term = await ContractManager.ConsensusStub.GetCurrentTermNumber.CallAsync(new Empty());
            Logger.Info($"Current term number: {term.Value}");
            foreach (var acc in accounts)
            {
                var schemeId = ProfitContract.Schemes[SchemeType.MinerBasicReward].SchemeId;
                var profitDetails =
                    ContractManager.Profit.GetProfitDetails(acc, schemeId);
                var totalShares = 0L;
                foreach (var profit in profitDetails.Details)
                {
                    Logger.Info($"Period: {profit.StartPeriod}-{profit.EndPeriod}, Shares: {profit.Shares}");
                    totalShares += profit.Shares;
                }

                Logger.Info($"Account: {acc}, total shares: {totalShares}");
            }
        }

        [TestMethod]
        public async Task TakeBpRewardsTest()
        {
            var accounts = NodeInfoHelper.Config.Nodes.Take(4).Select(o => o.Account);
            ContractManager.Profit.GetTreasurySchemes(ContractManager.Treasury.ContractAddress);
            var schemeId = ProfitContract.Schemes[SchemeType.MinerBasicReward].SchemeId;
            foreach (var acc in accounts)
            {
                var beforeBalance = ContractManager.Token.GetUserBalance(acc);
                var profitAmount = ContractManager.Profit.GetProfitAmount(acc, schemeId);
                var claimProfitResult = await ContractManager.ProfitStub.ClaimProfits.SendAsync(new ClaimProfitsInput
                {
                    Beneficiary = acc.ConvertAddress(),
                    SchemeId = schemeId
                });
                claimProfitResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
                var transactionFee = claimProfitResult.TransactionResult.GetDefaultTransactionFee();
                var afterBalance = ContractManager.Token.GetUserBalance(acc);
                Logger.Info($"Account: {acc}");
                Logger.Info(
                    $"Balance change: {beforeBalance}=>{afterBalance}, ProfitAmount: {profitAmount}, TransactionFee: {transactionFee}");
            }
        }

        [TestMethod]
        [DataRow("0c807600de0a45e482bf083abd2fe49ded7d3edb132acd0f880db7eb04304d98")]
        public async Task ContractDelpyTransactionResultTest(string transactionId)
        {
            var transactionResult = await NodeManager.ApiClient.GetTransactionResultAsync(transactionId);
            var byteString =
                ByteString.FromBase64(transactionResult.Logs.First(l => l.Name.Contains(nameof(ContractDeployed)))
                    .NonIndexed);
            var contractDeployed = ContractDeployed.Parser.ParseFrom(byteString);
            contractDeployed.Version.ShouldBe(1);
            Logger.Info($"{JsonConvert.SerializeObject(contractDeployed)}");
        }

        [TestMethod]
        [DataRow("712b4b4d9843b3c2941a6f0612757beb2311921f47c739778669d983ecc2965d")]
        [DataRow("3c80ea852661d0cab2c3b5a24ab0ad7cb0cdba8d656f9d9f26a4f7a84f8efa78")]
        public async Task ContractUpdateTransactionResultTest(string transactionId)
        {
            var transactionResult = await NodeManager.ApiClient.GetTransactionResultAsync(transactionId);
            var byteString =
                ByteString.FromBase64(
                    transactionResult.Logs.First(l => l.Name.Contains(nameof(CodeUpdated))).NonIndexed);
            var codeUpdate = CodeUpdated.Parser.ParseFrom(byteString);
            Logger.Info($"{JsonConvert.SerializeObject(codeUpdate)}");
        }
    }
}