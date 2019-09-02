using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Acs0;
using AElf.Automation.Common.Contracts;
using AElf.Automation.Common.Helpers;
using AElf.Contracts.MultiToken;
using AElf.Contracts.TokenConverter;
using AElf.Types;
using AElfChain.AccountService;
using AElfChain.SDK;
using AElfChain.SDK.Models;
using Google.Protobuf;
using log4net;
using Newtonsoft.Json;
using Shouldly;
using Volo.Abp.Threading;

namespace AElf.Automation.SideChainTests.EconomicTest
{
    public class MainChainManager
    {
        public ContractServices MainChain { get; set; }

        public GenesisContract Genesis => MainChain.GenesisService;

        public TokenContract Token => MainChain.TokenService;

        public IApiHelper ApiHelper => MainChain.ApiHelper;

        public IApiService ApiService => MainChain.ApiHelper.ApiService;

        public static readonly ILog Logger = Log4NetHelper.GetLogger();
        
        public List<string> Symbols = new List<string>{"CPU", "NET", "STO"};


        public MainChainManager(string serviceUrl, string account)
        {
            var chainId = ChainHelper.ConvertBase58ToChainId("AELF");
            MainChain = new ContractServices(serviceUrl, account, AccountOption.DefaultPassword, chainId);
        }

        public async Task GetTokenInfos()
        {
            var tester = Token.GetTestStub<TokenContractContainer.TokenContractStub>(MainChain.CallAddress);
            foreach (var symbol in Symbols.Concat(new List<string>{"ELF"}))
            {
                var result = await tester.GetTokenInfo.CallAsync(new GetTokenInfoInput
                {
                    Symbol = symbol
                });
                Logger.Info($"Token '{symbol}' info: {result}");
            }
        }

        public async Task BuyResources(string account, long amount)
        {
            var tokenConverter = Genesis.GetContractAddressByName(NameProvider.TokenConverterName);
            var tokenContract = new TokenConverterContract(ApiHelper, account, tokenConverter.GetFormatted());
            var converter =
                tokenContract.GetTestStub<TokenConverterContractContainer.TokenConverterContractStub>(
                    account);

            foreach (var symbol in Symbols)
            {
                var beforeBalance = Token.GetUserBalance(account, symbol);
                //buy
                var transactionResult = await converter.Buy.SendAsync(new BuyInput
                {
                    Symbol = symbol,
                    Amount = amount * 10000_0000
                });
                transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

                var afterBalance = Token.GetUserBalance(account, symbol);
                Logger.Info($"Token '{symbol}' balance: before = {beforeBalance}, after: {afterBalance}");
            }
        }

        public void TransferResourceToken(ContractServices services, string acs8Contract)
        {
            foreach (var symbol in Symbols.Concat(new List<string>{"ELF"}))
            {
                var ownerBalance = services.TokenService.GetUserBalance(services.CallAddress, symbol);
                services.TokenService.TransferBalance(services.CallAddress, acs8Contract, ownerBalance / 2, symbol);
            }
        }
        
        public void GetContractTokenInfo(string contract)
        {
            foreach (var symbol in Symbols.Concat(new List<string>{"ELF"}))
            {
                var balance = Token.GetUserBalance(contract, symbol);
                Logger.Info($"Contract balance {symbol}={balance}");
            }
        }
    }
}