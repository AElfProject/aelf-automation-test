using System.Collections.Generic;
using System.Linq;
using AElf.Contracts.TestContract.BasicFunctionWithParallel;
using AElfChain.Common;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using log4net;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AElf.Automation.Contracts.ScenarioTest
{
    [TestClass]
    public class ParallelExecutionTests
    {
        private static readonly ILog Logger = Log4NetHelper.GetLogger();
        public string TestContract = "uSXxaGWKDBPV6Z8EG8Et9sjaXhH1uMWEpVvmo2KzKEaueWzSe";

        public ParallelExecutionTests()
        {
            Log4NetHelper.LogInit();
            NodeInfoHelper.SetConfig("nodes-local");
            var node = NodeInfoHelper.Config.Nodes.First();
            NodeManager = new NodeManager(node.Endpoint);
            TestAccount = node.Account;
        }

        public INodeManager NodeManager { get; set; }
        public string TestAccount { get; set; }

        [TestMethod]
        public void TestParallelTestContract()
        {
            var contracts = new List<BasicWithParallelContract>();
            var accounts = NodeManager.ListAccounts();
            NodeInfoHelper.Config.Nodes.ForEach(o =>
            {
                contracts.Add(new BasicWithParallelContract(NodeManager, o.Account, TestContract));
            });
            for (var i = 0; i < 30; i++)
            {
                var contract = contracts[0];
                var txId = contract.ExecuteMethodWithTxId(
                    nameof(BasicFunctionWithParallelContractContainer.BasicFunctionWithParallelContractStub
                        .IncreaseWinMoney),
                    new IncreaseWinMoneyInput
                    {
                        First = accounts[CommonHelper.GenerateRandomNumber(0, accounts.Count - 1)].ConvertAddress(),
                        Second = accounts[CommonHelper.GenerateRandomNumber(0, accounts.Count - 1)].ConvertAddress()
                    });
                Logger.Info($"TxId: {txId}");

                txId = contract.ExecuteMethodWithTxId(
                    nameof(BasicFunctionWithParallelContractContainer.BasicFunctionWithParallelContractStub
                        .UpdateBetLimit),
                    new BetLimitInput
                    {
                        MinValue = CommonHelper.GenerateRandomNumber(1, 100),
                        MaxValue = CommonHelper.GenerateRandomNumber(5000, 6000)
                    });
                Logger.Info($"TxId: {txId}");
            }
        }
    }
}