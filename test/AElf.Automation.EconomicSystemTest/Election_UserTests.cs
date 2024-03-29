using System;
using System.Collections.Generic;
using System.Linq;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.Election;
using AElf.Contracts.Profit;
using AElf.Contracts.Vote;
using AElf.CSharp.Core;
using AElf.CSharp.Core.Extension;
using AElf.Types;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using Google.Protobuf;
using Google.Protobuf.Collections;
using Google.Protobuf.WellKnownTypes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using Volo.Abp.Threading;

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

        [TestMethod]
        public void Vote_One_Candidates_ForBP()
        {
            var amount = 2000_00000000;
            long fee = 0;
            var lockTime = 91;
            var i = 1;
            var term = Behaviors.ConsensusService.GetCurrentTermInformation();
            var voterInfo = new Dictionary<string, string>();
            foreach (var voter in Voter)
            {
                var transfer = Behaviors.TokenService.TransferBalance(InitAccount, voter, 20000_00000000);
                var transferFee = transfer.GetDefaultTransactionFee();
                fee += transferFee;
            }

            for (int j = 0; j < FullNodeAddress.Count; j++)
            {
                var k = j;
                while (k > Voter.Count - 1)
                {
                    k = k - Voter.Count;
                }

                voterInfo.Add(FullNodeAddress[j], Voter[k]);
            }
            //
            // for (int j = 0; j < Voter.Count; j++)
            //     voterInfo.Add(FullNodeAddress[j], Voter[j]);

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
        [DataRow(0, 200, 0)]
        [DataRow(1, 200, 1)]
        public void One_Vote_One_Candidates_ForBP(int voterIndex, int i, int candidatesIndex)
        {
            var amount = 1_00000000 * candidatesIndex;
            long fee = 0;
            var lockTime = 60;
            var term = Behaviors.ConsensusService.GetCurrentTermInformation();
            var voter = Voter[voterIndex];
            var candidate = FullNodeAddress[candidatesIndex];
            var schemeId = Behaviors.Schemes[SchemeType.CitizenWelfare].SchemeId;

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

            var votesInformation = Behaviors.GetVotesInformation(voter);
            Logger.Info(votesInformation.ActiveVotingRecords.First(a => a.VoteId.Equals(voteId)));
            var voteProfit =
                Behaviors.ProfitService.GetProfitDetails(voter, schemeId);
            Logger.Info(voteProfit.Details);

            var virtualAddress = CommonHelper.GetVirtualAddress(Behaviors.ElectionService.Contract,
                voter.ConvertAddress(), Behaviors.TokenService.Contract, voteId);
            var lockBalance = Behaviors.TokenService.GetUserBalance(virtualAddress.ToBase58());
            Logger.Info(lockBalance);
            lockBalance.ShouldBe(amount);
        }
        
        [TestMethod]
        public void One_Vote_One_Candidate_ForBP()
        {
            var amount = 1_00000000;
            var lockTime = 1;
            var term = Behaviors.ConsensusService.GetCurrentTermInformation();
            var voter = Voter[0];
            var candidate = "2UPL7d6qG878cEpKhdDfJ5phT3D2v5rybrcpguL8uFAMArVPzP";
            var schemeId = Behaviors.Schemes[SchemeType.CitizenWelfare].SchemeId;

            var transfer = Behaviors.TokenService.TransferBalance(InitAccount, voter, 20000_00000000);
            var transferFee = transfer.GetDefaultTransactionFee();
            var fee = transferFee;
            
            var voteResult = Behaviors.UserVote(voter, candidate, lockTime, amount);
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
                        $"time: {lockTime}");
            voteResult.ShouldNotBeNull();
            voteResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var result =
                Behaviors.ElectionService.CallViewMethod<CandidateVote>(ElectionMethod.GetCandidateVoteWithRecords,
                    new StringValue {Value = NodeManager.AccountManager.GetPublicKey(candidate)});
            result.ObtainedActiveVotingRecords.Select(o => o.VoteId).ShouldContain(voteId);
            Logger.Info($"{result.ObtainedActiveVotedVotesAmount}");
            Logger.Info($"{term.TermNumber}, {fee}");

            var votesInformation = Behaviors.GetVotesInformation(voter);
            Logger.Info(votesInformation.ActiveVotingRecords.First(a => a.VoteId.Equals(voteId)));
            var voteProfit =
                Behaviors.ProfitService.GetProfitDetails(voter, schemeId);
            Logger.Info(voteProfit.Details);

            var virtualAddress = CommonHelper.GetVirtualAddress(Behaviors.ElectionService.Contract,
                voter.ConvertAddress(), Behaviors.TokenService.Contract, voteId);
            var lockBalance = Behaviors.TokenService.GetUserBalance(virtualAddress.ToBase58());
            Logger.Info(lockBalance);
            lockBalance.ShouldBe(amount);
        }

        [TestMethod]
        public void CheckVirtualAddressBalance()
        {
            var voteId = Hash.LoadFromHex("4713eec58839f7464a8bb7637815a2a106d6111ff99fd52316276b14a11ce1e5");
            var voter = Voter.Last();

            var virtualAddress = CommonHelper.GetVirtualAddress(Behaviors.ElectionService.Contract,
                voter.ConvertAddress(), Behaviors.TokenService.Contract, voteId);
            var lockBalance = Behaviors.TokenService.GetUserBalance(virtualAddress.ToBase58());
            Logger.Info(lockBalance);
        }

        [TestMethod]
        [DataRow("de00c30097f56a9391609c64c0190553d083fdda236d731b1f3e1347dc89f92c", 0,1)]
        public void ChangeUserVoteOld(string hash, int voteIndex, int candidateIndex)
        {
            var schemeId = Behaviors.Schemes[SchemeType.CitizenWelfare].SchemeId;
            // var term = Behaviors.ConsensusService.GetCurrentTermInformation();
            // Logger.Info($"Term: {term}");
            var account = Voter[voteIndex];
            var candidate = FullNodeAddress[candidateIndex];
            var voteId = Hash.LoadFromHex(hash);
            var originVoteProfit =
                Behaviors.ProfitService.GetProfitDetails(account, schemeId);
            var originVoteRecord = Behaviors.GetVotesInformation(account);
            var currentOriginVoteRecord = originVoteRecord.ActiveVotingRecords.First(a => a.VoteId.Equals(voteId));
            Logger.Info(originVoteRecord.ActiveVotingRecords);
            var virtualAddress = CommonHelper.GetVirtualAddress(Behaviors.ElectionService.Contract,
                account.ConvertAddress(), Behaviors.TokenService.Contract, voteId);
            var originVirtualBalance = Behaviors.TokenService.GetUserBalance(virtualAddress.ToBase58());

            var transactionResult = Behaviors.UserChangeVoteOld(account, candidate, voteId);
            transactionResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var voteResult = Behaviors.ElectionService.CallViewMethod<CandidateVote>(
                ElectionMethod.GetCandidateVoteWithRecords,
                new StringValue {Value = NodeManager.AccountManager.GetPublicKey(candidate)});
            voteResult.ObtainedActiveVotingRecords.Select(o => o.VoteId).ShouldContain(voteId);

            var txBlock = transactionResult.BlockNumber;
            var txTime = AsyncHelper.RunSync(() => Behaviors.NodeManager.ApiClient.GetBlockByHeightAsync(txBlock))
                .Header.Time;
            Logger.Info(txTime);

            var voteRecord = Behaviors.GetVotesInformation(account);
            var currentVoteRecord = voteRecord.ActiveVotingRecords.First(a => a.VoteId.Equals(voteId));
            Logger.Info(voteRecord.ActiveVotingRecords);

            var voteProfit =
                Behaviors.ProfitService.GetProfitDetails(account, schemeId);
            Logger.Info(voteProfit.Details);

            var virtualBalance = Behaviors.TokenService.GetUserBalance(virtualAddress.ToBase58());
            originVirtualBalance.ShouldBe(virtualBalance);
        }
        
        [TestMethod]
        [DataRow("de00c30097f56a9391609c64c0190553d083fdda236d731b1f3e1347dc89f92c", true, 0, 0)]
        public void ChangeUserVote(string hash, bool isReset, int voteIndex, int candidateIndex)
        {
            var schemeId = Behaviors.Schemes[SchemeType.CitizenWelfare].SchemeId;
            // var term = Behaviors.ConsensusService.GetCurrentTermInformation();
            // Logger.Info($"Term: {term}");
            var account = Voter[voteIndex];
            var candidate = FullNodeAddress[candidateIndex];
            var voteId = Hash.LoadFromHex(hash);
            var originVoteProfit =
                Behaviors.ProfitService.GetProfitDetails(account, schemeId);
            Logger.Info(originVoteProfit);
            var originVoteRecord = Behaviors.GetVotesInformation(account);
            var currentOriginVoteRecord = originVoteRecord.ActiveVotingRecords.First(a => a.VoteId.Equals(voteId));
            Logger.Info(originVoteRecord.ActiveVotingRecords);
            var virtualAddress = CommonHelper.GetVirtualAddress(Behaviors.ElectionService.Contract,
                account.ConvertAddress(), Behaviors.TokenService.Contract, voteId);
            var originVirtualBalance = Behaviors.TokenService.GetUserBalance(virtualAddress.ToBase58());

            var transactionResult = Behaviors.UserChangeVote(account, candidate, voteId, isReset);
            transactionResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var voteResult = Behaviors.ElectionService.CallViewMethod<CandidateVote>(
                ElectionMethod.GetCandidateVoteWithRecords,
                new StringValue {Value = NodeManager.AccountManager.GetPublicKey(candidate)});
            voteResult.ObtainedActiveVotingRecords.Select(o => o.VoteId).ShouldContain(voteId);

            var txBlock = transactionResult.BlockNumber;
            var txTime = AsyncHelper.RunSync(() => Behaviors.NodeManager.ApiClient.GetBlockByHeightAsync(txBlock))
                .Header.Time;
            Logger.Info(txTime);

            var voteRecord = Behaviors.GetVotesInformation(account);
            var currentVoteRecord = voteRecord.ActiveVotingRecords.First(a => a.VoteId.Equals(voteId));
            Logger.Info(voteRecord.ActiveVotingRecords);

            var voteProfit =
                Behaviors.ProfitService.GetProfitDetails(account, schemeId);
            Logger.Info(voteProfit.Details);
            if (isReset)
            {
                originVoteProfit.Details.Count.ShouldBe(voteProfit.Details.Count);
                currentVoteRecord.VoteTimestamp.ShouldBe(txTime.ToTimestamp());
                currentVoteRecord.UnlockTimestamp.ShouldBe(txTime
                    .Add(TimeSpan.FromSeconds(currentVoteRecord.LockTime)).ToTimestamp());
            }
            else
            {
                originVoteProfit.Details.ShouldBe(voteProfit.Details);
                currentVoteRecord.VoteTimestamp.ShouldBe(txTime.ToTimestamp());
                currentVoteRecord.UnlockTimestamp.ShouldBe(
                    currentVoteRecord.VoteTimestamp.AddSeconds(
                        currentVoteRecord.LockTime));
            }

            var virtualBalance = Behaviors.TokenService.GetUserBalance(virtualAddress.ToBase58());
            originVirtualBalance.ShouldBe(virtualBalance);
        }
        
        [TestMethod]
        public void CheckProfit()
        {
            var profit = Behaviors.ProfitService;
            var schemeId = Behaviors.Schemes[SchemeType.CitizenWelfare].SchemeId;
            var term = Behaviors.ConsensusService.GetCurrentTermInformation();
            long fee = 0;
            var voteIds = new List<Hash>();
//            Get user profit amount
            foreach (var voter in Voter)
            {
                var voteProfit =
                    profit.GetProfitDetails(voter, schemeId);
                if (voteProfit.Equals(new ProfitDetails())) continue;
                Logger.Info($"20% user vote profit for account: {voter}.\r\nDetails number: {voteProfit.Details}");
                var profitMap = profit.GetProfitsMap(voter, schemeId);
                if (profitMap.Equals(new ReceivedProfitsMap()))
                    continue;
                var profitAmount = profitMap.Value["ELF"];
                var beforeBalance = Behaviors.TokenService.GetUserBalance(voter);
                Logger.Info(
                    $"{term.TermNumber} Profit amount: user {voter} profit amount is {profitAmount},balance {beforeBalance}");
                var voteInfo = Behaviors.GetElectorVoteWithAllRecords(voter); 
                voteIds.AddRange(voteInfo.ActiveVotingRecordIds);
                voteIds.AddRange(voteInfo.WithdrawnVotingRecordIds);

                var newProfit = profit.GetNewTester(voter);
                var profitResult = newProfit.ExecuteMethodWithResult(ProfitMethod.ClaimProfits, new ClaimProfitsInput
                {
                    SchemeId = schemeId,
                    Beneficiary = voter.ConvertAddress()
                });
                profitResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
                var sizeFee = profitResult.GetDefaultTransactionFee();
                fee += sizeFee;

                var claimProfit = profitResult.Logs.Where(l => l.Name.Contains(nameof(ProfitsClaimed))).ToList();
                foreach (var cf in claimProfit)
                {
                    var info = ProfitsClaimed.Parser.ParseFrom(ByteString.FromBase64(cf.NonIndexed));
                    Logger.Info($"{info.Period}: {info.Amount}");
                    var currentPeriodAddress = CommonHelper.GeneratePeriodVirtualAddressFromHash(schemeId, info.Period);
                    var currentPeriodBalance = Behaviors.TokenService.GetUserBalance(currentPeriodAddress.ToBase58());
                    Logger.Info(currentPeriodBalance);
                }

                var afterBalance = Behaviors.TokenService.GetUserBalance(voter);
                afterBalance.ShouldBe(beforeBalance + profitAmount - sizeFee);
                var afterProfitAmount = profit.GetProfitsMap(voter, schemeId);
//                afterProfitAmount.Equals(new ReceivedProfitsMap()).ShouldBeTrue();
                Logger.Info(
                    $"{term.TermNumber} after profit amount is {afterProfitAmount},user balance {afterBalance}");
                var afterVoteProfit = profit.GetProfitDetails(voter, schemeId);
                Logger.Info(afterVoteProfit);
            }

            Logger.Info(fee);
        }

        [TestMethod]
        public void CheckProfitVoters()
        {
            var profit = Behaviors.ProfitService;
            var schemeId = Behaviors.Schemes[SchemeType.CitizenWelfare].SchemeId;
            var flexibleReward = Behaviors.Schemes[SchemeType.FlexibleReward].SchemeId;
            var term = Behaviors.ConsensusService.GetCurrentTermInformation();
            var symbol = "ELF";
            long profitAmount = 0;
            long sumFlexible = 0;
            foreach (var voter in Voter)
            {
                var voteProfit =
                    profit.GetProfitDetails(voter, schemeId);
                if (voteProfit.Equals(new ProfitDetails())) continue;
                Logger.Info($"Details: {voteProfit.Details}");
                
                var profitMap = profit.GetProfitsMap(voter, schemeId);
                if (profitMap.Equals(new ReceivedProfitsMap()))
                    continue;
                var profitAmountFull = profitMap.Value[symbol];
                Logger.Info(
                    $"{term.TermNumber}  Profit {symbol} amount: voter {voter} CitizenWelfare profit amount is {profitAmountFull}");
                profitAmount += profitAmountFull;

                var flexibleRewardMap = profit.GetProfitsMap(voter, flexibleReward);
                if (!flexibleRewardMap.Equals(new ReceivedProfitsMap()))
                {
                    var flexibleRewardAmount = flexibleRewardMap.Value[symbol];
                    sumFlexible += flexibleRewardAmount;
                    Logger.Info(
                        $"FlexibleReward amount: user {voter} profit {symbol} amount is {flexibleRewardAmount}");
                }

                var profitDetail = profit.GetProfitDetails(voter, schemeId);
                Logger.Info(profitDetail);
            }

            var currentProfit = profit.GetDistributedProfitsInfo(term.TermNumber.Sub(1), schemeId);
            var elfProfit = currentProfit.AmountsMap.First(l => l.Key.Equals("ELF"));
            var amount = elfProfit.Value;
            var isReleased = currentProfit.IsReleased;
            
            Logger.Info($"{term.TermNumber}  " +
                        $"CitizenWelfare {profitAmount}; " +
                        $"Flexible {sumFlexible}; " +
                        $"DistributeProfit: {amount}; " +
                        $"IsReleased: {isReleased}");
        }

        [TestMethod]
        [DataRow("d69dfa54a1270a00dd9e23a6a519f3427e6a0f549fc2f3eac7c0a7401b3a5920", 3, 1)]
        public void Withdraw(string voteId, int voterIndex, int candidateIndex)
        {
            var account = Voter[voterIndex];
            var candidate = FullNodeAddress[candidateIndex];
            var schemeId = Behaviors.Schemes[SchemeType.CitizenWelfare].SchemeId;

            var allShares = Behaviors.ProfitService.GetScheme(schemeId).TotalShares;
            var votesInfo = Behaviors.GetVotesInformation(account);
            var voteIds = votesInfo.ActiveVotingRecordIds;
            var getCandidateVoteWithRecords =
                Behaviors.ElectionService.CallViewMethod<CandidateVote>(ElectionMethod.GetCandidateVoteWithRecords,
                    new StringValue {Value = NodeManager.AccountManager.GetPublicKey(candidate)});
            getCandidateVoteWithRecords.ObtainedActiveVotingRecords.Select(o => o.VoteId).ShouldContain(Hash.LoadFromHex(voteId));
            Logger.Info(getCandidateVoteWithRecords);
            var amount = getCandidateVoteWithRecords.ObtainedActiveVotingRecords
                .First(l => l.VoteId.Equals(Hash.LoadFromHex(voteId))).Amount;

            var profitDetails = Behaviors.ProfitService.GetProfitDetails(account, schemeId);
            Logger.Info(profitDetails);
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
            afterElfBalance.ShouldBe(beforeElfBalance + amount - fee);
            Logger.Info($"{afterVoteBalance},{afterShareBalance},{afterElfBalance}");
            var afterVotesInfo = Behaviors.GetVotesInformation(account);
            afterVotesInfo.WithdrawnVotingRecordIds.ShouldContain(Hash.LoadFromHex(voteId));
            
            //check all shares 
            var afterAllShares = Behaviors.ProfitService.GetScheme(schemeId).TotalShares;
            Logger.Info($"Shares: {allShares}\n After shares: {afterAllShares}");

            if (profitDetails.Details.Count.Equals(0))
            {
                afterAllShares.ShouldBe(allShares);
            }
            else
            {
                afterAllShares.ShouldBe(
                    allShares.Sub(
                        votesInfo.ActiveVotingRecords.First(
                            a => a.VoteId.Equals(
                                Hash.LoadFromHex(voteId))).Weight));
            }
            var profit = Behaviors.ProfitService;
            var voteProfit =
                profit.GetProfitDetails(account, schemeId);
            Logger.Info($"20% user vote profit for account: {account}.\r\nDetails number: {voteProfit.Details}");
            
           var afterGetCandidateVoteWithRecords =
                Behaviors.ElectionService.CallViewMethod<CandidateVote>(ElectionMethod.GetCandidateVoteWithRecords,
                    new StringValue {Value = NodeManager.AccountManager.GetPublicKey(candidate)});
           Logger.Info(afterGetCandidateVoteWithRecords);

           afterGetCandidateVoteWithRecords.ObtainedActiveVotingRecords.Select(o => o.VoteId).ShouldNotContain(Hash.LoadFromHex(voteId));
           afterGetCandidateVoteWithRecords.ObtainedWithdrawnVotingRecordIds.ShouldContain(Hash.LoadFromHex(voteId));
           // afterGetCandidateVoteWithRecords.AllObtainedVotedVotesAmount.ShouldBe(getCandidateVoteWithRecords.AllObtainedVotedVotesAmount.Sub(amount));
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
                        Behaviors.UserWithdraw(voter, voteId.ToHex(), amount);
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
            var schemeId = Behaviors.Schemes[SchemeType.CitizenWelfare].SchemeId;
            foreach (var voter in Voter)
            {
                var voteInfo = Behaviors.GetElectorVoteWithAllRecords(voter);
                foreach (var activeVoting in voteInfo.ActiveVotingRecords)
                    Logger.Info($"Active Voting Record: \n{activeVoting}\n");
                foreach (var withdrawnVotes in voteInfo.WithdrawnVotesRecords)
                    Logger.Info($"Withdrawn Votes Record: \n{withdrawnVotes}\n");

                var profit = Behaviors.ProfitService.GetProfitDetails(voter, schemeId);
                var activityProfitDetails = profit.Details.Where(d => d.IsWeightRemoved.Equals(false)).ToList();
                long share = activityProfitDetails.Sum(profitDetail => profitDetail.Shares);
                Logger.Info(share);
                Logger.Info(profit.Details);
            }
            
            var allShares = Behaviors.ProfitService.GetScheme(schemeId).TotalShares;
            Logger.Info($"All share: {allShares}");

            var term = Behaviors.GetCurrentTermInformation();
            Logger.Info($"Term: {term}");
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
                var profitInfo = Behaviors.ProfitService.GetProfitDetails(voter, schemeId);
                Logger.Info(profitInfo);
            }
        }

        [TestMethod]
        public void CheckVote()
        {
            var votersCount = Behaviors.ElectionService.GetVotersCount();
            Logger.Info(votersCount);
            //GetVotesAmount
            var votesAmount =Behaviors.ElectionService.GetVotesAmount();
            Logger.Info(votesAmount);

            
            var account = Voter[2];
            var schemeId = Behaviors.Schemes[SchemeType.CitizenWelfare].SchemeId;

            var allShares = Behaviors.ProfitService.GetScheme(schemeId).TotalShares;
            var votesInfo = Behaviors.GetVotesInformation(account);
            var profitDetails = Behaviors.ProfitService.GetProfitDetails(account, schemeId);

            Logger.Info(allShares);
            Logger.Info(votesInfo);
            Logger.Info(profitDetails);
        }

        [TestMethod]
        public void VoteWeight()
        {
            var time = 10;
            var amount = 1000_00000000;
            var profit = Behaviors.ProfitService;

            var schemeId = Behaviors.Schemes[SchemeType.CitizenWelfare].SchemeId;
            var schemeInfo = profit.GetScheme(schemeId);
            Logger.Info(schemeInfo.TotalShares);

            for (int i = 1; i < 11; i++)
            {
                long lockTime = time * i * 60;
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