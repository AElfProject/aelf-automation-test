using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.TestContract.RandomNumberProvider;
using AElf.Types;
using AElfChain.Common.Contracts;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using log4net;

namespace AElf.Automation.ContractsTesting
{
    public class RandomGenerate
    {
        public static readonly ILog Logger = Log4NetHelper.GetLogger();
        private readonly string _account;
        private readonly ConcurrentQueue<Hash> _hashQueue;
        private readonly INodeManager _nodeManager;
        private AEDPoSContractImplContainer.AEDPoSContractImplStub _consensusImplStub;
        private RandomNumberProviderContract _randomNumberProviderContract;
        private RandomNumberProviderContractContainer.RandomNumberProviderContractStub _randomNumberStub;

        public RandomGenerate(INodeManager nodeManager, string account)
        {
            _nodeManager = nodeManager;
            _hashQueue = new ConcurrentQueue<Hash>();
            _account = account;
            _randomNumberProviderContract = new RandomNumberProviderContract(_nodeManager,account);
            _randomNumberStub = _randomNumberProviderContract
                .GetTestStub<RandomNumberProviderContractContainer.RandomNumberProviderContractStub>(account);
        }

        public async Task GenerateAndCheckRandomNumbers(int count)
        {
            var accounts = GetAvailableAccounts();
            var genesis = _nodeManager.GetGenesisContract();
            GetConsensusStub();
            var tasks = new List<Task>
            {
                Task.Run(async () => await GetRandomHashByHeight(count)),
                Task.Run(async () => await GetRandomByte(count))
            };
            Task.WaitAll(tasks.ToArray());

            await Task.CompletedTask;
        }

        private void GetConsensusStub()
        {
            var genesis = GenesisContract.GetGenesisContract(_nodeManager, _account);
            
            _consensusImplStub = genesis.GetConsensusImplStub();
        }
        
        private async Task GetRandomHashByHeight(int count)
        {
            var height = await _nodeManager.ApiClient.GetBlockHeightAsync();
            var checkHeight = height > count ? count : height;
            for (var i = height; i > height - checkHeight; i--)
            {
                var randomHashResult = await _consensusImplStub.GetRandomHash.CallAsync(new Int64Value{Value = i});
                if (!_hashQueue.Contains(randomHashResult))
                    _hashQueue.Enqueue(randomHashResult);
                else
                    Logger.Error($"Random hash: height {i}=>{randomHashResult} is repeated.");
                Logger.Info($"Random hash: height {i}=>{randomHashResult}");
            }
        }
        
        private async Task GetRandomByte(int count)
        {
            var height = await _nodeManager.ApiClient.GetBlockHeightAsync();
            var checkHeight = height > count ? count : height;
            for (var i = height; i > height - checkHeight; i--)
            {
                var blockHash = await _nodeManager.ApiClient.GetBlockByHeightAsync(i);
                var byteString = Hash.LoadFromHex(blockHash.BlockHash).ToByteString();
                
                var randomBytes1 = await _randomNumberStub.GetRandomBytes.CallAsync(new GetRandomBytesInput
                {
                    Kind = 1,
                    Value = HashHelper.ComputeFrom("Test1").ToByteString()
                }.ToBytesValue());
                
                var randomBytes = await _randomNumberStub.GetRandomBytes.CallAsync(new GetRandomBytesInput
                {
                    Kind = 1,
                    Value = byteString
                }.ToBytesValue());
                
                var randomHash = new Hash();
                randomHash.MergeFrom(randomBytes.Value);
                if (!_hashQueue.Contains(randomHash))
                    _hashQueue.Enqueue(randomHash);
                else
                    Logger.Error($"Random hash: height {i}=>{randomHash} is repeated.");
                Logger.Info($"Random hash: height {i}=>{randomHash}");
            }
            
            var randomIntegers = new List<long>();
            for (var i = height; i > height - checkHeight; i--)
            {
                var blockHash = await _nodeManager.ApiClient.GetBlockByHeightAsync(i);
                var byteString = Hash.LoadFromHex(blockHash.BlockHash).ToByteString();
                
                var randomBytes = await _randomNumberStub.GetRandomBytes.CallAsync(new GetRandomBytesInput
                {
                    Kind = 2,
                    Value = byteString
                }.ToBytesValue());
                var randomNumber = new Int64Value();
                randomNumber.MergeFrom(randomBytes.Value);
                if (!randomIntegers.Contains(randomNumber.Value))
                    randomIntegers.Add(randomNumber.Value);
                else
                    Logger.Error($" block hash: height {i}=>{randomNumber} is repeated.");
                Logger.Info($"block hash {blockHash.BlockHash}: height {i}=>{randomNumber}");
            }
        }

        private List<string> GetAvailableAccounts()
        {
            var accounts = _nodeManager.ListAccounts();
            var genesis = _nodeManager.GetGenesisContract();
            var token = genesis.GetTokenContract();
            var primaryToken = _nodeManager.GetPrimaryTokenSymbol();
            var availableAccounts = new List<string>();
            foreach (var acc in accounts)
            {
                var balance = token.GetUserBalance(acc, primaryToken);
                if (balance > 10000_0000)
                    availableAccounts.Add(acc);
            }

            return availableAccounts;
        }
    }
}