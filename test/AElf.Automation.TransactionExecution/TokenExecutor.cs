using System.Security.Cryptography.X509Certificates;
using AElf.Automation.Common.Contracts;
using AElf.Automation.Common.Helpers;
using AElf.Common;
using AElf.Contracts.MultiToken.Messages;

namespace AElf.Automation.TransactionExecution
{
    public class TokenExecutor
    {
        private readonly CliHelper CH;
        private readonly string TokenAddress;
        private string CallOwner;

        public TokenContract Token;

        public TokenExecutor(CliHelper cliHelper, string callOwner, string tokenAddress)
        {
            CH = cliHelper;
            CallOwner = callOwner;
            TokenAddress = tokenAddress;
            Token = new TokenContract(cliHelper, callOwner, tokenAddress);
            InitToken(1000_000_000L);
        }

        public TokenExecutor(CliHelper cliHelper, string callOwner)
        {
            CH = cliHelper;
            CallOwner = callOwner;
            Token = new TokenContract(cliHelper, callOwner);
            TokenAddress = Token.ContractAbi;
            InitToken(1000_000_000L);
        }

        public void InitToken(long amount)
        {
            Token.CallMethodWithResult(TokenMethod.Create, new CreateInput
            {
                Symbol = "ELF",
                TokenName = "elf token",
                TotalSupply = amount,
                Issuer = Address.Parse(CallOwner),
                Decimals = 2,
            });

            Token.CallMethodWithResult(TokenMethod.Issue, new IssueInput
            {
                Symbol = "ELF",
                Amount = amount,
                To = Address.Parse(CallOwner),
                Memo = "Test issue money to owner."
            });
        }
    }
}