using System.Collections.Generic;
using AElf.Contracts.Profit;
using AElfChain.Common;
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
        public readonly AuthorityManager AuthorityManager;

        public readonly ElectionContract ElectionService;
        public readonly INodeManager NodeManager;
        public readonly ProfitContract ProfitService;
        public readonly TokenContract TokenService;
        public readonly TreasuryContract Treasury;
        public readonly VoteContract VoteService;
        public static Dictionary<SchemeType, Scheme> Schemes { get; set; }
        public Behaviors(ContractManager contractManager,string account)
        {
            NodeInfoHelper.SetConfig("nodes-env2-main");
            NodeManager = contractManager.NodeManager;
            AuthorityManager = new AuthorityManager(NodeManager,account);
            ContractManager = contractManager;

            ElectionService = ContractManager.Election;
            VoteService = ContractManager.Vote;
            ProfitService = ContractManager.Profit;
            TokenService = ContractManager.Token;
            Treasury = ContractManager.Treasury;
            ConsensusService = ContractManager.Consensus;
            ProfitService.GetTreasurySchemes(Treasury.ContractAddress);
            Schemes = ProfitContract.Schemes;
        }
    }
}