using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
        private readonly string _account;
        private readonly INodeManager _nodeManager;
        private AEDPoSContractImplContainer.AEDPoSContractImplStub _consensusImplStub;
        private readonly ConcurrentQueue<Hash> hashQueue;

        public ILog Logger = Log4NetHelper.GetLogger();

        public RandomGenerate(INodeManager nodeManager, string account)
        {
            _nodeManager = nodeManager;
            hashQueue = new ConcurrentQueue<Hash>();
            _account = account;
        }

        public async Task GenerateAndCheckRandomNumbers(int count)
        {
            var accounts = GetAvailableAccounts();
            var genesis = _nodeManager.GetGenesisContract();
            GetConsensusStub();
            var tasks = new List<Task>
            {
                Task.Run(async () =>
                {
                    for (var i = 0; i < count; i++)
                    {
                        var id = CommonHelper.GenerateRandomNumber(0, accounts.Count);
                        _consensusImplStub = genesis.GetConsensusImplStub(accounts[id]);
                        await GenerateRandomHash();
                        await Task.Delay(500);
                    }
                }),
                Task.Run(async ()=> await GetAllRandomHash())
            };
            Task.WaitAll(tasks.ToArray());
        }

        private void GetConsensusStub()
        {
            var genesis = GenesisContract.GetGenesisContract(_nodeManager, _account);

            _consensusImplStub = genesis.GetConsensusImplStub();
        }

        private async Task GenerateRandomHash()
        {
            var roundInfo = await _consensusImplStub.GetCurrentRoundNumber.CallAsync(new Empty());
            Logger.Info($"Current round info: {roundInfo.Value}");
            var randomOrder = await _consensusImplStub.RequestRandomNumber.SendAsync(new Empty());
            if(!hashQueue.Contains(randomOrder.Output.TokenHash))
                hashQueue.Enqueue(randomOrder.Output.TokenHash);
            var blockHeight = randomOrder.Output.BlockHeight;
            var tokenHash = randomOrder.Output.TokenHash;
            Logger.Info($"Random token height: {blockHeight}, hash: {tokenHash}");
        }

        private async Task GetAllRandomHash()
        {
            await Task.Delay(10 * 1000);
            while (hashQueue.TryDequeue(out var tokenHash))
            {
                try
                {
                    var randomResult = await _consensusImplStub.GetRandomNumber.CallAsync(tokenHash);
                    if (!randomResult.Equals(new Hash()))
                    {
                        Logger.Info($"Random hash: {tokenHash}=>{randomResult}");
                        continue;
                    }

                    hashQueue.Enqueue(tokenHash);
                    var currentRound = await _consensusImplStub.GetCurrentRoundNumber.CallAsync(new Empty());
                    var height = await _nodeManager.ApiService.GetBlockHeightAsync();
                    Logger.Info($"Current information: round={currentRound.Value}, height={height}");
                    await Task.Delay(1000);
                }
                catch (Exception e)
                {
                    e.Message.WriteErrorLine();
                    hashQueue.Enqueue(tokenHash);
                    await Task.Delay(1000);
                }
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
                if(balance > 10000_0000)
                    availableAccounts.Add(acc);
            }

            return availableAccounts;
        }
    }
}