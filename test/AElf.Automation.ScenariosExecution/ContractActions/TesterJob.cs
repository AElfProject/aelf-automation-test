using System;
using System.Collections.Generic;
using System.Linq;
using AElf.Contracts.MultiToken;
using AElfChain.Common;
using AElfChain.Common.Contracts;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using AElfChain.Common.Utils;
using log4net;

namespace AElf.Automation.ScenariosExecution.ContractActions
{
    public class TesterJob
    {
        public List<string> Testers { get; set; }
        public INodeManager NodeManager { get; set; }
        
        private static readonly ILog Logger = Log4NetHelper.GetLogger();

        public TesterJob(INodeManager nodeManager)
        {
            Testers = new List<string>();
            NodeManager = nodeManager;
        }
        
        public void CheckTesterToken()
        {
            Logger.Info("Prepare chain basic token for tester.");
            var bps = NodeInfoHelper.Config.Nodes;
            var genesis = NodeManager.GetGenesisContract(bps.First().Account);
            var token = genesis.GetTokenContract();
            var symbol = CheckTokenAndIssueBalance(token);
            foreach (var bp in bps)
            {
                var balance = token.GetUserBalance(bp.Account, symbol);
                if (balance < 200_0000_00000000) continue;
                token.SetAccount(bp.Account, bp.Password);
                foreach (var tester in Testers)
                {
                    if (tester == bp.Account) continue;
                    var userBalance = token.GetUserBalance(tester, symbol);
                    if (userBalance < 1_000_00000000)
                        token.ExecuteMethodWithTxId(TokenMethod.Transfer, new TransferInput
                        {
                            To = tester.ConvertAddress(),
                            Amount = 1_0000_00000000,
                            Symbol = symbol,
                            Memo = $"Transfer token for test {Guid.NewGuid()}"
                        });
                }

                token.CheckTransactionResultList();
            }
        }
        
        private string CheckTokenAndIssueBalance(TokenContract token)
        {
            var bps = NodeInfoHelper.Config.Nodes;
            var firstBp = bps.First();
            token.SetAccount(firstBp.Account, firstBp.Password);
            var primaryToken = token.GetPrimaryTokenSymbol();
            if (primaryToken != NodeOption.NativeTokenSymbol)
            {
                var tokenInfo = token.GetTokenInfo(primaryToken);
                var issueBalance = tokenInfo.TotalSupply - tokenInfo.Supply - tokenInfo.Burned;
                if (issueBalance >= 1000_00000000)
                {
                    var account = token.CallAddress;
                    token.IssueBalance(account, account, issueBalance,
                        primaryToken);
                }
            }

            return primaryToken;
        }

        public string GetRandomAccount()
        {
            var id = CommonHelper.GenerateRandomNumber(0, Testers.Count);
            return Testers[id];
        }

        public (string, string) GetRandomAccountPair()
        {
            var id1 = CommonHelper.GenerateRandomNumber(0, Testers.Count);
            while (true)
            {
                var id2 = CommonHelper.GenerateRandomNumber(0, Testers.Count);
                if (id1 == id2)
                {
                    $"Same id: {id1}".WriteWarningLine();
                    continue;
                }

                return (Testers[id1], Testers[id2]);
            }
        }

        public void GetTestAccounts(int count)
        {
            var accounts = NodeManager.ListAccounts();
            if (accounts.Count >= count)
            {
                foreach (var acc in accounts.Take(count)) Testers.Add(acc);
            }
            else
            {
                foreach (var acc in accounts) Testers.Add(acc);

                var generateCount = count - accounts.Count;
                for (var i = 0; i < generateCount; i++)
                {
                    var account = NodeManager.NewAccount();
                    Testers.Add(account);
                }
            }
        }
    }
}