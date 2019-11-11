using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Automation.Common;
using AElf.Automation.Common.Contracts;
using AElf.Automation.Common.Helpers;
using AElf.Automation.Common.Managers;
using AElf.Automation.Common.Utils;
using AElf.Contracts.MultiToken;
using AElf.Types;
using Shouldly;

namespace AElf.Automation.RpcPerformance
{
    public class TransactionInitialize
    {
        private readonly INodeManager _nodeManager;
        private readonly GenesisContract _genesis;
        private List<AccountInfo> _accounts;
        
        public TransactionInitialize(INodeManager nodeManager)
        {
            _nodeManager = nodeManager;
            _genesis = GenesisContract.GetGenesisContract(nodeManager);
            _accounts = new List<AccountInfo>();
        }

        public List<AccountInfo> GetOrGenerateTestUsers(int count)
        {
            var testUsers = new List<AccountInfo>();
            
            var accounts = _nodeManager.ListAccounts();
            if (accounts.Count >= count)
            {
                testUsers.AddRange(accounts.Take(count).Select(acc => new AccountInfo(acc)));
            }
            else
            {
                testUsers.AddRange(accounts.Select(acc => new AccountInfo(acc)));

                var generateCount = count - accounts.Count;
                for (var i = 0; i < generateCount; i++)
                {
                    var account = _nodeManager.NewAccount();
                    testUsers.Add(new AccountInfo(account));
                }
            }

            _accounts = testUsers;
            return testUsers;
        }
        
        public void TokenPreparation(IEnumerable<AccountInfo> testUsers)
        {
            var bpInfos = NodeInfoHelper.Config.Nodes;
            var tokenAddress = _genesis.GetContractAddressByName(NameProvider.Token);
            if(tokenAddress == new Address())
                throw new Exception("Token was not deployed.");
            
            var bpTester = new TokenContract(_nodeManager, bpInfos.First().Account, tokenAddress.GetFormatted());
            var tokenSymbol = NodeOption.IsMainChain ? NodeOption.NativeTokenSymbol : NodeOption.ChainToken;
            
            foreach (var user in testUsers)
            {
                var balance = bpTester.GetUserBalance(user.Account, tokenSymbol);
                if(balance != 0) continue;
                
                bpTester.ExecuteMethodWithTxId(TokenMethod.Transfer, new TransferInput
                {
                    Symbol = tokenSymbol,
                    Amount = 100_00000000,
                    To = user.Account.ConvertAddress(),
                    Memo = "Prepare token for user testing"
                });
            }
            
            bpTester.CheckTransactionResultList();
        }

        public List<ContractInfo> DeployTestContracts(int groups)
        {
            var contracts = new List<ContractInfo>();
            
            for (var i = 0; i < groups; i++)
            {
                string account;
                var authority = new AuthorityManager(_nodeManager);
                if (!NodeOption.IsMainChain)
                {
                    var miners = authority.GetCurrentMiners();
                    if (i > miners.Count) continue;
                    account = miners[i];
                }
                else
                {
                    account = _accounts[i].Account;
                }
                
                var contractAddress = authority.DeployContractWithAuthority(account, "AElf.Contracts.MultiToken.dll");
                contracts.Add(new ContractInfo(account, contractAddress.GetFormatted()));
            }

            return contracts;
        }

        public async Task InitializeTestContracts(IEnumerable<ContractInfo> contracts)
        {
            foreach (var contract in contracts)
            {
                var account = contract.Owner;
                var contractPath = contract.ContractAddress;
                var symbol = $"ELF{CommonHelper.RandomString(4, false)}";
                contract.Symbol = symbol;
                
                //create
                var tokenTester = new TokenContract(_nodeManager, account, contractPath);
                var tokenStub = tokenTester.GetTestStub<TokenContractContainer.TokenContractStub>(account);
                var createResult = await tokenStub.Create.SendAsync(new CreateInput
                {
                    Symbol = symbol,
                    TokenName = $"elf token {symbol}",
                    TotalSupply = long.MaxValue,
                    Decimals = 2,
                    Issuer = account.ConvertAddress(),
                    IsBurnable = true
                });
                createResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
                
                //issue
                var issueResult = await tokenStub.Issue.SendAsync(new IssueInput
                {
                    Amount = long.MaxValue,
                    Memo = $"Issue all balance to owner - {Guid.NewGuid()}",
                    Symbol = symbol,
                    To = account.ConvertAddress()
                });
                issueResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            }
        }
    }
}