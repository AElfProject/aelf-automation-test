using System.Linq;
using AElf.Automation.Common.WebApi.Dto;
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
        [DataRow(0, 2, 5, 100)]
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
        [DataRow(5, 100)]
        public void Vote_One_Candidates_ForBP(int no, long amount)
        {
            var voteResult = Behaviors.UserVote(UserList[0], FullNodeAddress[no], 90, amount);
            var transactionResult = voteResult.InfoMsg as TransactionResultDto;
            
            transactionResult.ShouldNotBeNull();
            transactionResult.Status.ShouldBe("Mined");
        }

        [TestMethod]
        [DataRow(0, 1, 3)]
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
    }
}