using AElf.Automation.SideChainEconomicTest.EconomicTest;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using log4net;

namespace AElf.Automation.SideChainEconomicTest
{
    public class TestBase
    {
        public AccountManager AccountManager = new AccountManager(AElfKeyStore.GetKeyStore());
        public ILog Logger = Log4NetHelper.GetLogger();

        public MainChainManager MainManager;

        public SideChainManager SideManager;

        public TestBase()
        {
            MainManager = new MainChainManager(ChainConstInfo.MainChainUrl, ChainConstInfo.ChainAccount);

            SideManager = InitializeSideChainManager();
        }

        public ContractServices Main => MainManager.MainChain;
        public ContractServices SideA => SideManager.SideChains[ChainConstInfo.SideChainIdA];
        public ContractServices SideB => SideManager.SideChains[ChainConstInfo.SideChainIdB];

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