using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AElf.Automation.Common;
using AElf.Automation.Common.Contracts;
using AElf.Automation.Common.Helpers;
using AElf.Automation.Common.Managers;
using AElf.Automation.Common.Utils;
using AElf.Contracts.MultiToken;
using log4net;

namespace AElf.Automation.RpcPerformance
{
    public class TesterTokenMonitor
    {
        private static readonly ILog Logger = Log4NetHelper.GetLogger();
        public TokenContract SystemToken { get; set; }
        
        public TesterTokenMonitor(INodeManager nodeManager)
        {
            var genesis = GenesisContract.GetGenesisContract(nodeManager);
            SystemToken = genesis.GetTokenContract();
        }

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
            Logger.Info("Prepare basic token for tester.");
            TransferTokenToTester(testers);
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
                    {
                        SystemToken.ExecuteMethodWithTxId(TokenMethod.Transfer, new TransferInput
                        {
                            To = tester.ConvertAddress(),
                            Amount = 1_0000_00000000,
                            Symbol = NodeOption.ChainToken,
                            Memo = $"Transfer token for test {Guid.NewGuid()}"
                        });
                    }
                }
                SystemToken.CheckTransactionResultList();
            }
        }
    }
}