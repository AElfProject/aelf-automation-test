using System.Collections.Generic;
using System.Linq;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.Election;
using AElf.Contracts.Profit;
using AElf.Contracts.Vote;
using AElf.CSharp.Core;
using AElf.Types;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace AElf.Automation.EconomicSystemTest
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
        public void Vote_One_Candidates_ForBP()
        {
            var amount = 1000_00000000;
            long fee = 0;
            var lockTime = 90;
            var i = 1;
            var voterInfo = new Dictionary<string, string>();
            foreach (var voter in Voter)
            {
                var transfer = Behaviors.TokenService.TransferBalance(InitAccount, voter, 20000_00000000);
                var transferFee = transfer.GetDefaultTransactionFee();
                fee += transferFee;
            }

            for (int j = 0; j < Voter.Count; j++)
                voterInfo.Add(FullNodeAddress[j], Voter[j]);

            foreach (var (key, value) in voterInfo)
            {
//            var full = FullNodeAddress[0];
                var voteResult = Behaviors.UserVote(value, key, lockTime * i, amount);
                var voteFee = voteResult.GetDefaultTransactionFee();
                fee += voteFee;
                var voteId = Hash.Parser.ParseFrom(ByteArrayHelper.HexStringToByteArray(voteResult.ReturnValue));
                var logVoteId = Voted.Parser
                    .ParseFrom(ByteString.FromBase64(
                        voteResult.Logs.First(l => l.Name.Equals(nameof(Voted))).NonIndexed))
                    .VoteId;
                var voteRecord = Behaviors.VoteService.CallViewMethod<VotingRecord>(VoteMethod.GetVotingRecord, voteId);
                voteRecord.Amount.ShouldBe(amount);
                Logger.Info($"vote id is: {voteId}\n" +
                            $"{logVoteId}\n" +
                            $"{voteRecord.Amount}");
                voteResult.ShouldNotBeNull();
                voteResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
                var result =
                    Behaviors.ElectionService.CallViewMethod<CandidateVote>(ElectionMethod.GetCandidateVoteWithRecords,
                        new StringValue {Value = NodeManager.AccountManager.GetPublicKey(key)});
                result.ObtainedActiveVotingRecords.Select(o => o.VoteId).ShouldContain(voteId);
                i++;
            }

            Logger.Info($"{fee}");
        }

        [TestMethod]
        [DataRow("3f0e46bf7fe01f416444f1396827ea07217437aef29196252566bba3c0594c52")]
        public void ChangeUserVote(string hash)
        {
            var account = "YF8o6ytMB7n5VF9d1RDioDXqyQ9EQjkFK3AwLPCH2b9LxdTEq";
            var candidate = FullNodeAddress[1];
            var voteId = Hash.LoadFromHex(hash);
            var transactionResult = Behaviors.UserChangeVote(account, candidate, voteId);
            transactionResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var voteResult = Behaviors.ElectionService.CallViewMethod<CandidateVote>(
                ElectionMethod.GetCandidateVoteWithRecords,
                new StringValue {Value = NodeManager.AccountManager.GetPublicKey(candidate)});
            voteResult.ObtainedActiveVotingRecords.Select(o => o.VoteId).ShouldContain(voteId);
        }

        [TestMethod]
        public void CheckProfit()
        {
            var profit = Behaviors.ProfitService;
            var account = "YF8o6ytMB7n5VF9d1RDioDXqyQ9EQjkFK3AwLPCH2b9LxdTEq";
            var schemeId = Behaviors.Schemes[SchemeType.CitizenWelfare].SchemeId;
            var term = Behaviors.ConsensusService.GetCurrentTermInformation();
            var voteProfit =
                profit.GetProfitDetails(account, schemeId);
            if (voteProfit.Equals(new ProfitDetails())) return;
            Logger.Info($"20% user vote profit for account: {account}.\r\nDetails number: {voteProfit.Details}");

            //Get user profit amount
            var profitMap = profit.GetProfitsMap(account, schemeId);
            if (profitMap.Equals(new ReceivedProfitsMap()))
                return;
            var profitAmount = profitMap.Value["ELF"];
            Logger.Info($"{term.TermNumber} Profit amount: user {account} profit amount is {profitAmount}");
            var beforeBalance = Behaviors.TokenService.GetUserBalance(account);
            var newProfit = profit.GetNewTester(account);
            var profitResult = newProfit.ExecuteMethodWithResult(ProfitMethod.ClaimProfits, new ClaimProfitsInput
            {
                SchemeId = schemeId,
                Beneficiary = account.ConvertAddress()
            });
            profitResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var fee = profitResult.GetDefaultTransactionFee();
            var afterBalance = Behaviors.TokenService.GetUserBalance(account);
            afterBalance.ShouldBe(beforeBalance + profitAmount - fee);
            var afterProfitAmount = profit.GetProfitsMap(account, schemeId);
            afterProfitAmount.Equals(new ReceivedProfitsMap()).ShouldBeTrue();
            Logger.Info(fee);
        }

        [TestMethod]
        public void CheckProfitCandidates()
        {
            var profit = Behaviors.ProfitService;
            var candidates = Behaviors.GetCandidates();
            var account = Address.FromPublicKey(candidates.Value.First().ToByteArray());
            var schemeId = Behaviors.Schemes[SchemeType.BackupSubsidy].SchemeId;
            long profitAmount = 0;

            foreach (var candidate in FullNodeAddress)
            {
                var profitMap = profit.GetProfitsMap(candidate, schemeId);
                if (profitMap.Equals(new ReceivedProfitsMap()))
                    continue;
                var profitAmountFull = profitMap.Value["ELF"];
                if (candidate.Equals(account.ToBase58()))
                    profitAmount = profitAmountFull;
                Logger.Info($"Profit amount: user {candidate} profit amount is {profitAmountFull}");
            }

            var beforeBalance = Behaviors.TokenService.GetUserBalance(account.ToBase58());
            var newProfit = profit.GetNewTester(account.ToBase58());
            var profitResult = newProfit.ExecuteMethodWithResult(ProfitMethod.ClaimProfits, new ClaimProfitsInput
            {
                SchemeId = schemeId,
                Beneficiary = account
            });
            profitResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var fee = profitResult.GetDefaultTransactionFee();
            var afterBalance = Behaviors.TokenService.GetUserBalance(account.ToBase58());
            afterBalance.ShouldBe(beforeBalance + profitAmount - fee);
        }

        [TestMethod]
        public void ClaimBackupSubsidyCandidates()
        {
            var profit = Behaviors.ProfitService;
            var candidates = Behaviors.GetCandidates();
            var MinerBasicReward = Behaviors.Schemes[SchemeType.MinerBasicReward].SchemeId;
            var ReElectionReward = Behaviors.Schemes[SchemeType.ReElectionReward].SchemeId;
            var VotesWeightReward = Behaviors.Schemes[SchemeType.VotesWeightReward].SchemeId;
            var CitizenWelfare = Behaviors.Schemes[SchemeType.CitizenWelfare].SchemeId;
            var BackupSubsidy = Behaviors.Schemes[SchemeType.BackupSubsidy].SchemeId;
            long profitAmount = 0;

            foreach (var candidate in FullNodeAddress)
            {
                var profitMap = profit.GetProfitsMap(candidate, BackupSubsidy);
                if (profitMap.Equals(new ReceivedProfitsMap()))
                    continue;
                var profitAmountFull = profitMap.Value["ELF"];
                Logger.Info($"Profit amount: user {candidate} profit amount is {profitAmountFull}");
                var beforeBalance = Behaviors.TokenService.GetUserBalance(candidate);
                var newProfit = profit.GetNewTester(candidate);
                var profitResult = newProfit.ExecuteMethodWithResult(ProfitMethod.ClaimProfits, new ClaimProfitsInput
                {
                    SchemeId = BackupSubsidy,
                    Beneficiary = candidate.ConvertAddress()
                });
                profitResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
                var fee = profitResult.GetDefaultTransactionFee();
                var afterBalance = Behaviors.TokenService.GetUserBalance(candidate);
                afterBalance.ShouldBe(beforeBalance + profitAmountFull - fee);
            }
        }

        [TestMethod]
        public void Withdraw()
        {
            var account = "7BSmhiLtVqHSUVGuYdYbsfaZUGpkL2ingvCmVPx66UR5L5Lbs";
            var voteId = "2888302e588dd3506aca811d0b81f686feb12e3a7218578d62fd45fbac36edae";
            var amount = 100000000000;
            Behaviors.ElectionService.SetAccount(account);
            var beforeVoteBalance = Behaviors.TokenService.GetUserBalance(account, "VOTE");
            var beforeShareBalance = Behaviors.TokenService.GetUserBalance(account, "SHARE");
            beforeShareBalance.ShouldBe(beforeVoteBalance);

            var beforeElfBalance = Behaviors.TokenService.GetUserBalance(account);
            var result =
                Behaviors.ElectionService.ExecuteMethodWithResult(ElectionMethod.Withdraw,
                    Hash.LoadFromHex(voteId));
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var fee = result.GetTransactionFee().Item2;
            var afterVoteBalance = Behaviors.TokenService.GetUserBalance(account, "VOTE");
            var afterShareBalance = Behaviors.TokenService.GetUserBalance(account, "SHARE");

            var afterElfBalance = Behaviors.TokenService.GetUserBalance(account);
            afterVoteBalance.ShouldBe(beforeVoteBalance - amount);
            afterShareBalance.ShouldBe(beforeShareBalance - amount);
            afterElfBalance.ShouldBe(beforeElfBalance + amount - fee);
        }

        [TestMethod]
        public void Vote_All_Candidates_ForBP()
        {
            foreach (var full in FullNodeAddress)
            {
                var voteResult = Behaviors.UserVote(InitAccount, full, 100, 2000);

                voteResult.ShouldNotBeNull();
                voteResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            }
        }

        [TestMethod]
        [DataRow(0, 2, 4)]
        public void Query_Candidate_Victories(int no1, int no2, int no3)
        {
            var victories = Behaviors.GetVictories();
            victories.Value.Count.ShouldBe(3);

            var publicKeys = victories.Value.Select(o => o.ToByteArray().ToHex()).ToList();

            publicKeys.Contains(
                Behaviors.NodeManager.GetAccountPublicKey(FullNodeAddress[no1])).ShouldBeTrue();
            publicKeys.Contains(
                Behaviors.NodeManager.GetAccountPublicKey(FullNodeAddress[no2])).ShouldBeTrue();
            publicKeys.Contains(
                Behaviors.NodeManager.GetAccountPublicKey(FullNodeAddress[no3])).ShouldBeTrue();
        }

        [TestMethod]
        public void GetCurrentRoundInformation()
        {
            var roundInformation =
                Behaviors.ConsensusService.CallViewMethod<Round>(ConsensusMethod.GetCurrentRoundInformation,
                    new Empty());
            Logger.Info(roundInformation.ToString());
        }

        [TestMethod]
        public void VoteWeight()
        {
            var voteWeight =
                Behaviors.ElectionService.CallViewMethod<Int64Value>(ElectionMethod.GetCalculateVoteWeight,
                    new VoteInformation
                    {
                        Amount = 1000_0000000,
                        LockTime = 365.Mul(86400)
                    });
            Logger.Info(voteWeight.Value);
        }
    }
}