using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Standards.ACS0;
using AElf.Client.Consensus.AEDPoS;
using AElf.Client.Service;
using AElf.Contracts.MultiToken;
using AElf.Contracts.TokenConverter;
using AElf.Types;
using AElfChain.Common;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using log4net;
using Shouldly;

namespace AElf.Automation.SideChainEconomicTest.EconomicTest
{
    public class MainChainManager
    {
        public static ILog Logger = Log4NetHelper.GetLogger();
        public readonly List<string> Symbols;

        public MainChainManager(string serviceUrl, string account)
        {
            MainChain = new ContractServices(serviceUrl, account, NodeOption.DefaultPassword);
            Symbols = MainChain.Symbols;
        }

        public ContractServices MainChain { get; set; }

        public GenesisContract Genesis => MainChain.GenesisService;

        public TokenContract Token => MainChain.TokenService;

        public INodeManager NodeManager => MainChain.NodeManager;

        public AElfClient ApiClient => MainChain.NodeManager.ApiClient;

        public void BuyResources(string account, long amount,List<string> symbols = null)
        {
            if (symbols == null)
                symbols = Symbols;
            var tokenConverter = Genesis.GetTokenConverterContract();
            foreach (var symbol in symbols)
            {
                var beforeBalance = Token.GetUserBalance(account, symbol);
                if (beforeBalance >= amount * 10000_0000)
                {
                    Logger.Info($"Token '{symbol}' balance = {beforeBalance}");
                    continue;
                }

                //buy
                var transactionResult = tokenConverter.Buy( account,symbol, amount * 10000_0000);
                transactionResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

                var afterBalance = Token.GetUserBalance(account, symbol);
                Logger.Info($"Token '{symbol}' balance: before = {beforeBalance}, after ={afterBalance}");
            }
        }
    }
}