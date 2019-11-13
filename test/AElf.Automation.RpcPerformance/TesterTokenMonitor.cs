using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AElf.Contracts.MultiToken;
using AElfChain.Common;
using AElfChain.Common.Contracts;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using AElfChain.Common.Utils;
using Google.Protobuf.WellKnownTypes;
using log4net;

namespace AElf.Automation.RpcPerformance
{
    public class TesterTokenMonitor
    {
        private static readonly ILog Logger = Log4NetHelper.GetLogger();

        public TesterTokenMonitor(INodeManager nodeManager)
        {
            var genesis = GenesisContract.GetGenesisContract(nodeManager);
            SystemToken = genesis.GetTokenContract();
        }

        public TokenContract SystemToken { get; set; }

        public void ExecuteTokenCheckTask(List<string> testers)
        {
            while (true)
            {
                Thread.Sleep(5 * 60 * 1000);
                try
                {
                    Logger.Info("Start check tester token balance job.");
                    TransferTokenToTester(testers);
                }
                catch (Exception e)
                {
                    Logger.Error(e);
                    break;
                }
            }
        }

        public void TransferTokenForTest(List<string> testers)
        {
            Logger.Info("Prepare main chain basic token for tester.");
            TransferTokenToTester(testers);
        }

        public void IssueTokenForTest(List<string> testers)
        {
            Logger.Info("Prepare side chain basic token for tester.");
            IssueTokenForSideChain(testers);
        }

        private void TransferTokenToTester(List<string> testers)
        {
            var bps = NodeInfoHelper.Config.Nodes;
            foreach (var bp in bps)
            {
                var balance = SystemToken.GetUserBalance(bp.Account);
                if (balance < 200_0000_00000000) continue;
                SystemToken.SetAccount(bp.Account, bp.Password);
                foreach (var tester in testers)
                {
                    if (tester == bp.Account) continue;
                    var userBalance = SystemToken.GetUserBalance(tester, NodeOption.ChainToken);
                    if (userBalance < 1_0000_00000000)
                        SystemToken.ExecuteMethodWithTxId(TokenMethod.Transfer, new TransferInput
                        {
                            To = tester.ConvertAddress(),
                            Amount = 1_0000_00000000,
                            Symbol = NodeOption.ChainToken,
                            Memo = $"Transfer token for test {Guid.NewGuid()}"
                        });
                }

                SystemToken.CheckTransactionResultList();
            }
        }

        private void IssueTokenForSideChain(List<string> testers)
        {
            var bps = NodeInfoHelper.Config.Nodes;
            //issue all token to first bp
            var firstBp = bps.First();
            SystemToken.SetAccount(firstBp.Account, firstBp.Password);
            var primaryToken = SystemToken.CallViewMethod<StringValue>(TokenMethod.GetPrimaryTokenSymbol, new Empty());
            var tokenInfo = SystemToken.CallViewMethod<TokenInfo>(TokenMethod.GetTokenInfo, new GetTokenInfoInput
            {
                Symbol = primaryToken.Value
            });
            var issueBalance = tokenInfo.TotalSupply - tokenInfo.Supply - tokenInfo.Burned;
            if (issueBalance >= 1000_00000000)
            {
                SystemToken.ExecuteMethodWithResult(TokenMethod.Issue, new IssueInput
                {
                    To = SystemToken.CallAccount,
                    Amount = issueBalance,
                    Symbol = primaryToken.Value,
                    Memo = $"Issue all token to bp {Guid.NewGuid()}"
                });
            }
            //transfer to tester
            foreach (var bp in bps)
            {
                var balance = SystemToken.GetUserBalance(bp.Account, NodeOption.ChainToken);
                if (balance < 200_0000_00000000) continue;
                SystemToken.SetAccount(bp.Account, bp.Password);
                foreach (var tester in testers)
                {
                    if (tester == bp.Account) continue;
                    var userBalance = SystemToken.GetUserBalance(tester, NodeOption.ChainToken);
                    if (userBalance < 1_0000_00000000)
                        SystemToken.ExecuteMethodWithTxId(TokenMethod.Transfer, new TransferInput
                        {
                            To = tester.ConvertAddress(),
                            Amount = 1_0000_00000000,
                            Symbol = NodeOption.ChainToken,
                            Memo = $"Issue token for test {Guid.NewGuid()}"
                        });
                }

                SystemToken.CheckTransactionResultList();
            }
        }
    }
}