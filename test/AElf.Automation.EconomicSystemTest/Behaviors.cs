using AElfChain.Common.Contracts;
using AElfChain.Common.Managers;

namespace AElf.Automation.EconomicSystemTest
{
    public partial class Behaviors
    {
        public enum ProfitType
        {
            Treasury,
            MinerReward,
            BackSubsidy,
            CitizenWelfare,
            BasicMinerReward,
            VotesWeightReward,
            ReElectionReward
        }

        public readonly ConsensusContract ConsensusService;
        public readonly ContractManager ContractManager;

        public readonly ElectionContract ElectionService;
        public readonly INodeManager NodeManager;
        public readonly ProfitContract ProfitService;
        public readonly TokenConverterContract TokenConverterService;
        public readonly TokenContract TokenService;
        public readonly TreasuryContract Treasury;
        public readonly VoteContract VoteService;

        public Behaviors(ContractManager contractManager)
        {
            NodeManager = contractManager.NodeManager;
            ContractManager = contractManager;

            ElectionService = ContractManager.Election;
            VoteService = ContractManager.Vote;
            ProfitService = ContractManager.Profit;
            TokenService = ContractManager.Token;
            Treasury = ContractManager.Treasury;
            ConsensusService = ContractManager.Consensus;
        }
    }
}