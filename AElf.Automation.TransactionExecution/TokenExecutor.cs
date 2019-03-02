using System.Security.Cryptography.X509Certificates;
using AElf.Automation.Common.Contracts;
using AElf.Automation.Common.Helpers;

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
            TokenAddress = tokenAddress;
            Token = new TokenContract(cliHelper, callOwner, tokenAddress);
            InitToken(1000_000_000UL);
        }

        public TokenExecutor(CliHelper cliHelper, string callOwner)
        {
            CH = cliHelper;
            Token = new TokenContract(cliHelper, callOwner);
            TokenAddress = Token.ContractAbi;
            InitToken(1000_000_000UL);
        }

        public void InitToken(ulong amount)
        {
            Token.CallContractMethod(TokenMethod.Initialize, "ELF", "elf token", amount.ToString(), "0");
        }
    }
}