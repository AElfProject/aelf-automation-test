using AElf.Automation.Common.Contracts;
using AElf.Automation.Common.Helpers;
using AElf.Contracts.MultiToken.Messages;
using AElf.Contracts.TokenConverter;
using AElf.Kernel;

namespace AElf.Automation.EconomicSystem.Tests
{
    public partial class Behaviors
    {
        //action
        public CommandInfo UserVote(string account,string candidate, int lockTime, long amount)
        {
            ElectionService.SetAccount(account);
            var vote = ElectionService.ExecuteMethodWithResult(ElectionMethod.Vote, new VoteMinerInput
            {
                CandidatePublicKey = ApiHelper.GetPublicKeyFromAddress(candidate),
                LockTime = lockTime,
                LockTimeUnit = TimeUnit.Days,
                Amount = amount,
            });

            return vote;
        }

        public CommandInfo ReleaseProfit(long period,int amount,string profitId)
        {
            var result =
                ProfitService.ExecuteMethodWithResult(ProfitMethod.ReleaseProfit, new ReleaseProfitInput
                {
                    Period = period,
                    Amount = amount,
                    ProfitId = Hash.LoadHex(profitId)
                });
            return result;
        }

        public CommandInfo Profit(string account,Hash profitId)
        {
            ProfitService.SetAccount(account);
            var result = ProfitService.ExecuteMethodWithResult(ProfitMethod.Profit, new ProfitInput
            {
                ProfitId = profitId
            });
            
            return result;
        }
        
        #region TokenConverter Method

        
        // action
        public CommandInfo TokenConverterInitialize(string initAccount)
        {
            var ramConnector = new Connector
            {
                Symbol = "RAM",
                IsPurchaseEnabled = true,
                IsVirtualBalanceEnabled = false,
                VirtualBalance = 0,
                Weight = "0.5"
            };
            var cpuConnector = new Connector
            {
                Symbol = "CPU",
                IsPurchaseEnabled = true,
                IsVirtualBalanceEnabled = false,
                VirtualBalance = 0,
                Weight = "0.5"
            };
            var netConnector = new Connector
            {
                Symbol = "NET",
                IsPurchaseEnabled = true,
                IsVirtualBalanceEnabled = false,
                VirtualBalance = 0,
                Weight = "0.5"
            };
            var stoConnector = new Connector
            {
                Symbol = "STO",
                IsPurchaseEnabled = true,
                IsVirtualBalanceEnabled = false,
                VirtualBalance = 0,
                Weight = "0.5"
            };
            var elfConnector = new Connector
            {
                Symbol = "ELF",
                IsPurchaseEnabled = true,
                IsVirtualBalanceEnabled = true,
                VirtualBalance = 100_0000,
                Weight = "0.5"
            };
            
            var result = TokenConverterService.ExecuteMethodWithResult(TokenConverterMethod.Initialize, new InitializeInput
            {
                BaseTokenSymbol = "ELF",
                ManagerAddress = Address.Parse(initAccount),
                FeeReceiverAddress = Address.Parse(FeeReceiverService.ContractAddress),
                FeeRate = "0.05",
                TokenContractAddress = Address.Parse(TokenService.ContractAddress),
                Connectors = {ramConnector,cpuConnector,netConnector,stoConnector,elfConnector}
            });

            return result;
        }
        
        //token action
        public CommandInfo TransferToken(string from, string to, long amount, string symbol = "ELF")
        {
            TokenService.SetAccount(from);
            
            return TokenService.ExecuteMethodWithResult(TokenMethod.Transfer, new TransferInput
            {
                Symbol = symbol,
                Amount = amount,
                To = Address.Parse(to),
                Memo = $"transfer {from}=>{to} with amount {amount}."
            });
        }

        public CommandInfo CreateToken(string issuer,string symbol,string tokenName)
        {
            TokenService.SetAccount(issuer);
            var create = TokenService.ExecuteMethodWithResult(TokenMethod.Create, new CreateInput
            {
                Symbol = symbol,
                Decimals = 2,
                IsBurnable = true,
                Issuer = Address.Parse(issuer),
                TokenName = tokenName,
                TotalSupply = 100_0000
            });
            return create;
        }

        public CommandInfo IssueToken(string issuer, string symbol, string toAddress)
        {
            TokenService.SetAccount(issuer);
            var issue = TokenService.ExecuteMethodWithResult(TokenMethod.Issue, new IssueInput
            {
                Symbol = symbol,
                Amount = 100_0000,
                Memo = "Issue",
                To = Address.Parse(toAddress)
            });

            return issue;
        }

        #endregion
    }
}