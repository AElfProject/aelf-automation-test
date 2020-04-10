using System;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.Types;
using AElfChain.Common;
using AElfChain.Common.Contracts;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using log4net;
using Shouldly;

namespace AElf.Automation.ContractsTesting
{
    public class ContractIssueLimit
    {
        private static readonly ILog Logger = Log4NetHelper.GetLogger();
        private readonly string _account;
        private readonly string _bpAccount;

        private readonly GenesisContract _genesisContract;
        private readonly INodeManager _nodeManager;
        private readonly ContractTesterFactory _stub;
        private string _contractAddress;

        public ContractIssueLimit(string serviceUrl)
        {
            _nodeManager = new NodeManager(serviceUrl);
            _stub = new ContractTesterFactory(_nodeManager);

            _account = _nodeManager.NewAccount();
            _genesisContract = GenesisContract.GetGenesisContract(_nodeManager, _account);

            _bpAccount = "eu6nm4Kxu3HcA7FhSdQpPjy29x896yqcPHSq55gKaggTKEwA3";
        }

        public async Task PrepareUserToken()
        {
            var systemToken = _genesisContract.GetContractAddressByName(NameProvider.Token);
            var systemStub = _stub.Create<TokenContractContainer.TokenContractStub>(
                systemToken, _bpAccount);
            var transferBalance = await systemStub.Transfer.SendAsync(new TransferInput
            {
                Amount = 1000,
                Symbol = NodeOption.NativeTokenSymbol,
                To = AddressHelper.Base58StringToAddress(_account),
                Memo = $"T-{Guid.NewGuid()}"
            });
            transferBalance.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            await GetUserBalance(systemStub, NodeOption.NativeTokenSymbol, _account);
        }

        public void DeployTestContract()
        {
            var tokenContract = new TokenContract(_nodeManager, _account);
            _contractAddress = tokenContract.ContractAddress;
        }

        public async Task ExecuteMethodTest()
        {
            const string symbol = "CELF";

            var tokenStub = _stub.Create<TokenContractContainer.TokenContractStub>(
                AddressHelper.Base58StringToAddress(_contractAddress), _account);

            //create
            var createResult = await tokenStub.Create.SendAsync(new CreateInput
            {
                Symbol = symbol,
                TokenName = $"elf token {symbol}",
                TotalSupply = 8000_000,
                Decimals = 2,
                Issuer = AddressHelper.Base58StringToAddress(_account),
                IsBurnable = true
            });
            createResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            //issue
            var issueResult = await tokenStub.Issue.SendAsync(new IssueInput
            {
                Symbol = symbol,
                Amount = 8000_000,
                To = AddressHelper.Base58StringToAddress(_account),
                Memo = $"I-{Guid.NewGuid()}"
            });
            issueResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var totalBurn = 0;
            await GetUserBalance(tokenStub, symbol, _account);
            for (var i = 1; i < 10; i++)
            {
                Logger.Info($"Test times: {i}");
                //get token info
                await GetTokenInfo(tokenStub, symbol);

                //burn
                totalBurn += 1000 * i;
                var burnResult = await tokenStub.Burn.SendAsync(new BurnInput
                {
                    Symbol = symbol,
                    Amount = 1000 * i
                });
                burnResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

                await GetTokenInfo(tokenStub, symbol);
            }

            await GetUserBalance(tokenStub, symbol, _account);

            Logger.Info($"Issue another token: {totalBurn}");
            issueResult = await tokenStub.Issue.SendAsync(new IssueInput
            {
                Symbol = symbol,
                Amount = totalBurn - 1000,
                To = AddressHelper.Base58StringToAddress(_account),
                Memo = $"I-{Guid.NewGuid()}"
            });
            issueResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            await GetTokenInfo(tokenStub, symbol);
            await GetUserBalance(tokenStub, symbol, _account);
        }

        private async Task GetTokenInfo(TokenContractContainer.TokenContractStub tester, string symbol)
        {
            var tokenInfo = await tester.GetTokenInfo.CallAsync(new GetTokenInfoInput
            {
                Symbol = symbol
            });
            Logger.Info($"Token info: Total supply:{tokenInfo.TotalSupply}, Supply: {tokenInfo.Supply}");
        }

        private async Task GetUserBalance(TokenContractContainer.TokenContractStub tester, string symbol,
            string account)
        {
            var balanceInfo = await tester.GetBalance.CallAsync(new GetBalanceInput
            {
                Symbol = symbol,
                Owner = AddressHelper.Base58StringToAddress(account)
            });
            Logger.Info($"Balance info: {balanceInfo.Balance}");
        }
    }
}