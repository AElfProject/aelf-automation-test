using AElf.Automation.Common.Contracts;
using AElf.Automation.Common.Helpers;
using AElf.Automation.Common.Managers;

namespace AElf.Automation.EconomicSystem.Tests
{
    public partial class Behaviors
    {
        public readonly INodeManager NodeManager;
        public readonly ContractServices ContractServices;

        public readonly ElectionContract ElectionService;
        public readonly VoteContract VoteService;
        public readonly ProfitContract ProfitService;
        public readonly TokenContract TokenService;
        public readonly TokenConverterContract TokenConverterService;
        public readonly FeeReceiverContract FeeReceiverService;
        public readonly ConsensusContract ConsensusService;

        public Behaviors(ContractServices contractServices)
        {
            NodeManager = contractServices.NodeManager;
            ContractServices = contractServices;

            ElectionService = ContractServices.ElectionService;
            VoteService = ContractServices.VoteService;
            ProfitService = ContractServices.ProfitService;
            TokenService = ContractServices.TokenService;
            ConsensusService = ContractServices.ConsensusService;
        }

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
    }
}