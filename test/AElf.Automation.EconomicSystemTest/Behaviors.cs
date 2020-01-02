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
        public readonly ContractServices ContractServices;

        public readonly ElectionContract ElectionService;
        public readonly INodeManager NodeManager;
        public readonly ProfitContract ProfitService;
        public readonly TokenConverterContract TokenConverterService;
        public readonly TokenContract TokenService;
        public readonly TreasuryContract Treasury;
        public readonly VoteContract VoteService;

        public Behaviors(ContractServices contractServices)
        {
            NodeManager = contractServices.NodeManager;
            ContractServices = contractServices;

            ElectionService = ContractServices.ElectionService;
            VoteService = ContractServices.VoteService;
            ProfitService = ContractServices.ProfitService;
            TokenService = ContractServices.TokenService;
            Treasury = ContractServices.TreasuryService;
            ConsensusService = ContractServices.ConsensusService;
        }
    }
}