using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AElf.Contracts.MultiToken;
using AElf.Types;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using Shouldly;

namespace AElf.Automation.AccountCheck
{
    public class TransferAction : BasicAction
    {
        public void Transfer(Dictionary<TokenContract, string> tokenInfo)
        {
            var amount = TransferAmount;

            foreach (var (contract,symbol) in tokenInfo)
            {
                for (int i = 0; i < FromAccountList.ToList().Count; i++)
                {
                    contract.SetAccount(FromAccountList.ToList()[i]);
                    contract.ExecuteMethodWithTxId(TokenMethod.Transfer, new TransferInput
                    {
                        To = ToAccountList.ToList()[i].ConvertAddress(),
                        Amount = amount,
                        Symbol = symbol,
                        Memo = $"T-{Guid.NewGuid()}"
                    });
                }
                contract.CheckTransactionResultList();
            }
            Thread.Sleep(1000);
        }

        public void PrepareTransfer(Dictionary<TokenContract, string> tokenInfo)
        {
            var amount = TransferAmount;
            var times = CheckTimes;
            foreach (var (contract, symbol) in tokenInfo)
            {
                foreach (var account in FromAccountList)
                {
                    if (account == InitAccount) continue;
                    contract.ExecuteMethodWithTxId(TokenMethod.Transfer, new TransferInput
                        {
                            To = account.ConvertAddress(),
                            Amount = amount * times,
                            Symbol = symbol,
                            Memo = $"T-{Guid.NewGuid()}"
                        });
                }
                contract.CheckTransactionResultList();
            }
        }
        
        public List<TokenContract> DeployContractWithAuthority()
        {
            var list = new List<TokenContract>();
            while (list.Count != ContractCount)
            {
                var contractAddress =
                    AuthorityManager.DeployContract(InitAccount, "AElf.Contracts.MultiToken", Password);
                if (contractAddress.Equals(null))
                    continue;
                var tokenContract = new TokenContract(NodeManager,InitAccount,contractAddress.ToBase58());
                list.Add(tokenContract);
            }
            return list;
        }

        public Dictionary<TokenContract,string> CreateAndIssueToken(IEnumerable<TokenContract> contracts)
        {
            var tokenList = new Dictionary<TokenContract,string>();
            foreach (var contract in contracts)
            {
                var symbol = GenerateNotExistTokenSymbol(contract);

                var transaction = contract.ExecuteMethodWithResult(TokenMethod.Create, new CreateInput
                    {
                        Symbol = symbol,
                        TokenName = $"elf token {symbol}",
                        TotalSupply = 10_0000_0000_00000000L,
                        Decimals = 8,
                        Issuer = InitAccount.ConvertAddress(),
                        IsBurnable = true
                    });
                    transaction.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

                    var issueToken = contract.IssueBalance(InitAccount, InitAccount, 10_0000_0000_00000000,symbol);
                    issueToken.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
                    var balance = contract.GetUserBalance(InitAccount, symbol);
                    balance.ShouldBe(10_0000_0000_00000000);
                    
                    tokenList.Add(contract,symbol);
            }

            return tokenList;
        }
        
        private string GenerateNotExistTokenSymbol(TokenContract token)
        {
            while (true)
            {
                var symbol = CommonHelper.RandomString(8, false);
                var tokenInfo = token.GetTokenInfo(symbol);
                if (tokenInfo.Equals(new TokenInfo())) return symbol;
            }
        }
    }
}