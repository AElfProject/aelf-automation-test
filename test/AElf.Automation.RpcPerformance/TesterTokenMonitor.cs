using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AElf.Contracts.MultiToken;
using AElf.Types;
using AElfChain.Common;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using log4net;
using Shouldly;

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

        public void ExecuteTokenCheckTask(List<string> testers, CancellationToken ct)
        {
            var checkRound = 1;
            while (true)
            {
                if (ct.IsCancellationRequested)
                {
                    Logger.Warn("ExecuteTokenCheckTask was been cancelled.");
                    break;
                }

                Thread.Sleep(3 * 60 * 1000);
                try
                {
                    Logger.Info($"Start check tester token balance job round: {checkRound++}");
                    TransferTokenForTest(testers);
                }
                catch (Exception e)
                {
                    Logger.Error(e.Message);
                }
            }
        }

        public void TransferTokenForTest(List<string> testers)
        {
            Logger.Info("Prepare chain basic token for tester.");
            var bps = NodeInfoHelper.Config.Nodes;
            var symbol = CheckTokenAndIssueBalance();
            foreach (var bp in bps)
            {
                var balance = SystemToken.GetUserBalance(bp.Account, symbol);
                if (balance < 2_0000_00000000) continue;
                SystemToken.SetAccount(bp.Account, bp.Password);
                foreach (var tester in testers)
                {
                    if (tester == bp.Account) continue;
                    var userBalance = SystemToken.GetUserBalance(tester, symbol);
                    if (userBalance < 1000_00000000)
                        SystemToken.ExecuteMethodWithTxId(TokenMethod.Transfer, new TransferInput
                        {
                            To = tester.ConvertAddress(),
                            Amount = 5000_00000000,
                            Symbol = symbol,
                            Memo = $"T-{Guid.NewGuid()}"
                        });
                }

                SystemToken.CheckTransactionResultList();
                break;
            }
        }

        public static string GenerateNotExistTokenSymbol(INodeManager nodeManager)
        {
            while (true)
            {
                var symbol = CommonHelper.RandomString(8, false);
                var tokenInfo = nodeManager.GetTokenInfo(symbol);
                if (tokenInfo.Equals(new TokenInfo())) return symbol;
            }
        }

        private string CheckTokenAndIssueBalance()
        {
            var bps = NodeInfoHelper.Config.Nodes;
            //issue all token to first bp
            var firstBp = bps.First();
            SystemToken.SetAccount(firstBp.Account, firstBp.Password);
            var primaryToken = SystemToken.GetPrimaryTokenSymbol();
            if (primaryToken != NodeOption.NativeTokenSymbol)
            {
                var tokenInfo = SystemToken.GetTokenInfo(primaryToken);
                var issueBalance = tokenInfo.TotalSupply - tokenInfo.Issued;
                if (issueBalance >= 1000_00000000)
                {
                    var account = SystemToken.CallAddress;
                    SystemToken.IssueBalance(account, account, issueBalance,
                        primaryToken);
                }
            }

            return primaryToken;
        }
    }
}