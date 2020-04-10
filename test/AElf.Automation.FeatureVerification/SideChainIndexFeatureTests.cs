using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Acs7;
using AElf.Types;
using AElfChain.Common;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using Google.Protobuf.WellKnownTypes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace AElf.Automation.Contracts.ScenarioTest
{
    [TestClass]
    public class SideChainIndexFeatureTests
    {
        public ILogHelper Logger = LogHelper.GetLogger();

        public SideChainIndexFeatureTests()
        {
            Log4NetHelper.LogInit();
            Logger.InitLogHelper();

            NodeInfoHelper.SetConfig("nodes-env1-main");
            var last = NodeInfoHelper.Config.Nodes.Last();
            NodeManager = new NodeManager(last.Endpoint);
            MainManager = new ContractManager(NodeManager, last.Account);
            Side1Manager = new ContractManager("192.168.197.14:8001", last.Account);
            Side2Manager = new ContractManager("192.168.197.14:8002", last.Account);
        }

        public INodeManager NodeManager { get; set; }
        public ContractManager MainManager { get; set; }
        public ContractManager Side1Manager { get; set; }
        public ContractManager Side2Manager { get; set; }

        [TestMethod]
        public async Task GetSideChainHeight()
        {
            //index side1
            var indexHeight1 = await MainManager.CrossChainStub.GetSideChainHeight.CallAsync(new Int32Value
            {
                Value = Side1Manager.ChainId
            });

            //index side2
            var indexHeight2 = await MainManager.CrossChainStub.GetSideChainHeight.CallAsync(new Int32Value
            {
                Value = Side2Manager.ChainId
            });

            Logger.Info($"Index height: Side1={indexHeight1.Value}, Side2={indexHeight2.Value}");
        }

        [TestMethod]
        public async Task GetIndexAndBalanceInfo()
        {
            await GetSideChainHeight();
            await GetSideChainBalance(Side1Manager.ChainId);
            await GetSideChainBalance(Side2Manager.ChainId);
        }

        [TestMethod]
        public async Task RechargeAndQueryBalance1()
        {
            await GetIndexAndBalanceInfo();

            var before = GetBpBalances();
            await RechargeSideChain1(5000_00);
            var after = GetBpBalances();
            for (var i = 0; i < 4; i++) Logger.Info($"Balance change: {after[i]}=>{before[i]} {after[i] - before[i]}");
        }

        [TestMethod]
        public async Task RechargeAndQueryBalance2()
        {
            await GetIndexAndBalanceInfo();

            var before = GetBpBalances();
            var rechargeFee = await RechargeSideChain2(8000_00L);
            Logger.Info($"RechargeFee: {rechargeFee}");
            var after = GetBpBalances();
            for (var i = 0; i < 4; i++) Logger.Info($"Balance change: {after[i]}=>{before[i]} {after[i] - before[i]}");
        }

        [TestMethod]
        public async Task<long> RechargeSideChain1(long amount)
        {
            var transactionResult = await MainManager.CrossChainStub.Recharge.SendAsync(new RechargeInput
            {
                ChainId = Side1Manager.ChainId,
                Amount = amount
            });
            transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            return transactionResult.TransactionResult.GetDefaultTransactionFee() + amount;
        }

        [TestMethod]
        public async Task<long> RechargeSideChain2(long amount)
        {
            var transactionResult = await MainManager.CrossChainStub.Recharge.SendAsync(new RechargeInput
            {
                ChainId = Side2Manager.ChainId,
                Amount = amount
            });
            transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            return transactionResult.TransactionResult.GetDefaultTransactionFee() + amount;
        }

        [TestMethod]
        public async Task AdjustIndexingFeePriceTest()
        {
            var chainId = ChainHelper.ConvertBase58ToChainId("tDVV");
            var proposer = NodeInfoHelper.Config.Nodes.First().Account;
            var association = MainManager.CrossChain.GetSideChainIndexingFeeController(chainId).AuthorityInfo
                .OwnerAddress;
            var adjustIndexingFeeInput = new AdjustIndexingFeeInput
            {
                IndexingFee = 10,
                SideChainId = chainId
            };
            var proposalId = MainManager.Association.CreateProposal(
                MainManager.CrossChain.ContractAddress,
                nameof(CrossChainContractMethod.AdjustIndexingFeePrice), adjustIndexingFeeInput, association,
                proposer);
            MainManager.Association.SetAccount(proposer);
            MainManager.Association.ApproveProposal(proposalId, proposer);
            var defaultOrganization =
                (await MainManager.CrossChainStub.GetSideChainLifetimeController.CallAsync(new Empty()))
                .OwnerAddress;
            var approveProposalId = MainManager.ParliamentAuth.CreateProposal(
                MainManager.Association.ContractAddress, nameof(AssociationMethod.Approve), proposalId,
                defaultOrganization, proposer);
            var currentMiners = MainManager.Authority.GetCurrentMiners();
            foreach (var miner in currentMiners) MainManager.ParliamentAuth.ApproveProposal(approveProposalId, miner);

            MainManager.ParliamentAuth.ReleaseProposal(approveProposalId, proposer);
            MainManager.Association.ReleaseProposal(proposalId, proposer);

            var afterCheckPrice = MainManager.CrossChain.GetSideChainIndexingFeePrice(chainId);
            afterCheckPrice.ShouldBe(10);
        }

        private async Task GetSideChainBalance(int chainId)
        {
            var balance = await MainManager.CrossChainStub.GetSideChainBalance.CallAsync(new Int32Value
            {
                Value = chainId
            });
            Logger.Info($"Chain {ChainHelper.ConvertChainIdToBase58(chainId)}-{chainId} balance: {balance.Value}");
        }

        private List<long> GetBpBalances()
        {
            var list = new List<long>();
            var nodes = NodeInfoHelper.Config.Nodes.Select(o => o.Account).ToList();
            Logger.Info("Check Bps balance.");
            for (var i = 0; i < 4; i++)
            {
                var j = i;
                var balance = MainManager.Token.GetUserBalance(nodes[j], "ELF");
                list.Add(balance);
                Logger.Info($"Account: {nodes[j]}, Balance: {balance}");
            }

            return list;
        }
    }
}