using System.Linq;
using AElfChain.Common.Contracts;
using AElf.Contracts.Election;
using AElf.Types;
using AElfChain.SDK.Models;
using Google.Protobuf.WellKnownTypes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace AElf.Automation.EconomicSystem.Tests
{
    [TestClass]
    public class NodeTests : ElectionTests
    {
        [TestInitialize]
        public void InitializeNodeTests()
        {
            Initialize();
        }

        [TestCleanup]
        public void CleanUpNodeTests()
        {
            TestCleanUp();
        }

        [TestMethod]
        public void Announcement_AllNodes_Scenario()
        {
            foreach (var nodeAddress in FullNodeAddress)
            {
                var result = Behaviors.AnnouncementElection(nodeAddress);
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            }
        }

        [TestMethod]
        public void NodeAnnounceElectionAction()
        {
            foreach (var user in FullNodeAddress)
            {
                var election = Behaviors.ElectionService.GetNewTester(user, "123");
                var electionResult = election.ExecuteMethodWithResult(ElectionMethod.AnnounceElection, new Empty());
            }

            var candidateList = Behaviors.GetCandidates();
            foreach (var publicKey in candidateList.Value)
                _logger.Info($"Candidate PublicKey: {publicKey.ToByteArray().ToHex()}");
        }


        [TestMethod]
        public void Get_Miners_Count()
        {
            var miners = Behaviors.GetMinersCount();
            miners.ShouldBe(3);
        }

        [TestMethod]
        [DataRow(0)]
        public void GetVotesInformationResult(int nodeId)
        {
            var records = Behaviors.GetElectorVoteWithAllRecords(UserList[nodeId]);
        }

        [TestMethod]
        public void GetVictories()
        {
            var victories = Behaviors.GetVictories();

            var publicKeys = victories.Value.Select(o => o.ToByteArray().ToHex()).ToList();

            publicKeys.Contains(Behaviors.NodeManager.GetAccountPublicKey(FullNodeAddress[0])).ShouldBeTrue();
            publicKeys.Contains(Behaviors.NodeManager.GetAccountPublicKey(FullNodeAddress[1])).ShouldBeTrue();
            publicKeys.Contains(Behaviors.NodeManager.GetAccountPublicKey(FullNodeAddress[2])).ShouldBeTrue();
        }

        [TestMethod]
        [DataRow(5)]
        public void QuitElection(int nodeId)
        {
            var beforeBalance = Behaviors.GetBalance(FullNodeAddress[nodeId]).Balance;
            var result = Behaviors.QuitElection(FullNodeAddress[nodeId]);

            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var afterBalance = Behaviors.GetBalance(FullNodeAddress[nodeId]).Balance;
            beforeBalance.ShouldBe(afterBalance - 100_000L);
        }

        [TestMethod]
        public void GetCandidates()
        {
            var candidates = Behaviors.GetCandidates();
            _logger.Info($"Candidate count: {candidates.Value.Count}");
            foreach (var candidate in candidates.Value) _logger.Info($"Candidate: {candidate.ToByteArray().ToHex()}");
        }

        [TestMethod]
        public void GetCandidateHistory()
        {
            foreach (var candidate in FullNodeAddress)
            {
                var candidateResult = Behaviors.GetCandidateInformation(candidate);
                _logger.Info("Candidate: ");
                _logger.Info($"PublicKey: {candidateResult.Pubkey}");
                _logger.Info($"Terms: {candidateResult.Terms}");
                _logger.Info($"ContinualAppointmentCount: {candidateResult.ContinualAppointmentCount}");
                _logger.Info($"ProducedBlocks: {candidateResult.ProducedBlocks}");
                _logger.Info($"MissedTimeSlots: {candidateResult.MissedTimeSlots}");
                _logger.Info($"AnnouncementTransactionId: {candidateResult.AnnouncementTransactionId}");
            }
        }
    }
}