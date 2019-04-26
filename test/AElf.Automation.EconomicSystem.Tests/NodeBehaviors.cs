using AElf.Automation.Common.Contracts;
using AElf.Automation.Common.Helpers;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Automation.EconomicSystem.Tests
{
    public class NodeBehaviors
    {
        public readonly RpcApiHelper ApiHelper;
        public readonly ContractServices ContractServices;

        public readonly ElectionContract Election;
        
        public NodeBehaviors(ContractServices contractServices)
        {
            ApiHelper = contractServices.ApiHelper;
            ContractServices = contractServices;

            Election = ContractServices.ElectionService;
        }
        
        //action
        public CommandInfo AnnouncementElection(string account)
        {
            Election.SetAccount(account);

            return Election.ExecuteMethodWithResult(ElectionMethod.AnnounceElection, new Empty());
        }
        
        //view
        public PublicKeysList GetVictories()
        {
            return Election.CallViewMethod<PublicKeysList>(ElectionMethod.Withdraw, new Empty());
        }
    }
}