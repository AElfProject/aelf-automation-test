using System.Linq;
using AElf.Automation.Common.WebApi.Dto;
using AElf.Contracts.Profit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace AElf.Automation.EconomicSystem.Tests
{
    [TestClass]
    public class UserTests : ElectionTests
    {
        [TestInitialize]
        public void InitializeUserTests()
        {
            Initialize();
        }

        [TestCleanup]
        public void CleanUpUserTests()
        {
            TestCleanUp();
        }

        [TestMethod]
        public void Vote_One_Candidate_With_NotEnough_Token_Scenario()
        {
            var voteResult1 = Behaviors.UserVote(UserList[0], FullNodeAddress[0], 90, 2000);
            var transactionResult = voteResult1.InfoMsg as TransactionResultDto;
            transactionResult?.Status.ShouldBe("Failed");
            transactionResult?.Error.Contains("Insufficient balance").ShouldBeTrue();
        }

        [TestMethod]
        [DataRow(0, 1, 2, 100)]
        public void Vote_Three_Candidates_ForBP(int no1, int no2, int no3, long amount)
        {
            var voteResult1 = Behaviors.UserVote(UserList[0], FullNodeAddress[no1], 90, amount);
            
            var txResult1 = voteResult1.InfoMsg as TransactionResultDto;
            txResult1.ShouldNotBeNull();
            txResult1.Status.ShouldBe("Mined");

            var voteResult2 = Behaviors.UserVote(UserList[1], FullNodeAddress[no2], 90, amount);
            var txResult2 = voteResult2.InfoMsg as TransactionResultDto;
            txResult2.ShouldNotBeNull();
            txResult2.Status.ShouldBe("Mined");
            
            var voteResult3 = Behaviors.UserVote(UserList[2], FullNodeAddress[no3], 90, amount);
            
            var txResult3 = voteResult3.InfoMsg as TransactionResultDto;
            txResult3.ShouldNotBeNull();
            txResult3.Status.ShouldBe("Mined");

            //verify victories
            Query_Candidate_Victories(no1, no2, no3);
        }

        [TestMethod]
        [DataRow(2, 100)]
        public void Vote_One_Candidates_ForBP(int no, long amount)
        {
            var voteResult = Behaviors.UserVote(UserList[3], FullNodeAddress[no], 90, amount);
            var transactionResult = voteResult.InfoMsg as TransactionResultDto;
            
            transactionResult.ShouldNotBeNull();
            transactionResult.Status.ShouldBe("Mined");
        }

        [TestMethod]
        [DataRow(10)]
        public void Vote_MultipleTimes(int userCount)
        {
            for (int i = 0; i < userCount; i++)
            {
                Behaviors.UserVoteWithTxIds(UserList[i], FullNodeAddress[i%6], 90, 20);
            }
            Behaviors.ElectionService.CheckTransactionResultList();
        }

        [TestMethod]
        [DataRow(0, 2, 4)]
        public void Query_Candidate_Victories(int no1, int no2, int no3)
        {
            var victories = Behaviors.GetVictories();
            victories.Value.Count.ShouldBe(3);

            var publicKeys = victories.Value.Select(o=>o.ToByteArray().ToHex()).ToList();

            publicKeys.Contains(
                Behaviors.ApiHelper.GetPublicKeyFromAddress(FullNodeAddress[no1])).ShouldBeTrue();
            publicKeys.Contains(
                Behaviors.ApiHelper.GetPublicKeyFromAddress(FullNodeAddress[no2])).ShouldBeTrue();
            publicKeys.Contains(
                Behaviors.ApiHelper.GetPublicKeyFromAddress(FullNodeAddress[no3])).ShouldBeTrue();
        }

        [TestMethod]
        public void Get_Current_Miners()
        {
            var miners = Behaviors.GetCurrentMiners();
            foreach (var publicKey in miners.PublicKeys)
            {
                _logger.WriteInfo($"Miner PublicKey: {publicKey.ToByteArray().ToHex()}");
            }
        }

        [TestMethod]
        [DataRow(3)]
        public void GetUserProfitAndWithDraw(int userId)
        {
            var user = UserList[userId];
            var beforeBalance = Behaviors.GetBalance(user);
            _logger.WriteInfo($"User before balance: {beforeBalance.Balance}");
            var voteProfit =
                Behaviors.GetProfitDetails(user, ProfitItemsIds[Behaviors.ProfitType.CitizenWelfare]);

            _logger.WriteInfo($"20% user vote profit balance: {voteProfit}");
            if (voteProfit.Equals(new ProfitDetails())) return;
            Behaviors.Profit(user, ProfitItemsIds[Behaviors.ProfitType.CitizenWelfare]);
                
            var afterBalance = Behaviors.GetBalance(user);
            _logger.WriteInfo($"User withdraw end balance: {afterBalance.Balance}");
        }

        [TestMethod]
        public void GetTermCitizenWelfare()
        {
            var term = Behaviors.GetCurrentTermInformation();
            for (var i = 1; i < term; i++)
            {
                var profitsInformation = Behaviors.GetReleasedProfitsInformation(ProfitItemsIds[Behaviors.ProfitType.CitizenWelfare], i);
                _logger.WriteInfo($"Term: {i}");
                _logger.WriteInfo($"TotalWeight: {profitsInformation.TotalWeight}");
                _logger.WriteInfo($"ProfitsAmount: {profitsInformation.ProfitsAmount}");
            }
        }
    }
}