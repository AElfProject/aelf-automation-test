using System.Linq;
using System.Threading.Tasks;
using AElfChain.Common;
using AElfChain.Common.Managers;
using Google.Protobuf.WellKnownTypes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace AElf.Automation.E2ETest.ContractSuits
{
    [TestClass]
    public class CrosschainTests : ContractTestBase
    {
        [TestMethod]
        public async Task GetSideChainIdAndHeight_Test()
        {
            var result = await ContractManager.CrossChainStub.GetSideChainIdAndHeight.CallAsync(new Empty());
            result.IdHeightDict.Count.ShouldBe(2);
            result.IdHeightDict.Values.ShouldAllBe(o => o > 1);
        }

        [TestMethod]
        public async Task GetSideChainIndexingInformationList_Test()
        {
            var result =
                await ContractManager.CrossChainStub.GetSideChainIndexingInformationList.CallAsync(new Empty());
            result.IndexingInformationList.Count.ShouldBe(2);
            var chainIds = result.IndexingInformationList.Select(o => o.ChainId).ToList();
            chainIds.ShouldContain(1866392);
            chainIds.ShouldContain(1931928);
        }

        [TestMethod]
        public async Task GetSideChainCreator_Test()
        {
            var chainIdAndHeightDict =
                await ContractManager.CrossChainStub.GetAllChainsIdAndHeight.CallAsync(new Empty());
            var chainIds = chainIdAndHeightDict.IdHeightDict.Keys;
            foreach (var chainId in chainIds)
            {
                var creator = await ContractManager.CrossChainStub.GetSideChainCreator.CallAsync(new Int32Value
                {
                    Value = chainId
                });
                ConfigNodes.Select(o => o.Account).ShouldContain(creator.GetFormatted());
            }
        }

        [TestMethod]
        public async Task GetSideChainBalance_Test()
        {
            var chainIdAndHeightDict =
                await ContractManager.CrossChainStub.GetAllChainsIdAndHeight.CallAsync(new Empty());
            var chainIds = chainIdAndHeightDict.IdHeightDict.Keys;
            foreach (var chainId in chainIds)
            {
                var balanceInfo = await ContractManager.CrossChainStub.GetSideChainBalance.CallAsync(new Int32Value
                {
                    Value = chainId
                });
                balanceInfo.Value.ShouldBeGreaterThan(0);
            }
        }

        [TestMethod]
        public async Task GetParentChainId_Test()
        {
            var chainStatus = await ContractManager.NodeManager.ApiClient.GetChainStatusAsync();
            var chainId = ChainHelper.ConvertBase58ToChainId(chainStatus.ChainId);

            NodeInfoHelper.SetConfig(SideConfig);
            var sideNode = NodeInfoHelper.Config.Nodes.First();
            var nodeManager = new NodeManager(sideNode.Endpoint);
            var contractManager = new ContractManager(nodeManager, sideNode.Account);

            var parentChainId = await contractManager.CrossChainStub.GetParentChainId.CallAsync(new Empty());
            parentChainId.Value.ShouldBe(chainId);
        }
    }
}