using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Description;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.Election;
using AElf.Contracts.Profit;
using AElf.Contracts.Vote;
using AElf.CSharp.Core;
using AElf.Types;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using Google.Protobuf;
using Google.Protobuf.Collections;
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
            var term = Behaviors.ConsensusService.GetCurrentTermInformation();
            var voterInfo = new Dictionary<string, string>();
            foreach (var voter in Voter)
            {
                var transfer = Behaviors.TokenService.TransferBalance(InitAccount, voter, 20000_00000000);
                var transferFee = transfer.GetDefaultTransactionFee();
                fee += transferFee;
            }

//            for (int j = 0; j < FullNodeAddress.Count; j++)
//            {
//                var k = j;
//                while (k > Voter.Count - 1)
//                {
//                    k = k - Voter.Count;
//                }
//                voterInfo.Add(FullNodeAddress[j], Voter[k]);
//            }

            for (int j = 0; j < Voter.Count; j++)
                voterInfo.Add(FullNodeAddress[j], Voter[j]);

            foreach (var (key, value) in voterInfo)
            {
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
                            $"{voteRecord.Amount}\n" +
                            $"time: {lockTime * i}");
                voteResult.ShouldNotBeNull();
                voteResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
                var result =
                    Behaviors.ElectionService.CallViewMethod<CandidateVote>(ElectionMethod.GetCandidateVoteWithRecords,
                        new StringValue {Value = NodeManager.AccountManager.GetPublicKey(key)});
                result.ObtainedActiveVotingRecords.Select(o => o.VoteId).ShouldContain(voteId);
                i++;
            }

            Logger.Info($"{term.TermNumber}, {fee}");
        }
        
         [TestMethod]
        public void One_Vote_One_Candidates_ForBP()
        {
            var amount = 1000_00000000;
            long fee = 0;
            var lockTime = 60;
            var i = 1;
            var term = Behaviors.ConsensusService.GetCurrentTermInformation();
            var voter = Voter.First();
            var candidate = FullNodeAddress[0];
            var transfer = Behaviors.TokenService.TransferBalance(InitAccount, voter, 20000_00000000);
                var transferFee = transfer.GetDefaultTransactionFee();
                fee = transferFee;
                
            
                var voteResult = Behaviors.UserVote(voter, candidate, lockTime * i, amount);
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
                            $"{voteRecord.Amount}\n" +
                            $"time: {lockTime * i}");
                voteResult.ShouldNotBeNull();
                voteResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
                var result =
                    Behaviors.ElectionService.CallViewMethod<CandidateVote>(ElectionMethod.GetCandidateVoteWithRecords,
                        new StringValue {Value = NodeManager.AccountManager.GetPublicKey(candidate)});
                result.ObtainedActiveVotingRecords.Select(o => o.VoteId).ShouldContain(voteId);
                Logger.Info($"{result.ObtainedActiveVotedVotesAmount}");
                Logger.Info($"{term.TermNumber}, {fee}");
        }

        [TestMethod]
        [DataRow("9e40e8a0491d7a2aa3332b1c3410861c0e87e101e44d5a09ae10758e0e34bc54")]
        public void ChangeUserVote(string hash)
        {
            var term = Behaviors.ConsensusService.GetCurrentTermInformation();
            Logger.Info($"Term: {term}");
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

            long fee = 0;
//            Get user profit amount
            foreach (var voter in Voter)
            {
                var profitMap = profit.GetProfitsMap(voter, schemeId);
                if (profitMap.Equals(new ReceivedProfitsMap()))
                    continue;
                var profitAmount = profitMap.Value["ELF"];
                var beforeBalance = Behaviors.TokenService.GetUserBalance(voter);
                Logger.Info($"{term.TermNumber} Profit amount: user {voter} profit amount is {profitAmount},balance {beforeBalance}");
                var newProfit = profit.GetNewTester(voter);
                var profitResult = newProfit.ExecuteMethodWithResult(ProfitMethod.ClaimProfits, new ClaimProfitsInput
                {
                    SchemeId = schemeId,
                    Beneficiary = voter.ConvertAddress()
                });
                profitResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
                var sizeFee = profitResult.GetDefaultTransactionFee();
                fee += sizeFee;
                var afterBalance = Behaviors.TokenService.GetUserBalance(voter);
                afterBalance.ShouldBe(beforeBalance + profitAmount - sizeFee);
                
                var claimProfit = profitResult.Logs.Where(l => l.Name.Contains(nameof(ProfitsClaimed))).ToList();
                foreach (var cf in claimProfit)
                {
                    var info = ProfitsClaimed.Parser.ParseFrom(ByteString.FromBase64(cf.NonIndexed));
                    Logger.Info($"{info.Period}: {info.Amount}");
                }
                
                var afterProfitAmount = profit.GetProfitsMap(voter, schemeId);
//                afterProfitAmount.Equals(new ReceivedProfitsMap()).ShouldBeTrue();
                Logger.Info($"{term.TermNumber} after profit amount is {afterProfitAmount},user balance {afterBalance}");
            }

            Logger.Info(fee);
        }

        [TestMethod]
        public void CheckProfitVoters()
        {
            var profit = Behaviors.ProfitService;
            var schemeId = Behaviors.Schemes[SchemeType.CitizenWelfare].SchemeId;
            var term = Behaviors.ConsensusService.GetCurrentTermInformation();
            var symbol = "ELF";
            long profitAmount = 0;

            foreach (var voter in Voter)
            {
                var profitMap = profit.GetProfitsMap(voter, schemeId);
                if (profitMap.Equals(new ReceivedProfitsMap()))
                    continue;
                var profitAmountFull = profitMap.Value[symbol];
                Logger.Info($"{term.TermNumber}  Profit {symbol} amount: voter {voter} CitizenWelfare profit amount is {profitAmountFull}");
                profitAmount += profitAmountFull;
            }
            Logger.Info($"{term.TermNumber}  CitizenWelfare {profitAmount};");
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
            int i = 0;
            var term = Behaviors.ConsensusService.GetCurrentTermInformation();
            Logger.Info(term.TermNumber);
            foreach (var candidate in FullNodeAddress)
            {
                i++;
//                var account = Address.FromPublicKey(candidate);
                var account = candidate.ConvertAddress();
                
                var profitMap = profit.GetProfitsMap(account.ToBase58(), BackupSubsidy);
                if (profitMap.Equals(new ReceivedProfitsMap()))
                    Logger.Info($"{i}: Profit amount: user {account} profit amount is {profitMap}");
                else
                {
                    var profitAmountFull = profitMap.Value["ELF"];
                    Logger.Info($"{i}: Profit amount: user {account} profit amount is {profitAmountFull}");
                    
                    var beforeBalance = Behaviors.TokenService.GetUserBalance(account.ToBase58());
                    if (beforeBalance <= 100000000)
                    {
                        Behaviors.TransferToken(InitAccount, account.ToBase58(), 10_00000000);
                        beforeBalance = Behaviors.TokenService.GetUserBalance(account.ToBase58());
                    }

                    var newProfit = profit.GetNewTester(account.ToBase58());
                    var profitResult = newProfit.ExecuteMethodWithResult(ProfitMethod.ClaimProfits, new ClaimProfitsInput
                    {
                        SchemeId = BackupSubsidy,
                        Beneficiary = account
                    });
                    profitResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
                    var fee = profitResult.GetDefaultTransactionFee();
                    var afterBalance = Behaviors.TokenService.GetUserBalance(account.ToBase58());
                    var claimProfit = profitResult.Logs.Where(l => l.Name.Contains(nameof(ProfitsClaimed))).ToList();
                    foreach (var cf in claimProfit)
                    {
                        var info = ProfitsClaimed.Parser.ParseFrom(ByteString.FromBase64(cf.NonIndexed));
                        Logger.Info($"{info.Period}: {info.Amount}");
                    }
                    afterBalance.ShouldBe(beforeBalance + profitAmountFull - fee);
                }
            }
        }

        [TestMethod]
        public void Withdraw()
        {
            var account = "7BSmhiLtVqHSUVGuYdYbsfaZUGpkL2ingvCmVPx66UR5L5Lbs";
            var voteId = "2888302e588dd3506aca811d0b81f686feb12e3a7218578d62fd45fbac36edae";
            var amount = 100000000000;
            var votesInfo = Behaviors.GetVotesInformation(account);
            var voteIds = votesInfo.ActiveVotingRecordIds;
            var beforeVoteBalance = Behaviors.TokenService.GetUserBalance(account, "VOTE");
            var beforeShareBalance = Behaviors.TokenService.GetUserBalance(account, "SHARE");
            beforeShareBalance.ShouldBe(beforeVoteBalance);

            var beforeElfBalance = Behaviors.TokenService.GetUserBalance(account);
            Behaviors.ElectionService.SetAccount(account);
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
            afterElfBalance.ShouldBe(beforeElfBalance +  amount - fee);
            Logger.Info($"{afterVoteBalance},{afterShareBalance},{afterElfBalance}");
            var schemeId = Behaviors.Schemes[SchemeType.CitizenWelfare].SchemeId;
            var profit = Behaviors.ProfitService;
            var voteProfit =
                profit.GetProfitDetails(account, schemeId);
            Logger.Info($"20% user vote profit for account: {account}.\r\nDetails number: {voteProfit.Details}");
        }
        
        
        [TestMethod]
        public void WithdrawAllVote()
        {
            Voter.Add(InitAccount);
            foreach (var voter in Voter)
            {
                var voteInfo = Behaviors.GetElectorVoteWithAllRecords(voter);
                Logger.Info(voteInfo);
                var allVoteId = voteInfo.ActiveVotingRecordIds;
                var allVotedRecords = voteInfo.ActiveVotingRecords;
                if (allVotedRecords.Equals(new RepeatedField<ElectionVotingRecord>()))
                    continue;
                Behaviors.ElectionService.SetAccount(voter);
                foreach (var voteId in allVoteId)
                {
                    var electionVotingRecord = allVotedRecords.Where(l => l.VoteId.Equals(voteId)).ToList().First();
                    var amount = electionVotingRecord.Amount;
                    var result =
                        Behaviors.UserWithdraw(voter,voteId.ToHex(),amount);
                    result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
                }
            }
        }
        

        [TestMethod]
        public void Vote_All_Candidates_ForBP()
        {
            foreach (var full in FullNodeAddress.Take(4))
            {
                var voteResult = Behaviors.UserVote(InitAccount, full, 100, 2000_000000000);

                voteResult.ShouldNotBeNull();
                voteResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            }
        }

        [TestMethod]
        public void GetVotersVote()
        {
            foreach (var voter in Voter)
            {
                var voteInfo = Behaviors.GetElectorVoteWithAllRecords(voter);
                Logger.Info(voteInfo);
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
        public void CheckShare()
        {
            var schemeId = Behaviors.Schemes[SchemeType.CitizenWelfare].SchemeId;
            foreach (var voter in Voter)
            {
                var info = Behaviors.GetVotesInformation(voter);
                Logger.Info(info);
                var profitInfo = Behaviors.ProfitService.GetProfitDetails(voter,schemeId);
                Logger.Info(profitInfo);
            }
        }

        [TestMethod]
        public void VoteWeight()
        {
            var time = 90;
            var amount = 1000_00000000;
            var profit = Behaviors.ProfitService;

            var schemeId = Behaviors.Schemes[SchemeType.CitizenWelfare].SchemeId;
            var schemeInfo = profit.GetScheme(schemeId);
            Logger.Info(schemeInfo.TotalShares);
                
            for (int i = 1; i < 7; i++)
            {
                long lockTime = time * i * 86400;
                var voteWeight =
                    Behaviors.ElectionService.CallViewMethod<Int64Value>(ElectionMethod.GetCalculateVoteWeight,
                        new VoteInformation
                        {
                            Amount = amount,
                            LockTime = lockTime
                        });
                Logger.Info($"Amount:{amount}; LockTime {lockTime * i}: {voteWeight.Value}");
            }
        }
    }
}