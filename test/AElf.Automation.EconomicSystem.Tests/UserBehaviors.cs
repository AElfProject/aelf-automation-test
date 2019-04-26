using AElf.Automation.Common.Helpers;

namespace AElf.Automation.EconomicSystem.Tests
{
    public class UserBehaviors
    {
        public readonly RpcApiHelper ApiHelper;
        public readonly ContractServices ContractServices;
        
        public UserBehaviors(ContractServices contractServices)
        {
            ApiHelper = contractServices.ApiHelper;
            ContractServices = contractServices;
        }
        
        //action
        
        //view
    }
}