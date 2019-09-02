using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Automation.Common.OptionManagers.Authority;
using AElf.Contracts.MultiToken;
using AElf.Types;
using AElfChain.AccountService;
using log4net;
using Shouldly;
using NodeInfoHelper = AElf.Automation.Common.OptionManagers.Authority.NodeInfoHelper;

namespace AElf.Automation.SideChainTests.EconomicTest
{
    public class SideChainManager
    {
        private Dictionary<int, ContractServices> SideChains { get; set; }
        private List<string> Symbols = new List<string>{"CPU", "NET", "STO"};

        public ILog Logger { get; set; }

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

        public void TransferResourceToken(ContractServices services, string acs8Contract)
        {
            foreach (var symbol in Symbols)
            {
                var ownerBalance = services.TokenService.GetUserBalance(services.CallAddress, symbol);
                services.TokenService.TransferBalance(services.CallAddress, acs8Contract, ownerBalance / 2);
            }
        }
        
        public void GetContractTokenInfo(ContractServices services, string contract)
        {
            foreach (var symbol in Symbols)
            {
                var balance = services.TokenService.GetUserBalance(contract, symbol);
                Logger.Info($"Contract balance {symbol}={balance}");
            }
        }

        public void SetResourceUnitPrice(ContractServices services)
        {
            var authority = new AuthorityManager(services.ApiHelper, services.CallAddress);
            var ownerAddress = services.ParliamentService.GetGenesisOwnerAddress();
            //set resource token price
            var contract = services.TokenService.ContractAddress;
            const string method = "SetResourceTokenUnitPrice";
            var input = new SetResourceTokenUnitPriceInput
            {
                CpuUnitPrice = 100,
                NetUnitPrice = 100,
                StoUnitPrice = 100
            };
            var miners = NodeInfoHelper.Config.Nodes.Select(o => o.Account).ToList();
            var transactionResult = authority.ExecuteTransactionWithAuthority(contract, method, input, ownerAddress, miners, services.CallAddress);
            transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }
    }
}