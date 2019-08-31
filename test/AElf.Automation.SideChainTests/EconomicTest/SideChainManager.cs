using System.Collections.Generic;
using System.Threading.Tasks;
using AElfChain.AccountService;

namespace AElf.Automation.SideChainTests.EconomicTest
{
    public class SideChainManager
    {
        private Dictionary<int, ContractServices> SideChains { get; set; }

        public SideChainManager()
        {
            SideChains = new Dictionary<int, ContractServices>();
        }

        public ContractServices InitializeSideChain(string serviceUrl, string account, int chainId)
        {
            var contractServices = new ContractServices(serviceUrl, account, AccountOption.DefaultPassword, chainId);
            
            SideChains.Add(chainId, contractServices);

            return contractServices;
        }
    }
}