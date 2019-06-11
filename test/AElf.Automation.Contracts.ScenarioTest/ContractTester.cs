using AElf.Automation.Common.Contracts;
using AElf.Automation.Common.Helpers;

namespace AElf.Automation.Contracts.ScenarioTest
{
    public class ContractTester
    {
        public readonly IApiHelper ApiHelper;
        public readonly ContractServices ContractServices;
        
        public readonly ElectionContract ElectionService;
        public readonly VoteContract VoteService;
        public readonly ProfitContract ProfitService;
        public readonly TokenContract TokenService;
        public readonly TokenConverterContract TokenConverterService;
        public readonly FeeReceiverContract FeeReceiverService;
        public readonly ConsensusContract ConsensusService;
        public readonly AssociationAuthContract AssociationService;

        public ContractTester(ContractServices contractServices)
        {
            ApiHelper = contractServices.ApiHelper;
            ContractServices = contractServices;

            ElectionService = ContractServices.ElectionService;
            VoteService = ContractServices.VoteService;
            ProfitService = ContractServices.ProfitService;
            TokenService = ContractServices.TokenService;
            ConsensusService = ContractServices.ConsensusService;
            AssociationService = ContractServices.AssociationAuthService;

        }
    }
}