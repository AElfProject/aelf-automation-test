using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using AElf.Client.Consensus.AEDPoS;
using AElf.Contracts.Profit;
using AElf.Types;
using AElfChain.Common.Contracts;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using Google.Protobuf.WellKnownTypes;
using log4net;

namespace AElf.Automation.MainChainEconomicTest
{
    public class MiningRewards
    {
        private static readonly ILog Logger = Log4NetHelper.GetLogger();

        public MiningRewards(INodeManager nodeManager, string caller)
        {
            NodeManager = nodeManager;
            Caller = caller;
            Genesis = nodeManager.GetGenesisContract(Caller);
            Profit = Genesis.GetProfitContract();
            Consensus = Genesis.GetConsensusContract();
            Token = Genesis.GetTokenContract();
            Treasury = Genesis.GetTreasuryContract();
            Profit.GetTreasurySchemes(Treasury.ContractAddress);
            Schemes = ProfitContract.Schemes;
            GetCurrentMiningRewardPerBlock();
            Reward = new Dictionary<long, long>();
        }

        private INodeManager NodeManager { get; }
        private GenesisContract Genesis { get; }
        private ProfitContract Profit { get; }
        private ConsensusContract Consensus { get; }
        private TreasuryContract Treasury { get; }
        private TokenContract Token { get; }
        private string Caller { get; }
        private long MiningRewardPerBlock { get; set; }
        public static Dictionary<SchemeType, Scheme> Schemes { get; set; }
        public static Dictionary<long, long> Reward { get; set; }


        public void GetCurrentRoundMinedBlockBonus()
        {
            var round = Consensus.GetCurrentTermInformation();
            var roundNumber = round.RoundNumber;
            var term = round.TermNumber;
            var blocksBonus = Consensus.GetCurrentWelfareReward().Value;
            var blockCount = blocksBonus / MiningRewardPerBlock;
            Logger.Info($"{term} {roundNumber}: {blockCount} {blocksBonus}");
        }

        public void CheckMinerProfit()
        {
            var MinerBasicReward = Schemes[SchemeType.MinerBasicReward].SchemeId;
            var ReElectionReward = Schemes[SchemeType.ReElectionReward].SchemeId;
            var VotesWeightReward = Schemes[SchemeType.VotesWeightReward].SchemeId;

            long amount = 0;
            long sumBasicRewardAmount = 0;
            long sumReElectionRewardAmount = 0;
            long sumVoteWeightRewardAmount = 0;
            var miners = Consensus.CallViewMethod<MinerList>(ConsensusMethod.GetCurrentMinerList, new Empty()).Pubkeys
                .Select(p => Address.FromPublicKey(p.ToByteArray())).ToList();
            var term = Consensus.GetCurrentTermInformation();
            if (Reward.Keys.Contains(term.TermNumber))
                return;

            foreach (var miner in miners)
            {
                var minerBasicReward = Profit.GetProfitsMap(miner.ToBase58(), MinerBasicReward);
                long profitAmount = 0;
                if (!minerBasicReward.Equals(new ReceivedProfitsMap()))
                {
                    profitAmount = minerBasicReward.Value["ELF"];
                    Logger.Info($"MinerBasicReward amount: user {miner} profit amount is {profitAmount}");
                }

                sumBasicRewardAmount += profitAmount;
                amount += profitAmount;
                long reElectionRewardAmount = 0;
                var reElectionReward = Profit.GetProfitsMap(miner.ToBase58(), ReElectionReward);
                if (!reElectionReward.Equals(new ReceivedProfitsMap()))
                {
                    reElectionRewardAmount = reElectionReward.Value["ELF"];
                    Logger.Info($"ReElectionReward amount: user {miner} profit amount is {reElectionRewardAmount}");
                }

                sumReElectionRewardAmount += reElectionRewardAmount;
                amount += reElectionRewardAmount;
                long votesWeightRewardAmount = 0;
                var votesWeightReward = Profit.GetProfitsMap(miner.ToBase58(), VotesWeightReward);
                if (!votesWeightReward.Equals(new ReceivedProfitsMap()))
                {
                    votesWeightRewardAmount = votesWeightReward.Value["ELF"];
                    Logger.Info($"VotesWeightReward amount: user {miner} profit amount is {votesWeightRewardAmount}");
                }

                sumVoteWeightRewardAmount += votesWeightRewardAmount;
                amount += votesWeightRewardAmount;
            }
            Reward.Add(term.TermNumber, amount);
            Logger.Info(
                $"{term.TermNumber} {amount} MinerBasicReward (10%):{sumBasicRewardAmount}; " +
                $"ReElectionReward(5%):{sumReElectionRewardAmount}; VotesWeightReward(5%):{sumVoteWeightRewardAmount}");
        }

        private void GetCurrentMiningRewardPerBlock()
        {
            MiningRewardPerBlock = Consensus.GetCurrentMiningRewardPerBlock().Value;
            Logger.Info($"Pre block : {MiningRewardPerBlock}");
        }
    }
}