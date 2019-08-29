using System.Threading.Tasks;
using AElf.Automation.Common.Helpers;
using AElf.Contracts.MultiToken;
using AElf.Types;
using AElfChain.AccountService;
using AElfChain.ContractService;
using AElfChain.TestBase;
using Microsoft.Extensions.Logging;
using Volo.Abp.Threading;

namespace AElfChain.Console
{
    public class TokenIssue
    {
        private Address _tokenAddress;
        private AccountInfo _accountInfo;
        private ISystemContract _systemContract;
        private IAccountManager _accountManager;
        private IAuthorityManager _authorityManager;
        private TokenContractContainer.TokenContractStub _tokenContractStub;

        private const string Symbol = "TELF";

        public ILogger Logger { get; set; }

        public TokenIssue()
        {
            Logger = ServiceStore.LoggerFactory.CreateLogger<TokenIssue>();

            _systemContract = ServiceStore.Provider.GetService<ISystemContract>();
            _authorityManager = ServiceStore.Provider.GetService<IAuthorityManager>();
            _accountManager = ServiceStore.AccountManager;

            _accountInfo = AsyncHelper.RunSync(_accountManager.GetRandomAccountInfoAsync);
        }

        public async Task PrepareSomeToken(string bpAccount)
        {
            var accountInfo = await _accountManager.GetAccountInfoAsync(bpAccount);
            var systemTokenAddress = await _systemContract.GetSystemContractAddressAsync(SystemContracts.MultiToken);
            var tokenStub =
                _systemContract.GetTestStub<TokenContractContainer.TokenContractStub>(systemTokenAddress, accountInfo);

            var transactionResult = await tokenStub.Transfer.SendAsync(new TransferInput
            {
                Symbol = "ELF",
                Amount = 1000,
                To = _accountInfo.Account,
                Memo = "Prepare token for test"
            });
            Logger.LogInformation($"{transactionResult}");

            var balance = await tokenStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = _accountInfo.Account,
                Symbol = "ELF"
            });
            
            Logger.LogInformation($"Account: {_accountInfo.Formatted}, balance: {balance.Balance}");
        }

        public async Task DeployTokenContract()
        {
            _accountInfo = await _accountManager.GetRandomAccountInfoAsync();
            _tokenAddress =
                await _authorityManager.DeployContractWithAuthority(_accountInfo, "AElf.Contracts.MultiToken.dll");
            Logger.LogInformation($"Deployed contract address: {_tokenAddress}");
        }

        public async Task GetTokenStub()
        {
            _tokenContractStub =
                _systemContract.GetTestStub<TokenContractContainer.TokenContractStub>(_tokenAddress, _accountInfo);
        }

        public async Task ExecuteTokenTest()
        {
            var createOutput = await _tokenContractStub.Create.SendAsync(new CreateInput
            {
                Symbol = Symbol,
                TokenName = $"elf token {Symbol}",
                TotalSupply = 8000_000,
                Decimals = 2,
                Issuer = _accountInfo.Account,
                IsBurnable = true
            });

            var issueOutput = await _tokenContractStub.Issue.SendAsync(new IssueInput
            {
                Symbol = Symbol,
                Amount = 2000_000,
                To = _accountInfo.Account,
                Memo = "Initialize custom token"
            });

            var tokenInfo = await _tokenContractStub.GetTokenInfo.CallAsync(new GetTokenInfoInput
            {
                Symbol = Symbol
            });
            Logger.LogInformation($"Token info: {tokenInfo}");

            var burnOutput = await _tokenContractStub.Burn.SendAsync(new BurnInput
            {
                Symbol = Symbol,
                Amount = 1000_000
            });
            
            tokenInfo = await _tokenContractStub.GetTokenInfo.CallAsync(new GetTokenInfoInput
            {
                Symbol = Symbol
            });
            Logger.LogInformation($"After token burned, token info: {tokenInfo}");
            
            issueOutput = await _tokenContractStub.Issue.SendAsync(new IssueInput
            {
                Symbol = Symbol,
                Amount = 2000_000,
                To = _accountInfo.Account,
                Memo = "Initialize custom token"
            });
            
            tokenInfo = await _tokenContractStub.GetTokenInfo.CallAsync(new GetTokenInfoInput
            {
                Symbol = Symbol
            });
            Logger.LogInformation($"After token burned, token info: {tokenInfo}");
        }
    }
}