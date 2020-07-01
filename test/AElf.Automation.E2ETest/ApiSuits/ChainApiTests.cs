using System.Linq;
using System.Threading.Tasks;
using AElf.Client.Dto;
using AElf.Contracts.MultiToken;
using AElf.Types;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Managers;
using Google.Protobuf;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
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

        [TestMethod]
        public async Task MerklePathByTransactionId_Test()
        {
            var height = await Client.GetBlockHeightAsync();
            var block = await Client.GetBlockByHeightAsync(height - 100, true);
            var transaction = block.Body.Transactions.First();
            var merklePath = await Client.GetMerklePathByTransactionIdAsync(transaction);

            var transactionInfo = await Client.GetTransactionResultAsync(transaction);
            var getMerklePath = CrossChainManager.GetMerklePath(transactionInfo.BlockNumber, transaction, out var root);
            var hash = Hash.LoadFromHex(merklePath.MerklePathNodes.First().Hash);
            hash.ShouldBe(getMerklePath.MerklePathNodes.First().Hash);
        }
        
        [TestMethod]
        public async Task ExecuteTransaction()
        {
            var from = "28Y8JA1i2cN6oHvdv7EraXJr9a1gY6D1PpJXw9QtRMRwKcBQMK";
            var to = "JRmBduh4nXWi1aXgdUsj5gJrzeZb2LxmrAbf7W99faZSvoAaE";
            var method = nameof(TokenMethod.GetBalance);
            var input = new GetBalanceInput
            {
                Owner = from.ConvertAddress(),
                Symbol = "ELF"
            };
            var rawTransaction = NodeManager.GenerateRawTransaction(from,to,method,input);
            Logger.Info($"rawTransaction: {rawTransaction}");
            var executedInput = new ExecuteTransactionDto
            {
                RawTransaction = rawTransaction
            };
           var result = await Client.ExecuteTransactionAsync(executedInput);
           var owner = GetBalanceOutput.Parser.ParseFrom(ByteArrayHelper.HexStringToByteArray(result)).Owner;
           owner.ShouldBe(from.ConvertAddress());
        }
        
        [TestMethod]
        public async Task ExecuteRawTransaction()
        {
            var from = "2bmNbLLR94QATnweMcVwLpUAPXt21x8k4zyMxQ7fXSySsrhUNm";
            var to = "JRmBduh4nXWi1aXgdUsj5gJrzeZb2LxmrAbf7W99faZSvoAaE";
            var method = nameof(TokenMethod.GetBalance);
            var bestChain = await Client.GetChainStatusAsync();
            var input = new CreateRawTransactionInput
            {
                MethodName = method,
                From = from,
                To = to,
                Params = new JObject
                {
                    ["symbol"] = "ELF",
                    ["owner"] = new JObject
                    {
                        ["value"] = from.ConvertAddress().Value
                            .ToBase64()
                    }
                }.ToString(),
                RefBlockNumber = bestChain.BestChainHeight,
                RefBlockHash = bestChain.BestChainHash,
            };
            var rawTransaction = await  Client.CreateRawTransactionAsync(input);
            Logger.Info($"rawTransaction: {rawTransaction.RawTransaction}");

            var transactionId =
                HashHelper.ComputeFrom(ByteArrayHelper.HexStringToByteArray(rawTransaction.RawTransaction));
            var signature = NodeManager.TransactionManager.Sign(from, transactionId.ToByteArray())
                .ToByteArray().ToHex();
            Logger.Info($"signature: {signature}");
            
            var rawTransactionResult =
                await NodeManager.ApiClient.ExecuteRawTransactionAsync(new ExecuteRawTransactionDto
                {
                    RawTransaction = rawTransaction.RawTransaction,
                    Signature = signature
                });
            rawTransactionResult.ShouldContain(from);
            Logger.Info($"result: {rawTransactionResult}");
        }
        
        [TestMethod]
        public async Task SendRawTransaction()
        {
            var from = "Jx2X1BVJ23WteQDwrUfWQ4Axq9Kb9un1aZNRsiUBWuMMfDWme";
            var to = "JRmBduh4nXWi1aXgdUsj5gJrzeZb2LxmrAbf7W99faZSvoAaE";
            var method = nameof(TokenMethod.Transfer);
            var bestChain = await Client.GetChainStatusAsync();
            var input = new CreateRawTransactionInput
            {
                MethodName = method,
                From = from,
                To = to,
                Params = new JObject
                {
                    ["symbol"] = "ELF",
                    ["to"] = new JObject
                    {
                        ["value"] = to.ConvertAddress().Value
                            .ToBase64()
                    },
                    ["amount"] = 1000
                }.ToString(),
                RefBlockNumber = bestChain.BestChainHeight,
                RefBlockHash = bestChain.BestChainHash,
            };
            var rawTransaction = await  Client.CreateRawTransactionAsync(input);
            Logger.Info($"rawTransaction: {rawTransaction.RawTransaction}");

            var transactionId =
                HashHelper.ComputeFrom(ByteArrayHelper.HexStringToByteArray(rawTransaction.RawTransaction));
            var signature = NodeManager.TransactionManager.Sign(from, transactionId.ToByteArray())
                .ToByteArray().ToHex();
            Logger.Info($"signature: {signature}");
            
            var rawTransactionResult =
                await NodeManager.ApiClient.SendRawTransactionAsync(new SendRawTransactionInput
                {
                    Transaction = rawTransaction.RawTransaction,
                    Signature = signature,
                    ReturnTransaction = true
                });
            rawTransactionResult.Transaction.From.ShouldBe(from);
            rawTransactionResult.Transaction.To.ShouldBe(to);
            rawTransactionResult.TransactionId.ShouldBe(transactionId.ToHex());
            Logger.Info($"Transaction id : {rawTransactionResult.TransactionId}");

            var status = await Client.GetTransactionResultAsync(rawTransactionResult.TransactionId);
            status.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        }
    }
}