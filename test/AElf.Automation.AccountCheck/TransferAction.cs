using System;
using System.Collections.Generic;
using AElf.Contracts.MultiToken;
using AElf.Types;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using log4net;
using Shouldly;

namespace AElf.Automation.AccountCheck
{
    public class TransferAction : BasicAction
    {
        public TransferAction()
        {
            GetService();
            GetConfig();
            SystemToken = ContractManager.Token;
        }

        public void Transfer(Dictionary<TokenContract, string> tokenInfo)
        {
            var amount = TransferAmount;

            foreach (var (contract,symbol) in tokenInfo)
            {
                for (var i = 0; i < FromAccountList.Count; i++)
                {
                    contract.SetAccount(FromAccountList[i]);
                    contract.ExecuteMethodWithTxId(TokenMethod.Transfer, new TransferInput
                    {
                        To = ToAccountList[i].ConvertAddress(),
                        Amount = amount,
                        Symbol = symbol,
                        Memo = $"T-{Guid.NewGuid()}"
                    });
                }
                contract.CheckTransactionResultList();
            }
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
            if (ContractCount == 0)
            {
                list.Add(SystemToken);
                return list;
            }
            while (list.Count != ContractCount)
            {
                var contractAddress =
                    AuthorityManager.DeployContractWithAuthority(InitAccount, "AElf.Contracts.MultiToken", Password);
                if (contractAddress.Equals(null))
                    continue;
                var tokenContract = new TokenContract(NodeManager,InitAccount,contractAddress.ToBase58());
                list.Add(tokenContract);
            }

            if (IsAddSystemContract)
                list.Add(SystemToken);

            return list;
        }

        public Dictionary<TokenContract,string> CreateAndIssueToken(IEnumerable<TokenContract> contracts)
        {
            var systemToken = ContractManager.Token;
            var primaryToken = systemToken.GetPrimaryTokenSymbol();
            var tokenList = new Dictionary<TokenContract,string>();
            foreach (var contract in contracts)
            {
                var symbol = GenerateNotExistTokenSymbol(contract);
                if (!contract.ContractAddress.Equals(systemToken.ContractAddress))
                {
                    contract.ExecuteMethodWithResult(TokenMethod.Create, new CreateInput
                    {
                        Symbol = primaryToken,
                        TokenName = $"fake {primaryToken}",
                        TotalSupply = 10_0000_0000_00000000L,
                        Decimals = 8,
                        Issuer = InitAccount.ConvertAddress(),
                        IsBurnable = true
                    });
                    
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
                else
                    tokenList.Add(systemToken,primaryToken);

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

        public TokenContract SystemToken { get; }
        private static readonly ILog Logger = Log4NetHelper.GetLogger();
    }
}