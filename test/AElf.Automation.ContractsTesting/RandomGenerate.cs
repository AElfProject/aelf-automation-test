using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Acs6;
using AElfChain.Common.Contracts;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using log4net;

namespace AElf.Automation.ContractsTesting
{
    public class RandomGenerate
    {
        private readonly INodeManager _nodeManager;
        private readonly string _account;
        private AEDPoSContractImplContainer.AEDPoSContractImplStub _consensusImplStub;
        private ConcurrentQueue<Hash> hashQueue;

        public ILog Logger = Log4NetHelper.GetLogger();
        
        public RandomGenerate(INodeManager nodeManager, string account)
        {
            _nodeManager = nodeManager;
            hashQueue = new ConcurrentQueue<Hash>();
            _account = account;
        }

        public async Task GenerateAndCheckRandomNumbers(int count)
        {
            GetConsensusStub();
            for (var i = 0; i < count; i++)
            {
                await GenerateRandomHash();
            }

            await GetAllRandomHash();
        }

        private void GetConsensusStub()
        {
            var genesis = GenesisContract.GetGenesisContract(_nodeManager, _account);

            _consensusImplStub = genesis.GetConsensusImplStub();
        }

        private async Task<Hash> GenerateRandomHash()
        {
            var roundInfo = await _consensusImplStub.GetCurrentRoundNumber.CallAsync(new Empty());
            var height = await _nodeManager.ApiService.GetBlockHeightAsync();
            Logger.Info($"Current round info: {roundInfo.Value}");
            var randomOrder = await _consensusImplStub.RequestRandomNumber.SendAsync(new RequestRandomNumberInput
            {
                MinimumBlockHeight = CommonHelper.GenerateRandomNumber((int)height - 100, (int)height + 100)
            });
            hashQueue.Enqueue(randomOrder.Output.TokenHash);
            Logger.Info($"Random token info: {randomOrder.Output}");

            return randomOrder.Output.TokenHash;
        }

        private async Task<List<Hash>> GetAllRandomHash()
        {
            var randomHashCollection = new List<Hash>();
            while (hashQueue.TryDequeue(out var tokenHash))
            {
                var randomResult = await _consensusImplStub.GetRandomNumber.CallAsync(tokenHash);
                if (!randomResult.Equals(new Hash()))
                {
                    Logger.Info($"Random hash: {tokenHash}=>{randomResult}");
                    randomHashCollection.Add(randomResult);
                }
                
                hashQueue.Enqueue(tokenHash);
                var currentRound = await _consensusImplStub.GetCurrentRoundNumber.CallAsync(new Empty());
                var height = await _nodeManager.ApiService.GetBlockHeightAsync();
                Logger.Info($"Current information: round={currentRound.Value}, height={height}");
                await Task.Delay(1000);
            }

            return randomHashCollection;
        }
    }
}