using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Acs7;
using AElf.Automation.Common.Contracts;
using AElf.Automation.Common.Helpers;
using AElf.Contracts.MultiToken;
using AElf.CSharp.Core.Utils;
using AElf.Types;
using AElfChain.SDK;
using AElfChain.SDK.Models;
using log4net;
using ApiMethods = AElf.Automation.Common.Helpers.ApiMethods;

namespace AElf.Automation.SideChainEconomicTest.EconomicTest
{
    public class ContractServices
    {
        public readonly IApiHelper ApiHelper;
        public IApiService ApiService => ApiHelper.ApiService;
        public GenesisContract GenesisService { get; set; }
        public TokenContract TokenService { get; set; }
        public ConsensusContract ConsensusService { get; set; }
        public CrossChainContract CrossChainService { get; set; }
        public ParliamentAuthContract ParliamentService { get; set; }

        public string CallAddress { get; }
        
        public Address CallAccount => AddressHelper.Base58StringToAddress(CallAddress);

        public static ILog Logger = Log4NetHelper.GetLogger();
        
        public readonly List<string> Symbols = new List<string> { "ELF", "CPU", "RAM", "NET", "STO" };

        public ContractServices(string url, string callAddress, string password)
        {
            ApiHelper = new WebApiHelper(url);
            CallAddress = callAddress;
            UnlockAccounts(CallAddress, password);

            //get services
            GetContractServices();
        }

        public void GetTokenInfos()
        {
            Logger.Info($"Query token infos: ");
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
            foreach (var symbol in Symbols)
            {
                TokenService.TransferBalance(from, to, amount, symbol);
            }
        }

        public async Task<MerklePath> GetMerklePath(string transactionId)
        {
            var result = await ApiService.GetMerklePathByTransactionIdAsync(transactionId);
            
            return new MerklePath
            {
                MerklePathNodes = { result.MerklePathNodes.Select(o => new MerklePathNode
                {
                    Hash = HashHelper.HexStringToHash(o.Hash),
                    IsLeftChildNode = o.IsLeftChildNode
                })}
            };
        }
        
        private void GetContractServices()
        {
            Logger.Info($"Get contract service from: {ApiService.GetServiceUrl()}");
            
            GenesisService = GenesisContract.GetGenesisContract(ApiHelper, CallAddress);

            //TokenService contract
            TokenService = GenesisService.GetTokenContract();

            //Consensus contract
            ConsensusService = GenesisService.GetConsensusContract();

            //CrossChain contract
            CrossChainService = GenesisService.GetCrossChainContract();

            //ParliamentAuth contract
            ParliamentService = GenesisService.GetParliamentAuthContract();
        }
        
        private void UnlockAccounts(string account, string password)
        {
            var ci = new CommandInfo(ApiMethods.AccountUnlock)
            {
                Parameter = $"{account} {password} notimeout"
            };
            
            ApiHelper.UnlockAccount(ci);
        }
    }
}