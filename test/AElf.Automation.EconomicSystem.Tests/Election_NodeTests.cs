using System.Linq;
using AElf.Automation.Common.Helpers;
using AElf.Automation.Common.WebApi;
using AElf.Automation.Common.WebApi.Dto;
using AElf.Contracts.Profit;
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
        public void First_Announcement_Election_Scenario()
        {
            var beforeBalance = Behaviors.GetBalance(FullNodeAddress[0]).Balance;
            var result = Behaviors.AnnouncementElection(FullNodeAddress[0]);
            var transactionResult = result.InfoMsg as TransactionResultDto;
            transactionResult?.Status.ShouldBe("Mined");

            var afterBalance = Behaviors.GetBalance(FullNodeAddress[0]).Balance;
            beforeBalance.ShouldBe(afterBalance + 100_000L);
        }

        [TestMethod]
        public void Announcement_Election_MultipleTimes_Scenario()
        {
            var beforeBalance = Behaviors.GetBalance(FullNodeAddress[1]).Balance;
            var result = Behaviors.AnnouncementElection(FullNodeAddress[1]);
            var transactionResult = result.InfoMsg as TransactionResultDto;
            transactionResult?.Status.ShouldBe("Mined");

            var result1 = Behaviors.AnnouncementElection(FullNodeAddress[1]);
            var transactionResult1 = result1.InfoMsg as TransactionResultDto;
            transactionResult1?.Status.ShouldBe("Failed");

            var result2 = Behaviors.AnnouncementElection(FullNodeAddress[1]);
            var transactionResult2 = result2.InfoMsg as TransactionResultDto;
            transactionResult2?.Status.ShouldBe("Failed");

            var afterBalance = Behaviors.GetBalance(FullNodeAddress[1]).Balance;
            beforeBalance.ShouldBe(afterBalance + 100_000L);
        }

        [TestMethod]
        public void Announcement_Election_With_NotEnough_Token_Scenario()
        {
            var beforeBalance = Behaviors.GetBalance(UserList[0]).Balance;
            var result = Behaviors.AnnouncementElection(UserList[0]);
            var transactionResult = result.InfoMsg as TransactionResultDto;
            transactionResult?.Status.ShouldBe("Failed");

            var afterBalance = Behaviors.GetBalance(UserList[0]).Balance;
            beforeBalance.ShouldBe(afterBalance);
        }

        [TestMethod]
        public void QuiteElection_And_Announcement_Again_Scenario()
        {
            var beforeBalance = Behaviors.GetBalance(FullNodeAddress[5]).Balance;

            var announcement1 = Behaviors.AnnouncementElection(FullNodeAddress[5]);
            var transactionResult1 = announcement1.InfoMsg as TransactionResultDto;
            transactionResult1?.Status.ShouldBe("Mined");
            var balance1 = Behaviors.GetBalance(FullNodeAddress[5]).Balance;

            var quitElection = Behaviors.QuitElection(FullNodeAddress[5]);
            var transactionResult2 = quitElection.InfoMsg as TransactionResultDto;
            transactionResult2?.Status.ShouldBe("Mined");
            var balance2 = Behaviors.GetBalance(FullNodeAddress[5]).Balance;

            var announcement2 = Behaviors.AnnouncementElection(FullNodeAddress[5]);
            var transactionResult3 = announcement2.InfoMsg as TransactionResultDto;
            transactionResult3?.Status.ShouldBe("Mined");

            var candidates = Behaviors.GetCandidates();
            candidates.Value.Count.ShouldBe(0);

            var afterBalance = Behaviors.GetBalance(FullNodeAddress[5]).Balance;
            beforeBalance.ShouldBe(afterBalance);
        }

        [TestMethod]
        public void Announcement_AllNodes_Scenario()
        {
            foreach (var nodeAddress in FullNodeAddress)
            {
                var result = Behaviors.AnnouncementElection(nodeAddress);
                var transactionResult = result.InfoMsg as TransactionResultDto;
                transactionResult?.Status.ShouldBe("Mined");
            }
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

            publicKeys.Contains(Behaviors.ApiHelper.GetPublicKeyFromAddress(FullNodeAddress[0])).ShouldBeTrue();
            publicKeys.Contains(Behaviors.ApiHelper.GetPublicKeyFromAddress(FullNodeAddress[1])).ShouldBeTrue();
            publicKeys.Contains(Behaviors.ApiHelper.GetPublicKeyFromAddress(FullNodeAddress[2])).ShouldBeTrue();
        }

        [TestMethod]
        [DataRow(5)]
        public void QuitElection(int nodeId)
        {
            var beforeBalance = Behaviors.GetBalance(FullNodeAddress[nodeId]).Balance;
            var result = Behaviors.QuitElection(FullNodeAddress[nodeId]);

            var transactionResult = result.InfoMsg as TransactionResultDto;
            transactionResult?.Status.ShouldBe("Mined");

            var afterBalance = Behaviors.GetBalance(FullNodeAddress[nodeId]).Balance;
            beforeBalance.ShouldBe(afterBalance - 100_000L);
        }

        [TestMethod]
        public void GetCandidates()
        {
            var candidates = Behaviors.GetCandidates();
            _logger.WriteInfo($"Candidate count: {candidates.Value.Count}");
            foreach (var candidate in candidates.Value)
            {
                _logger.WriteInfo($"Candidate: {candidate.ToByteArray().ToHex()}");
            }
        }

        [TestMethod]
        [DataRow(1)]
        public void GetTermShot(int termNumber)
        {
            var termShot = Behaviors.GetTermSnapshot(termNumber);
        }

        [TestMethod]
        public void GetAllCandidatesBalance()
        {
            for (var i = 0; i < FullNodeAddress.Count; i++)
            {
                var balanceResult = Behaviors.GetBalance(FullNodeAddress[i]);

                _logger.WriteInfo($"Id: {i + 1}, Address: {FullNodeAddress[i]}, Balance: {balanceResult.Balance}");
            }
        }

        [TestMethod]
        public void GetProfitItems()
        {
            var result = Behaviors.GetCreatedProfitItems();
            _logger.WriteInfo($"{result}");
        }

        [TestMethod]
        [DataRow(2)]
        public void GetPIBlance(long period)
        {
            var treasuryAddress = Behaviors.GetTreasuryAddress(ProfitItemsIds[Behaviors.ProfitType.Treasury]);
            var treasuryBalance = Behaviors.GetBalance(treasuryAddress.GetFormatted());
            _logger.WriteInfo($"Treasury balance is {treasuryBalance.Balance}");

            foreach (var profitId in ProfitItemsIds)
            {
                var address = Behaviors.GetProfitItemVirtualAddress(profitId.Value, period);
                var balance = Behaviors.GetBalance(address.GetFormatted());
                _logger.WriteInfo($"{profitId.Key}balance is {balance.Balance}");
            }
        }

        [TestMethod]
        public void CandidateGetPiDetails()
        {
            foreach (var address in FullNodeAddress)
            {
                var reElectionResult =
                    Behaviors.GetProfitDetails(address, ProfitItemsIds[Behaviors.ProfitType.ReElectionReward]);
                _logger.WriteInfo($"{address} ReElectionReward detail is {reElectionResult}");
            }
        }

        [TestMethod]
        [DataRow(1, 20)]
        public void GetAllPeriodsBalance(int startPeriod, int endPeriod)
        {
            var treasuryAddress = Behaviors.GetTreasuryAddress(ProfitItemsIds[Behaviors.ProfitType.Treasury]);
            var treasuryBalance = Behaviors.GetBalance(treasuryAddress.GetFormatted());
            _logger.WriteInfo($"Treasury balance is {treasuryBalance.Balance}");

            for (var i = startPeriod; i <= endPeriod; i++)
            {
                _logger.WriteInfo($"term number: {i}");
                foreach (var profitId in ProfitItemsIds)
                {
                    var address = Behaviors.GetProfitItemVirtualAddress(profitId.Value, i);
                    var balance = Behaviors.GetBalance(address.GetFormatted());
                    _logger.WriteInfo($"{profitId.Key}balance is {balance.Balance}");
                }
            }
        }

        [TestMethod]
        public void GetCandidateHistory()
        {
            foreach (var candidate in FullNodeAddress)
            {
                var candidateResult = Behaviors.GetCandidateInformation(candidate);
                _logger.WriteInfo("Candidate: ");
                _logger.WriteInfo($"PublicKey: {candidateResult.PublicKey}");
                _logger.WriteInfo($"Terms: {candidateResult.Terms}");
                _logger.WriteInfo($"ContinualAppointmentCount: {candidateResult.ContinualAppointmentCount}");
                _logger.WriteInfo($"ProducedBlocks: {candidateResult.ProducedBlocks}");
                _logger.WriteInfo($"MissedTimeSlots: {candidateResult.MissedTimeSlots}");
                _logger.WriteInfo($"AnnouncementTransactionId: {candidateResult.AnnouncementTransactionId}");
            }
        }

        [TestMethod]
        public void GetCandidateProfitAndWithDraw()
        {
            foreach (var profitItem in ProfitItemsIds)
            {
                _logger.WriteInfo($"{profitItem.Key.ToString()}: {profitItem.Value}");             
            }
            foreach (var candidate in FullNodeAddress)
            {
                var beforeBalance = Behaviors.GetBalance(candidate);
                _logger.WriteInfo($"Candidate: {candidate}");
                _logger.WriteInfo($"Before Balance: {beforeBalance.Balance}");

                var basicProfit =
                    Behaviors.GetProfitDetails(candidate, ProfitItemsIds[Behaviors.ProfitType.BasicMinerReward]);
                var voteWeightProfit =
                    Behaviors.GetProfitDetails(candidate, ProfitItemsIds[Behaviors.ProfitType.VotesWeightReward]);
                var reElectionProfit =
                    Behaviors.GetProfitDetails(candidate, ProfitItemsIds[Behaviors.ProfitType.ReElectionReward]);
                var backupProfit =
                    Behaviors.GetProfitDetails(candidate, ProfitItemsIds[Behaviors.ProfitType.BackSubsidy]);
                var voteProfit =
                    Behaviors.GetProfitDetails(candidate, ProfitItemsIds[Behaviors.ProfitType.CitizenWelfare]);

                _logger.WriteInfo($"40% basic generate block profit balance: {basicProfit}");
                _logger.WriteInfo($"10% vote weight profit balance: {voteWeightProfit}");
                _logger.WriteInfo($"10% re election profit balance: {reElectionProfit}");
                _logger.WriteInfo($"20% backup node profit balance: {backupProfit}");
                _logger.WriteInfo($"20% user vote profit balance: {voteProfit}");

                if (!basicProfit.Equals(new ProfitDetails()))
                {
                    Behaviors.Profit(candidate, ProfitItemsIds[Behaviors.ProfitType.BasicMinerReward]);
                    var balance = Behaviors.GetBalance(candidate).Balance;
                    _logger.WriteInfo($"Balance: {balance}");
                }

                if (!voteWeightProfit.Equals(new ProfitDetails()))
                {
                    Behaviors.Profit(candidate, ProfitItemsIds[Behaviors.ProfitType.VotesWeightReward]);
                    var balance = Behaviors.GetBalance(candidate).Balance;
                    _logger.WriteInfo($"Balance: {balance}");
                }

                if (!reElectionProfit.Equals(new ProfitDetails()))
                {
                    Behaviors.Profit(candidate, ProfitItemsIds[Behaviors.ProfitType.ReElectionReward]);
                    var balance = Behaviors.GetBalance(candidate).Balance;
                    _logger.WriteInfo($"Balance: {balance}");
                }

                if (!backupProfit.Equals(new ProfitDetails()))
                {
                    Behaviors.Profit(candidate, ProfitItemsIds[Behaviors.ProfitType.BackSubsidy]);
                    var balance = Behaviors.GetBalance(candidate).Balance;
                    _logger.WriteInfo($"Balance: {balance}");
                }

                _logger.WriteInfo(string.Empty);
            }
        }
    }
}