using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.Types;
using AElfChain.Common;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using log4net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace AElf.Automation.Contracts.ScenarioTest
{
    [TestClass]
    public class TokenContractDemo
    {
        public TokenContractDemo()
        {
            Log4NetHelper.LogInit("TokenDemo");
            Logger = Log4NetHelper.GetLogger();

            NodeInfoHelper.SetConfig("nodes-local");
            var node = NodeInfoHelper.Config.Nodes.First();

            NodeManager = new NodeManager(node.Endpoint);
            ContractManager = new ContractManager(NodeManager, node.Account);
        }

        private ILog Logger { get; }
        public INodeManager NodeManager { get; set; }

        public ContractManager ContractManager { get; set; }

        [TestMethod]
        public async Task ChainHeight_Test()
        {
            var height = await NodeManager.ApiClient.GetBlockHeightAsync();
            height.ShouldBeGreaterThan(1);
        }

        [TestMethod]
        public async Task Transfer_Test()
        {
            var testAccount = "22aa2d73PDgBkjBk5Jf98S2UPkC8hJDGnZqPUAoKvYqAq7Lfow";
            var transferAmount = 1000_00000000L;

            var tokenStub = ContractManager.TokenStub;
            //call
            var beforeBalance = await tokenStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = ContractManager.CallAccount,
                Symbol = "ELF"
            });

            //action transfer
            var transactionResult = await tokenStub.Transfer.SendAsync(new TransferInput
            {
                To = testAccount.ConvertAddress(),
                Symbol = "ELF",
                Amount = transferAmount,
                Memo = "transfer demo"
            });
            transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var transactionFee = transactionResult.TransactionResult.GetDefaultTransactionFee();

            //assert
            var afterBalance = await tokenStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = ContractManager.CallAccount,
                Symbol = "ELF"
            });
            afterBalance.Balance.ShouldBe(beforeBalance.Balance - transferAmount - transactionFee);

            var testBalance = await tokenStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = ContractManager.CallAccount,
                Symbol = "ELF"
            });
            testBalance.Balance.ShouldBe(transferAmount);
        }
    }
}