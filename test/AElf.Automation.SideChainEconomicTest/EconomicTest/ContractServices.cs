using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Threading.Tasks;
using Acs10;
using AElf.Client.Dto;
using AElf.Client.Service;
using AElf.Contracts.MultiToken;
using AElf.Types;
using AElfChain.Common;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using log4net;
using Shouldly;

namespace AElf.Automation.SideChainEconomicTest.EconomicTest
{
    public class ContractServices
    {
        public static ILog Logger = Log4NetHelper.GetLogger();
        public readonly INodeManager NodeManager;
        public List<string> Symbols;
        public readonly List<string> FeeResourceSymbols;
        public readonly List<string> RentResourceSymbols;

        public ContractServices(string url, string callAddress, string password)
        {
            NodeManager = new NodeManager(url);
            CallAddress = callAddress;
            
            NodeManager.UnlockAccount(CallAddress, password);
            GetContractServices();
            FeeResourceSymbols = new List<string>
                {"READ", "WRITE", "STORAGE", "TRAFFIC"};
            RentResourceSymbols = new List<string>
                {"CPU", "NET", "DISK", "RAM"};
            Symbols = FeeResourceSymbols.Union(RentResourceSymbols).ToList();
        }

        public AElfClient ApiClient => NodeManager.ApiClient;
        public GenesisContract GenesisService { get; set; }
        public TokenContract TokenService { get; set; }
        public ConsensusContract ConsensusService { get; set; }
        public CrossChainContract CrossChainService { get; set; }
        public ParliamentContract ParliamentService { get; set; }
        public string CallAddress { get; }

        public Address CallAccount => CallAddress.ConvertAddress();

        public void GetTokenInfos()
        {
            Logger.Info("Query token infos: ");
            foreach (var symbol in Symbols)
            {
                var tokenInfo =
                    TokenService.CallViewMethod<TokenInfo>(TokenMethod.GetTokenInfo, new GetTokenInfoInput
                    {
                        Symbol = symbol
                    });
                Logger.Info($"Token info: {symbol}={tokenInfo}");
            }
        }

        public List<string> GetTokenBalances(string account,long amount = 0,List<string> symbols = null)
        {
            Logger.Info($"On chain {NodeManager.GetChainId()} Query account balance: {account}");
            var needCrossTransferToken = new List<string>();
            if (symbols == null)
                symbols = Symbols;
            if (amount == 0)
                amount = 10_00000000;
            foreach (var symbol in symbols)
            {
                var balance = TokenService.GetUserBalance(account, symbol);
                Logger.Info($"Token balance: {symbol}={balance}");
                if (balance < amount)
                    needCrossTransferToken.Add(symbol);
            }
            
            return needCrossTransferToken;
        }

        public bool GetPrimaryToken(string account)
        {
            var isEnough = true;
            var primaryToken = TokenService.GetPrimaryTokenSymbol();
            var primaryTokenBalance = TokenService.GetUserBalance(account, primaryToken);
            Logger.Info($"Token balance: {primaryToken}={primaryTokenBalance}");
            if (primaryTokenBalance < 100_0000000)
                isEnough = false;
            return isEnough;
        }

        public void TransferResources(string from, string to, long amount, List<string> symbols = null)
        {
            Logger.Info($"Transfer token from={from} to={to}, amount={amount}");
            if (symbols == null)
                symbols = Symbols;
            foreach (var symbol in symbols) TokenService.TransferBalance(from, to, amount, symbol);
        }
        
        public void TransferPrimaryToken(string from, string to, long amount)
        {
            Logger.Info($"Transfer token from={from} to={to}");
            var symbol = TokenService.GetPrimaryTokenSymbol(); 
            TokenService.TransferBalance(from, to, amount, symbol);
        }
        
        public void DonateSideChainDividendsPool(string symbol, long amount)
        {
            var init = CallAddress;
            var approveResult = TokenService.ApproveToken(CallAddress,ConsensusService.ContractAddress,amount,symbol);
            approveResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var allowance = TokenService.GetAllowance(init, ConsensusService.ContractAddress, symbol);
            Logger.Info($"Account: {init}, allowance: {symbol}={allowance}");

            var contributeResult = ConsensusService.ExecuteMethodWithResult(ConsensusMethod.Donate, new DonateInput
            {
                Symbol = symbol,
                Amount = amount
            });
                contributeResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var check = ConsensusService.GetSymbolList();
            var unAmount = ConsensusService.GetUndistributedDividends(); 
            Logger.Info($"Symbol list : {check}\n amount:{unAmount}");
        }

        private void GetContractServices()
        {
            Logger.Info($"Get contract service from: {ApiClient.BaseUrl}");

            GenesisService = GenesisContract.GetGenesisContract(NodeManager, CallAddress);

            //Token contract
            TokenService = GenesisService.GetTokenContract();

            //Consensus contract
            ConsensusService = GenesisService.GetConsensusContract();

            //CrossChain contract
            CrossChainService = GenesisService.GetCrossChainContract();

            //Parliament contract
            ParliamentService = GenesisService.GetParliamentContract();
        }
    }
}