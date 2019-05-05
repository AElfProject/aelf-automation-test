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

        [TestMethod]
        [DataRow("446e83c9c9d9b653c3626bf80097cdd77c3dc9871376e4dc111e948aa7793a8e")]
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
    }
}