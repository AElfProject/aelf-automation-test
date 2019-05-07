using AElf.Automation.Common.Contracts;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AElf.Automation.SideChainTests
{
    [TestClass]
    public class SideChainCreate: SideChainTestBase
    {
        [TestInitialize]
        public void InitializeNodeTests()
        {
            base.Initialize();
        }
               

        [TestMethod]
        [DataRow("W4xEKTZcvPKXRAmdu9xEpM69ArF7gUxDh9MDgtsKnu7JfePXo")]
        public void RequestSideChain(string account)
        {
            Tester.TransferToken(InitAccount, account, 200000, "ELF");
            Tester.TokenApprove(account, 200000);
            
            var result = Tester.RequestSideChain(account);
            var proposalId =result.JsonInfo["result"]["ReadableReturnValue"].ToString().Replace("\"", "");
            _logger.WriteInfo($"proposal id is {proposalId}");
        }

        //f95200242dbedfa4eed01557274037edebbff826095b4ad46df1a82e7e8c49f3
        [TestMethod]
        [DataRow("2c966cb232c97a2d67848919fdd1875ffa6a9a3c583cda37b0e396b124ceac0f")]
        public void ApproveProposal(string proposalId)
        {
            foreach (var bp in BpNodeAddress)
            {
                Tester.Approve(bp, proposalId);
            }
        }

        [TestMethod]
        [DataRow(2750978)]
        public void CheckStatus(int chainId)
        {
            var status =Tester.GetChainStatus(chainId);
            _logger.WriteInfo($"side chain is {status}");
        }

        [TestMethod]
        [DataRow("W4xEKTZcvPKXRAmdu9xEpM69ArF7gUxDh9MDgtsKnu7JfePXo",2750978,100000)]
        public void Recharge(string account,int chainId,long amount)
        {
            if (Tester.GetBalance(account,"ELF").Balance < amount)
            {
                Tester.TransferToken(InitAccount, account, amount, "ELF");
            }
            Tester.TokenApprove(account, amount);
            var reCharge = Tester.Recharge(chainId, amount);
            var balance = Tester.GetBalance(Tester.CrossChainService.ContractAddress, "ELF");
            _logger.WriteInfo($"side chain lock balance is {balance}");
        }
    }
}