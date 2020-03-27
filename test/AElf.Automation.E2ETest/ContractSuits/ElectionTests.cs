using System;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.Election;
using AElf.Contracts.MultiToken;
using AElf.Types;
using AElfChain.Common;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using Google.Protobuf.WellKnownTypes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace AElf.Automation.E2ETest.ContractSuits
{
    [TestClass]
    public class ElectionTests : ContractTestBase
    {
        [TestMethod]
        public async Task GetTermSnapshot_Test()
        {
            var termNumber = await ContractManager.ConsensusStub.GetCurrentTermNumber.CallAsync(new Empty());
            if (termNumber.Value == 1) return;
            var snapshot = await ContractManager.ElectionStub.GetTermSnapshot.CallAsync(new GetTermSnapshotInput
            {
                TermNumber = termNumber.Value - 1
            });
            snapshot.MinedBlocks.ShouldBeGreaterThan(1);
            snapshot.ElectionResult.Keys.Count.ShouldBeGreaterThanOrEqualTo(0);
            snapshot.EndRoundNumber.ShouldBeGreaterThan(1);
        }

        [TestMethod]
        public async Task GetMinersCount_Test()
        {
            var minersCount = await ContractManager.ElectionStub.GetMinersCount.CallAsync(new Empty());
            minersCount.Value.ShouldBeGreaterThanOrEqualTo(1);
        }

        [TestMethod]
        public async Task GetElectionResult_Test()
        {
            var electionResult = await ContractManager.ElectionStub.GetElectionResult.CallAsync(
                new GetElectionResultInput
                {
                    TermNumber = 1
                });
            electionResult.TermNumber.ShouldBe(1);
            electionResult.Results.Count.ShouldBe(0);
        }

        [TestMethod]
        public async Task Election_Test()
        {
            const long electionDeposit = 10_0000_00000000L;
            var nodeAccounts = NodeInfoHelper.Config.Nodes.Select(o => o.Account).ToList();
            var candidates = await ContractManager.ElectionStub.GetCandidates.CallAsync(new Empty());
            var candidatesPubkey = candidates.Value.Select(o => o.ToByteArray().ToHex()).ToList();
            //announcement
            if (candidatesPubkey.Count == 0)
            {
                //announce election
                var miners = await ContractManager.ConsensusStub.GetCurrentMinerList.CallAsync(new Empty());
                var minersAccount = miners.Pubkeys.Select(o => Address.FromPublicKey(o.ToByteArray()).GetFormatted())
                    .ToList();
                foreach (var account in nodeAccounts)
                {
                    if (minersAccount.Contains(account)) continue;
                    var balance = ContractManager.Token.GetUserBalance(account);
                    if (balance < electionDeposit)
                    {
                        var transactionResult = await ContractManager.TokenStub.Transfer.SendAsync(new TransferInput
                        {
                            To = account.ConvertAddress(),
                            Amount = electionDeposit + 5_00000000L,
                            Symbol = "ELF",
                            Memo = "transfer election deposit"
                        });
                        transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
                    }

                    var beforeBalance = ContractManager.Token.GetUserBalance(account);

                    var electionStub = ContractManager.Genesis.GetElectionStub(account);
                    var announceResult = await electionStub.AnnounceElection.SendAsync(new Empty());
                    announceResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
                    var transactionFee = announceResult.TransactionResult.GetDefaultTransactionFee();
                    var afterBalance = ContractManager.Token.GetUserBalance(account);
                    beforeBalance.ShouldBe(afterBalance + electionDeposit + transactionFee);

                    candidates = await ContractManager.ElectionStub.GetCandidates.CallAsync(new Empty());
                    candidatesPubkey = candidates.Value.Select(o => o.ToByteArray().ToHex()).ToList();
                    var candidateAccounts =
                        candidates.Value.Select(o => Address.FromPublicKey(o.ToByteArray()).GetFormatted());
                    candidateAccounts.ShouldContain(account);
                    break;
                }
            }

            //prepare tester and vote balance
            string tester;
            string testerPubkey;
            while (true)
            {
                tester = ContractManager.NodeManager.GetRandomAccount();
                if (nodeAccounts.Contains(tester)) continue;
                testerPubkey = ContractManager.NodeManager.GetAccountPublicKey(tester);
                var transferResult = await ContractManager.TokenStub.Transfer.SendAsync(new TransferInput
                {
                    To = tester.ConvertAddress(),
                    Symbol = "ELF",
                    Amount = 100_00000000,
                    Memo = "Vote test"
                });
                transferResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
                break;
            }

            //Vote
            var electionTester = ContractManager.Genesis.GetElectionStub(tester);
            var voteResult = await electionTester.Vote.SendAsync(new VoteMinerInput
            {
                CandidatePubkey = candidatesPubkey.First(),
                Amount = 50,
                EndTimestamp = DateTime.UtcNow.Add(TimeSpan.FromDays(120)).ToTimestamp()
            });
            voteResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var voteId = voteResult.TransactionResult.TransactionId;

            //GetElectorVote
            var electorResult = await electionTester.GetElectorVote.CallAsync(new StringValue
            {
                Value = testerPubkey
            });
            electorResult.ActiveVotingRecordIds.ShouldContain(voteId);
            electorResult.ActiveVotedVotesAmount.ShouldBeGreaterThanOrEqualTo(50);
            electorResult.AllVotedVotesAmount.ShouldBeGreaterThanOrEqualTo(50);
            electorResult.Pubkey.ToByteArray().ToHex().ShouldBe(testerPubkey);

            //GetElectorVoteWithRecords
            var voteWithRecords = await electionTester.GetElectorVoteWithRecords.CallAsync(new StringValue
            {
                Value = testerPubkey
            });
            voteWithRecords.ActiveVotingRecordIds.ShouldContain(voteId);
            voteWithRecords.ActiveVotedVotesAmount.ShouldBeGreaterThanOrEqualTo(50);
            voteWithRecords.AllVotedVotesAmount.ShouldBeGreaterThanOrEqualTo(50);
            voteWithRecords.ActiveVotingRecords.Select(o => o.Candidate).ShouldContain(candidatesPubkey.First());
            electorResult.Pubkey.ToByteArray().ToHex().ShouldBe(testerPubkey);

            //GetElectorVoteWithAllRecords
            var voteWithAllRecords = await electionTester.GetElectorVoteWithAllRecords.CallAsync(new StringValue
            {
                Value = testerPubkey
            });
            voteWithAllRecords.ActiveVotingRecordIds.ShouldContain(voteId);
            voteWithRecords.ActiveVotingRecords.Select(o => o.Candidate).ShouldContain(candidatesPubkey.First());
            voteWithAllRecords.ActiveVotedVotesAmount.ShouldBeGreaterThanOrEqualTo(50);
            voteWithAllRecords.AllVotedVotesAmount.ShouldBeGreaterThanOrEqualTo(50);
            voteWithAllRecords.Pubkey.ToByteArray().ToHex().ShouldBe(testerPubkey);

            //GetCandidateVote
            var candidateVote = await electionTester.GetCandidateVote.CallAsync(new StringValue
            {
                Value = candidatesPubkey.First()
            });
            candidateVote.AllObtainedVotedVotesAmount.ShouldBeGreaterThanOrEqualTo(50);
            candidateVote.ObtainedActiveVotingRecordIds.ShouldContain(voteId);
            candidateVote.Pubkey.ToByteArray().ToHex().ShouldBe(candidatesPubkey.First());

            //GetCandidateVoteWithRecords
            var candidateVoteWithRecords = await electionTester.GetCandidateVoteWithRecords.CallAsync(new StringValue
            {
                Value = candidatesPubkey.First()
            });
            candidateVoteWithRecords.AllObtainedVotedVotesAmount.ShouldBeGreaterThanOrEqualTo(50);
            candidateVoteWithRecords.ObtainedActiveVotingRecordIds.ShouldContain(voteId);
            candidateVoteWithRecords.ObtainedActiveVotingRecords.Select(o => o.Candidate)
                .ShouldAllBe(o => o == candidatesPubkey.First());
            candidateVoteWithRecords.Pubkey.ToByteArray().ToHex().ShouldBe(candidatesPubkey.First());

            //GetCandidateVoteWithAllRecords
            var candidateVoteWithAllRecords =
                await electionTester.GetCandidateVoteWithAllRecords.CallAsync(new StringValue
                {
                    Value = candidatesPubkey.First()
                });
            candidateVoteWithAllRecords.ObtainedActiveVotingRecords.Select(o => o.Voter)
                .ShouldContain(tester.ConvertAddress());
            candidateVoteWithAllRecords.ObtainedActiveVotingRecordIds.ShouldContain(voteId);
            candidateVoteWithRecords.ObtainedActiveVotingRecords.Select(o => o.Candidate)
                .ShouldAllBe(o => o == candidatesPubkey.First());
            candidateVoteWithAllRecords.AllObtainedVotedVotesAmount.ShouldBeGreaterThanOrEqualTo(50);
            candidateVoteWithAllRecords.Pubkey.ToByteArray().ToHex().ShouldBe(candidatesPubkey.First());

            //GetVotersCount
            var votersCount = await electionTester.GetVotersCount.CallAsync(new Empty());
            votersCount.Value.ShouldBeGreaterThanOrEqualTo(1);

            //GetVotesAmount
            var votesAmount = await electionTester.GetVotesAmount.CallAsync(new Empty());
            votesAmount.Value.ShouldBeGreaterThanOrEqualTo(50);
        }

        [TestMethod]
        public async Task SetVoteInterest_Test()
        {
            var voteWeightInterestList = new VoteWeightInterestList
            {
                VoteWeightInterestInfos =
                {
                    new VoteWeightInterest
                    {
                        Day = 90,
                        Capital = 10000,
                        Interest = 10
                    },
                    new VoteWeightInterest
                    {
                        Day = 180,
                        Capital = 10000,
                        Interest = 12
                    },
                    new VoteWeightInterest
                    {
                        Day = 270,
                        Capital = 10000,
                        Interest = 15
                    },
                    new VoteWeightInterest
                    {
                        Day = 360,
                        Capital = 10000,
                        Interest = 18
                    },
                    new VoteWeightInterest
                    {
                        Day = 720,
                        Capital = 10000,
                        Interest = 20
                    },
                    new VoteWeightInterest
                    {
                        Day = 1080,
                        Capital = 10000,
                        Interest = 24
                    }
                }
            };
            var transactionResult = ContractManager.Authority.ExecuteTransactionWithAuthority(
                ContractManager.Election.ContractAddress,
                nameof(ContractManager.ElectionStub.SetVoteWeightInterest),
                voteWeightInterestList, ContractManager.CallAddress);
            transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            //Query interest
            var queryResult = await ContractManager.ElectionStub.GetVoteWeightSetting.CallAsync(new Empty());
            queryResult.VoteWeightInterestInfos.ShouldBe(voteWeightInterestList.VoteWeightInterestInfos);
        }
    }
}