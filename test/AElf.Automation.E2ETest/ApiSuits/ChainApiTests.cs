using System.Linq;
using System.Threading.Tasks;
using AElf.Types;
using AElfChain.Common.DtoExtension;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace AElf.Automation.E2ETest.ApiSuits
{
    [TestClass]
    public class ChainApiTests : ApiTestBase
    {
        [TestMethod]
        public async Task GetChainHeight_Test()
        {
            var height = await Client.GetBlockHeightAsync();
            height.ShouldBeGreaterThan(1);
        }

        [TestMethod]
        public async Task GetChainStatus_Test()
        {
            var chainStatus = await Client.GetChainStatusAsync();
            chainStatus.ChainId.ShouldBe("AELF");
            chainStatus.BestChainHeight.ShouldBeGreaterThan(1);
            chainStatus.LongestChainHeight.ShouldBeGreaterThan(1);
            chainStatus.BestChainHash.ShouldNotBe(Hash.Empty.ToHex());
            chainStatus.LongestChainHash.ShouldNotBe(Hash.Empty.ToHex());
        }

        [TestMethod]
        public async Task GetBlock_Test()
        {
            var chainStatus = await Client.GetChainStatusAsync();
            var blockByHeight = await Client.GetBlockByHeightAsync(chainStatus.BestChainHeight, true);
            var blockByHash = await Client.GetBlockByHashAsync(chainStatus.BestChainHash, true);
            blockByHeight.BlockHash.ShouldBe(chainStatus.BestChainHash);
            blockByHash.Header.Height.ShouldBe(chainStatus.BestChainHeight);
            blockByHeight.Body.TransactionsCount.ShouldBe(blockByHash.Body.TransactionsCount);
            blockByHeight.Body.Transactions.ShouldBe(blockByHash.Body.Transactions);
        }

        [TestMethod]
        public async Task GetTransactionPoolStatus_Test()
        {
            var transactionPoolStatus = await Client.GetTransactionPoolStatusAsync();
            transactionPoolStatus.Queued.ShouldBeGreaterThanOrEqualTo(0);
            transactionPoolStatus.Validated.ShouldBeGreaterThanOrEqualTo(0);
        }

        [TestMethod]
        public async Task GetContractFileDescriptor_Test()
        {
            var chainStatus = await Client.GetChainStatusAsync();
            var fileDescriptor = await Client.GetContractFileDescriptorSetAsync(chainStatus.GenesisContractAddress);
            fileDescriptor.ShouldNotBeNull();
            fileDescriptor.Length.ShouldBeGreaterThan(1000);
        }

        [TestMethod]
        public async Task GetTaskQueueStatus()
        {
            var taskQueueStatus = await Client.GetTaskQueueStatusAsync();
            taskQueueStatus.Count.ShouldBeGreaterThan(8);
            taskQueueStatus.First(o => o.Name == "BlockBroadcastQueue").Size.ShouldBeGreaterThanOrEqualTo(0);
            taskQueueStatus.First(o => o.Name == "TransactionBroadcastQueue").Size.ShouldBeGreaterThanOrEqualTo(0);
            taskQueueStatus.First(o => o.Name == "InitialSyncQueue").Size.ShouldBeGreaterThanOrEqualTo(0);
            taskQueueStatus.First(o => o.Name == "PeerReconnectionQueue").Size.ShouldBeGreaterThanOrEqualTo(0);
            taskQueueStatus.First(o => o.Name == "MergeBlockStateQueue").Size.ShouldBeGreaterThanOrEqualTo(0);
        }

        [TestMethod]
        public async Task GetMerklePathByTxId_Test()
        {
            var block = await Client.GetBlockByHeightAsync(1, true);
            var txId = block.Body.Transactions.First();

            var merklePath = await Client.GetMerklePathByTransactionIdAsync(txId);
            merklePath.MerklePathNodes.Count.ShouldBeGreaterThan(1);
        }

        [TestMethod]
        public async Task GetTransactionResult_Test()
        {
            var block = await Client.GetBlockByHeightAsync(1, true);
            var txId = block.Body.Transactions.First();

            var transactionResult = await Client.GetTransactionResultAsync(txId);
            transactionResult.ShouldNotBeNull();
            transactionResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            transactionResult.TransactionId.ShouldBe(txId);
            transactionResult.Error.ShouldBeNullOrEmpty();
        }

        [TestMethod]
        public async Task GetTransactionResults_Test()
        {
            var block = await Client.GetBlockByHeightAsync(1, true);
            var blockHash = block.BlockHash;

            var transactionResults = await Client.GetTransactionResultsAsync(blockHash, 0, 5);
            transactionResults.Count.ShouldBe(5);
            transactionResults.Select(o => o.Status.ConvertTransactionResultStatus())
                .ShouldAllBe(status => status == TransactionResultStatus.Mined);
        }
    }
}