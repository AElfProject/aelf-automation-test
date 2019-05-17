using AElf.Automation.Common.Contracts;
using AElf.Automation.Common.Helpers;
using AElf.Contracts.MultiToken.Messages;

namespace AElf.Automation.ScenariosExecution
{
    public class TokenExecutor
    {
        private readonly IApiHelper _apiHelper;
        private readonly string TokenAddress;
        private string CallOwner;

        public TokenContract Token;

        public TokenExecutor(IApiHelper apiHelper, string callOwner, string tokenAddress)
        {
            _apiHelper = apiHelper;
            CallOwner = callOwner;
            TokenAddress = tokenAddress;
            Token = new TokenContract(apiHelper, callOwner, tokenAddress);
            InitToken(1000_000_000L);
        }

        public TokenExecutor(IApiHelper apiHelper, string callOwner)
        {
            _apiHelper = apiHelper;
            CallOwner = callOwner;
            Token = new TokenContract(apiHelper, callOwner);
            TokenAddress = Token.ContractAddress;
            InitToken(1000_000_000L);
        }

        public void InitToken(long amount)
        {
            Token.ExecuteMethodWithResult(TokenMethod.Create, new CreateInput
            {
                Symbol = "ELF",
                TokenName = "elf token",
                TotalSupply = amount,
                Issuer = Address.Parse(CallOwner),
                Decimals = 2,
            });

            Token.ExecuteMethodWithResult(TokenMethod.Issue, new IssueInput
            {
                Symbol = "ELF",
                Amount = amount,
                To = Address.Parse(CallOwner),
                Memo = "Test issue money to owner."
            });
        }
    }
}