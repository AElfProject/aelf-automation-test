using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AElf.Automation.SideChainEconomicTest.EconomicTest;
using AElf.Client.Consensus.AEDPoS;
using AElf.Client.Dto;
using AElf.Types;
using AElfChain.Common.Contracts;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using Google.Protobuf.WellKnownTypes;
using log4net;

namespace AElf.Automation.SideChainEconomicTest
{
    public class TestBase
    {
        protected DateTime UpdateEndpointTime = DateTime.Now;

        public AccountManager AccountManager = new AccountManager(AElfKeyStore.GetKeyStore());
        public ILog Logger = Log4NetHelper.GetLogger();
        public MainChainManager MainManager;
        public SideChainManager SideManager;
        public CrossChainManager MainToSideA;
        public CrossChainManager MainToSideB;
        public readonly ConfigInfo ConfigInfo;


        public TestBase()
        {
            ConfigInfo = ConfigInfoHelper.Config;
            var mainInfo = ConfigInfo.MainChainInfos;
            var creator = mainInfo.Creator;
            MainManager = new MainChainManager(mainInfo.MainChainUrl, creator);
            SideManager = InitializeSideChainManager();
            MainToSideA = new CrossChainManager(Main.NodeManager, SideA.NodeManager, creator);
            MainToSideB = new CrossChainManager(Main.NodeManager, SideB.NodeManager, creator);
        }

        public ContractServices Main => MainManager.MainChain;
        public ContractServices SideA => SideManager.SideChains[ConfigInfo.SideChainInfos.First().Id];
        public ContractServices SideB => SideManager.SideChains[ConfigInfo.SideChainInfos.Last().Id];
        public string ContractA => SideManager.Contracts[ConfigInfo.SideChainInfos.First().Id];
        public string ContractB => SideManager.Contracts[ConfigInfo.SideChainInfos.Last().Id];

        private SideChainManager InitializeSideChainManager()
        {
            var sideChainManager = new SideChainManager();
            var sideInfos = ConfigInfo.SideChainInfos;
            foreach (var side in sideInfos)
                sideChainManager.InitializeSideChain(side.SideChainUrl, side.Creator, side.Id, side.Contract);

            return sideChainManager;
        }

        protected void TransferToSideChain(CrossChainManager manager, ContractServices services, long amount,
            string account = "")
        {
            if (account == "")
                account = Main.CallAddress;
            Main.GetTokenBalances(account);
            var transactionResults = new Dictionary<string, TransactionResultDto>();
            //main chain transfer
            foreach (var symbol in Main.FeeResourceSymbols)
            {
                var result = manager.CrossChainTransfer(symbol, amount, account, account,
                    out string rawTx);
                transactionResults.Add(rawTx, result);
            }

            if (!manager.CheckPrivilegePreserved())
            {
                var elfResult = manager.CrossChainTransfer("ELF", amount, account, account,
                    out string rawTx);
                transactionResults.Add(rawTx, elfResult);
            }
            else
            {
                foreach (var symbol in Main.RentResourceSymbols)
                {
                    var result = manager.CrossChainTransfer(symbol, amount, account, account,
                        out string rawTx);
                    transactionResults.Add(rawTx, result);
                }
            }

            Main.GetTokenBalances(Main.CallAddress);
            //wait index
            var lastHeight = transactionResults.Last().Value.BlockNumber;
            manager.CheckSideChainIndexMainChain(lastHeight);

            //side chain accept
            services.GetTokenBalances(account);
            var token = services.TokenService;
            var txLists = new List<string>();
            foreach (var (rawTx, transactionResult) in transactionResults)
            {
                var receiveInput = manager.ReceiveFromMainChainInput(transactionResult.BlockNumber,
                    transactionResult.TransactionId, rawTx);
                token.SetAccount(account);
                var result = token.ExecuteMethodWithTxId(
                    TokenMethod.CrossChainReceiveToken,
                    receiveInput);
                txLists.Add(result);
            }

            services.NodeManager.CheckTransactionListResult(txLists);
            services.GetTokenBalances(account);
        }

        protected void TransferToSideChain(CrossChainManager manager, ContractServices services, long amount,
            List<string> symbols, string account = "")
        {
            if (account == "")
                account = Main.CallAddress;
            Main.GetTokenBalances(account);
            var transactionResults = new Dictionary<string, TransactionResultDto>();
            //main chain transfer
            foreach (var symbol in symbols)
            {
                var result = manager.CrossChainTransfer(symbol, amount, account, account,
                    out string rawTx);
                transactionResults.Add(rawTx, result);
            }

            Main.GetTokenBalances(Main.CallAddress);
            //wait index
            var lastHeight = transactionResults.Last().Value.BlockNumber;
            manager.CheckSideChainIndexMainChain(lastHeight);

            //side chain accept
            services.GetTokenBalances(account);
            var token = services.TokenService;
            var txLists = new List<string>();
            foreach (var (rawTx, transactionResult) in transactionResults)
            {
                var receiveInput = manager.ReceiveFromMainChainInput(transactionResult.BlockNumber,
                    transactionResult.TransactionId, rawTx);
                token.SetAccount(account);
                var result = token.ExecuteMethodWithTxId(
                    TokenMethod.CrossChainReceiveToken,
                    receiveInput);
                txLists.Add(result);
            }

            services.NodeManager.CheckTransactionListResult(txLists);
            services.GetTokenBalances(account);
        }

        public List<string> GetMiners()
        {
            var minerList = new List<string>();
            var miners =
                Main.ConsensusService.CallViewMethod<MinerList>(ConsensusMethod.GetCurrentMinerList, new Empty());
            foreach (var minersPubkey in miners.Pubkeys)
            {
                var miner = Address.FromPublicKey(minersPubkey.ToByteArray());
                minerList.Add(miner.ToBase58());
            }

            return minerList;
        }

        protected void ExecuteStandaloneTask(IEnumerable<Action> actions, int sleepSeconds = 0,
            bool interrupted = false)
        {
            foreach (var action in actions)
                try
                {
                    action.Invoke();
                }
                catch (Exception e)
                {
                    Logger.Error($"Execute action {action.Method.Name} got exception: {e.Message}", e);
                    if (interrupted)
                        break;
                }

            if (sleepSeconds != 0)
                Thread.Sleep(1000 * sleepSeconds);
        }
    }
}