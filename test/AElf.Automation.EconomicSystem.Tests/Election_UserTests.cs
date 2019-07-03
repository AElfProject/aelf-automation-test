using System.Linq;
using AElf.Automation.Common.Contracts;
using AElf.Automation.Common.WebApi.Dto;
using AElf.Contracts.Profit;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
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
        [DataRow(0, 100)]
        public void Vote_One_Candidates_ForBP(int no, long amount)
        {
            var voteResult = Behaviors.UserVote(UserList[0], FullNodeAddress[no], 100, amount);
            var transactionResult = voteResult.InfoMsg as TransactionResultDto;

            transactionResult.ShouldNotBeNull();
            transactionResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        }

        [TestMethod]
        [DataRow(0, 2, 4)]
        public void Query_Candidate_Victories(int no1, int no2, int no3)
        {
            var victories = Behaviors.GetVictories();
            victories.Value.Count.ShouldBe(3);

            var publicKeys = victories.Value.Select(o => o.ToByteArray().ToHex()).ToList();

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
            foreach (var publicKey in miners.Pubkeys)
            {
                _logger.WriteInfo($"Miner PublicKey: {publicKey.ToByteArray().ToHex()}");
            }
        }


        [TestMethod]
        public void GetCurrentRoundInformation()
        {
            var roundInformation =
                Behaviors.ConsensusService.CallViewMethod(ConsensusMethod.GetCurrentRoundInformation, new Empty());
            _logger.WriteInfo(roundInformation.ToString());
        }
    }
}