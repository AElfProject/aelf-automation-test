using System.Collections.Generic;
using System.Linq;
using AElf.Automation.SideChainEconomicTest.EconomicTest;
using AElfChain.Common.Contracts;
using AElfChain.Common.Managers;

namespace AElf.Automation.SideChainEconomicTest
{
    public class MainChainTests : TestBase
    {
        public void GetTokenInfo()
        {
            Logger.Info("Query main chain token info");

            Main.GetTokenInfos();
            Main.GetTokenBalances(Main.CallAddress);
        }

        public void PrepareSideChainToken(CrossChainManager manager, ContractServices services)
        {
            Logger.Info($"Transfer resource token to side chain {services.NodeManager.GetChainId()}: ");
            TransferToSideChain(manager, services, 20000_00000000);
        }
        
        public void TransferSideChainToken(CrossChainManager manager, ContractServices services, List<string> symbols)
        {
            Logger.Info($"Transfer resource token to side chain {services.NodeManager.GetChainId()}: ");
            TransferToSideChain(manager, services, 20000_00000000,symbols);
        }
        
        public void TransferPrimaryToken(CrossChainManager manager)
        {
            if (SideA.GetPrimaryToken(SideA.CallAddress))
                return;
            Logger.Info($"Transfer primary token to side chain {SideA.NodeManager.GetChainId()}: ");
            var primaryToken = SideA.TokenService.GetPrimaryTokenSymbol();
            var symbols = new List<string>{primaryToken};
            TransferToSideChain(manager, SideA, 20000_00000000,symbols);
        }
    }
}