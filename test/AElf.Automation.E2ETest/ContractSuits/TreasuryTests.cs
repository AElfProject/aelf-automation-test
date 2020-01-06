using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.Contracts.Treasury;
using AElf.Types;
using AElfChain.Common;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace AElf.Automation.E2ETest.ContractSuits
{
    [TestClass]
    public class TreasuryTests : ContractTestBase
    {
        [TestMethod]
        public async Task Donate_Test()
        {
            var nodeAccounts = NodeInfoHelper.Config.Nodes.Select(o => o.Account).ToList();
            //prepare tester
            string tester;
            while (true)
            {
                tester = ContractManager.NodeManager.GetRandomAccount();
                if (nodeAccounts.Contains(tester)) continue;
                var transferResult = await ContractManager.TokenStub.Transfer.SendAsync(new TransferInput
                {
                    To = tester.ConvertAddress(),
                    Symbol = "ELF",
                    Amount = 100_00000000,
                    Memo = "Vote test"
                });
                transferResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
                break;
            }
            
            //donate
            var beforeBalance = ContractManager.Token.GetUserBalance(tester);
            var treasuryStub = ContractManager.Genesis.GetTreasuryStub(tester);
            var donateResult = await treasuryStub.Donate.SendAsync(new DonateInput
            {
                Symbol = "ELF",
                Amount = 50_00000000
            });
            donateResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var transactionFee = donateResult.TransactionResult.TransactionFee.GetDefaultTransactionFee();
            var afterBalance = ContractManager.Token.GetUserBalance(tester);
            beforeBalance.ShouldBe(afterBalance + transactionFee + 50_00000000);
            
            //donate all
            var donateAllResult = await treasuryStub.DonateAll.SendAsync(new DonateAllInput
            {
                Symbol = "ELF"
            });
            donateAllResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var balance = ContractManager.Token.GetUserBalance(tester);
            balance.ShouldBe(0);
        }
    }
}