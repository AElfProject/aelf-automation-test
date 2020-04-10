using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Client.Service;
using AElf.Contracts.MultiToken;
using AElf.Types;
using AElfChain.Common.Contracts;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using log4net;

namespace AElf.Automation.SideChainEconomicTest.EconomicTest
{
    public class ContractServices
    {
        public static ILog Logger = Log4NetHelper.GetLogger();
        public readonly INodeManager NodeManager;

        public readonly List<string> Symbols = new List<string> {"ELF", "CPU", "RAM", "NET", "DISK"};

        public ContractServices(string url, string callAddress, string password)
        {
            NodeManager = new NodeManager(url);
            CallAddress = callAddress;

            NodeManager.UnlockAccount(CallAddress, password);
            GetContractServices();
        }

        public AElfClient ApiClient => NodeManager.ApiClient;
        public GenesisContract GenesisService { get; set; }
        public TokenContract TokenService { get; set; }
        public ConsensusContract ConsensusService { get; set; }
        public CrossChainContract CrossChainService { get; set; }
        public ParliamentAuthContract ParliamentService { get; set; }

        public string CallAddress { get; }

        public Address CallAccount => AddressHelper.Base58StringToAddress(CallAddress);

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
                Logger.Info($"Token balance: {symbol}={tokenInfo}");
            }
        }

        public void GetTokenBalances(string account)
        {
            Logger.Info($"Query account balance: {account}");
            foreach (var symbol in Symbols)
            {
                var balance = TokenService.GetUserBalance(account, symbol);
                Logger.Info($"Token balance: {symbol}={balance}");
            }
        }

        public void TransferResources(string from, string to, long amount)
        {
            Logger.Info($"Transfer token from={from} to={to}");
            foreach (var symbol in Symbols) TokenService.TransferBalance(from, to, amount, symbol);
        }

        public async Task<MerklePath> GetMerklePath(string transactionId)
        {
            var result = await ApiClient.GetMerklePathByTransactionIdAsync(transactionId);

            return new MerklePath
            {
                MerklePathNodes =
                {
                    result.MerklePathNodes.Select(o => new MerklePathNode
                    {
                        Hash = HashHelper.HexStringToHash(o.Hash),
                        IsLeftChildNode = o.IsLeftChildNode
                    })
                }
            };
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

            //ParliamentAuth contract
            ParliamentService = GenesisService.GetParliamentAuthContract();
        }
    }
}