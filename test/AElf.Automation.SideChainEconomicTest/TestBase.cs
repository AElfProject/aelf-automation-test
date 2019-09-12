using System.IO;
using AElf.Automation.Common.Helpers;
using AElf.Automation.Common.Managers;
using AElf.Automation.SideChainEconomicTest.EconomicTest;
using log4net;

namespace AElf.Automation.SideChainEconomicTest
{
    public class TestBase
    {
        public ILog Logger = Log4NetHelper.GetLogger();

        public TestBase()
        {
            MainManager = new MainChainManager(ChainConstInfo.MainChainUrl, ChainConstInfo.ChainAccount);

            SideManager = InitializeSideChainManager();
        }
        
        public MainChainManager MainManager;
        public ContractServices Main => MainManager.MainChain;
        
        public SideChainManager SideManager;
        public ContractServices SideA => SideManager.SideChains[ChainConstInfo.SideChainIdA];
        public ContractServices SideB => SideManager.SideChains[ChainConstInfo.SideChainIdB];
        
        public AccountManager AccountManager = new AccountManager(AElfKeyStore.GetKeyStore());

        private SideChainManager InitializeSideChainManager()
        {
            var chainManager = new SideChainManager();
            chainManager.InitializeSideChain(ChainConstInfo.SideChainUrlA, ChainConstInfo.ChainAccount,
                ChainConstInfo.SideChainIdA);
            chainManager.InitializeSideChain(ChainConstInfo.SideChainUrlB, ChainConstInfo.ChainAccount,
                ChainConstInfo.SideChainIdB);

            return chainManager;
        }
    }
}