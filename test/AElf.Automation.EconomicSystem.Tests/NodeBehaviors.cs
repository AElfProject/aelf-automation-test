using AElf.Automation.Common.Contracts;
using AElf.Automation.Common.Helpers;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Automation.EconomicSystem.Tests
{
    public class NodeBehaviors
    {
        public readonly RpcApiHelper ApiHelper;
        public readonly ContractServices ContractServices;

        public readonly ElectionContract ElectionService;
        public readonly VoteContract VoteService;
        public readonly ProfitContract ProfitService;
        public readonly TokenContract TokenService;
        public readonly ConsensusContract ConsensusService;
        
        public NodeBehaviors(ContractServices contractServices)
        {
            ApiHelper = contractServices.ApiHelper;
            ContractServices = contractServices;

            ElectionService = ContractServices.ElectionService;
            VoteService = ContractServices.VoteService;
            ProfitService = ContractServices.ProfitService;
            TokenService = ContractServices.TokenService;
            ConsensusService = ContractServices.ConsensusService;
        }
        
        //action
        public CommandInfo AnnouncementElection(string account)
        {
            ElectionService.SetAccount(account);
            return ElectionService.ExecuteMethodWithResult(ElectionMethod.AnnounceElection, new Empty());
        }

        public CommandInfo QuitElection(string account)
        {
            ElectionService.SetAccount(account);
            var result = ElectionService.ExecuteMethodWithResult(ElectionMethod.QuitElection, new Empty());
            return result;
        }
        
        //view
    }
}