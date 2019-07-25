using AElf.Automation.Common.Contracts;
using AElf.Automation.Common.WebApi.Dto;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AElf.Automation.SideChainTests
{
    [TestClass]
    public class SideChainCreate : SideChainTestBase
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

            var result = Tester.RequestSideChain(account, 100000);
            var transactionResult = result.InfoMsg as TransactionResultDto;
            var message = transactionResult.ReadableReturnValue;
            _logger.Info($"proposal message is {message}");
        }

        //382395c5c1ab8144a522c07bd7dbbb3a1e02b1a345e1cdecb3bdd610fb2f1d20 2882050
        //c2e18f61111bba0b15e1c250c2f36c0a70375e7ad84a09f0baae9fbe34f98b7b 2947586
        [TestMethod]
        [DataRow("8f639af4c2f132ddd4827d3d6700be37c244b26c619fd3c9d05106af4b0d368c")]
        [DataRow("daab2a83e3d5154e2d848db4c17c5ecebc0ce1cbff83525d938751231d6b9440")]
//        [DataRow("b8ed3964a6567a2aafd62a82e4cfe4515757cb0acacea675f7bdd9664737f5c1")]
//        [DataRow("75d7b205ee08c44408e92dc437390da05f769a967b7db481e6ad7d16f3898ea2")] //disposal
        public void ApproveProposal(string proposalId)
        {
            foreach (var bp in BpNodeAddress)
            {
                Tester.Approve(bp, proposalId);
            }
        }

        [TestMethod]
        [DataRow(2750978)]
        [DataRow(2816514)]
        [DataRow(2882050)]
        [DataRow(2947586)]
        public void CheckStatus(int chainId)
        {
            var status = Tester.GetChainStatus(chainId);
            _logger.Info($"side chain is {status}");
        }

        [TestMethod]
        [DataRow("W4xEKTZcvPKXRAmdu9xEpM69ArF7gUxDh9MDgtsKnu7JfePXo", 2750978, 1000)]
        public void Recharge(string account, int chainId, long amount)
        {
            if (Tester.GetBalance(account, "ELF").Balance < amount)
            {
                Tester.TransferToken(InitAccount, account, amount, "ELF");
            }

            Tester.TokenApprove(account, amount);
            var reCharge = Tester.Recharge(account, chainId, amount);
            var balance = Tester.GetBalance(Tester.CrossChainService.ContractAddress, "ELF");
            _logger.Info($"side chain lock balance is {balance}");
        }

        [TestMethod]
        [DataRow("W4xEKTZcvPKXRAmdu9xEpM69ArF7gUxDh9MDgtsKnu7JfePXo", 2882050)]
        public void WithdrawRequest(string account, int chainId)
        {
            Tester.WithdrawRequest(account, chainId);
            var chainStatus = Tester.GetChainStatus(chainId);
            _logger.Info($"side chain is {chainStatus}");
        }

        [TestMethod]
        [DataRow("W4xEKTZcvPKXRAmdu9xEpM69ArF7gUxDh9MDgtsKnu7JfePXo", 2882050)]
        public void RequestChainDisposal(string account, int chainId)
        {
            var result = Tester.RequestChainDisposal(account, chainId);
            var transactionReturn = result.InfoMsg as TransactionResultDto;
            var proposalId = transactionReturn.ReadableReturnValue;

            _logger.Info($"Disposal chain proposal id is {proposalId}");
        }


        [TestMethod]
        [DataRow("W4xEKTZcvPKXRAmdu9xEpM69ArF7gUxDh9MDgtsKnu7JfePXo")]
        public void CheckBalance(string account)
        {
            var balance = Tester.GetBalance(Tester.CrossChainService.ContractAddress, "ELF");
            _logger.Info($"side chain balance is {balance}");

            var userBalance = Tester.GetBalance(account, "ELF");
            _logger.Info($"user balance is {userBalance}");
        }
    }
}