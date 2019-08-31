using System;
using System.Collections.Generic;
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

        public MainChainManager(string serviceUrl, string account)
        {
            var chainId = ChainHelper.ConvertBase58ToChainId("AELF");
            MainChain = new ContractServices(serviceUrl, account, AccountOption.DefaultPassword, chainId);
        }

        public async Task ValidateMainChainTokenInfo()
        {
            var tester = Genesis.GetBasicContractTester();
            var result = await tester.ValidateSystemContractAddress.SendAsync(new ValidateSystemContractAddressInput
            {
                Address = AddressHelper.Base58StringToAddress(Token.ContractAddress),
                SystemContractHashName = Hash.FromString("AElf.ContractNames.Token")
            });
            if (result.TransactionResult.Status == TransactionResultStatus.Failed)
                throw new Exception($"Validate chain {MainChain.ChainId} token contract failed");
            var validationRawTx = result.Transaction.ToByteArray().ToHex();
            Logger.Info($"Validate main chain token address {Token.ContractAddress}");
        }

        public async Task GetTokenInfos(List<string> symbols)
        {
            var tester = Token.GetTestStub<TokenContractContainer.TokenContractStub>(MainChain.CallAddress);
            foreach (var symbol in symbols)
            {
                var result = await tester.GetTokenInfo.CallAsync(new GetTokenInfoInput
                {
                    Symbol = symbol
                });
                Logger.Info($"Token '{symbol}' info: {result}");
            };
        }

        public async Task BuyResource(string account, string symbol, long amount)
        {
            var tokenConverter = Genesis.GetContractAddressByName(NameProvider.TokenConverterName);
            var tokenContract = new TokenConverterContract(ApiHelper, account, tokenConverter.GetFormatted());
            var converter =
                tokenContract.GetTestStub<TokenConverterContractContainer.TokenConverterContractStub>(
                    account);

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
}