using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.Contracts.Vote;
using AElf.Types;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using Google.Protobuf.WellKnownTypes;
using log4net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace AElf.Automation.Contracts.ScenarioTest
{
    [TestClass]
    public class VoteContractTest
    {
        private ILog Logger { get; set; }
        private INodeManager NodeManager { get; set; }

        private string InitAccount { get; } = "28Y8JA1i2cN6oHvdv7EraXJr9a1gY6D1PpJXw9QtRMRwKcBQMK";
        private string TestAccount { get; } = "2REajHMeW2DMrTdQWn89RQ26KQPRg91coCEtPP42EC9Cj7sZ61";
        private static string RpcUrl { get; } = "192.168.197.44:8000";
        private static string Symbol { get; } = "TEST";
        private GenesisContract _genesisContract;
        private TokenContract _tokenContract;
        private VoteContract _voteContract;
        private TokenContractContainer.TokenContractStub _tokenSub;
        private VoteContractContainer.VoteContractStub _voteSub;
        private VoteContractContainer.VoteContractStub _testVoteSub;


        /// <summary>
        /// 0001-01-01T00:00:00Z
        /// </summary>
        public static Timestamp MinValue => new Timestamp {Nanos = 0, Seconds = -62135596800L};

        /// <summary>
        /// 9999-12-31T23:59:59.999999999Z
        /// </summary>
        public static Timestamp MaxValue => new Timestamp {Nanos = 999999999, Seconds = 253402300799L};

        VotingRegisterInput _votingRegisterInput = new VotingRegisterInput
        {
            IsLockToken = true,
            AcceptedCurrency = Symbol,
            TotalSnapshotNumber = long.MaxValue,
            StartTimestamp = MinValue,
            EndTimestamp = MaxValue,
        };



        [TestInitialize]
        public void Initialize()
        {
            Log4NetHelper.LogInit("VoteContract");
            Logger = Log4NetHelper.GetLogger();

            NodeManager = new NodeManager(RpcUrl);
            _genesisContract = GenesisContract.GetGenesisContract(NodeManager, InitAccount);
            _tokenContract = _genesisContract.GetTokenContract(InitAccount);
            _voteContract = _genesisContract.GetVoteContract(InitAccount);
            _tokenSub = _genesisContract.GetTokenStub(InitAccount);
            _voteSub = _genesisContract.GetVoteStub(InitAccount);
            _testVoteSub = _genesisContract.GetVoteStub(TestAccount);
        }

        [TestMethod]
        public async Task Register()
        {
            var result = await _voteSub.Register.SendAsync(_votingRegisterInput);
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var logs = result.TransactionResult.Logs.First(l => l.Name.Equals(nameof(VotingItemRegistered))).NonIndexed;
            var votingItem = VotingItemRegistered.Parser.ParseFrom(logs);
            var votingItemId = HashHelper.ConcatAndCompute(
                HashHelper.ComputeFrom(_votingRegisterInput),
                HashHelper.ComputeFrom(InitAccount.ConvertAddress()));
            votingItem.VotingItemId.ShouldBe(votingItemId);
            votingItem.Sponsor.ShouldBe(InitAccount.ConvertAddress());
            votingItem.AcceptedCurrency.ShouldBe(Symbol);
            Logger.Info(votingItem.VotingItemId.ToHex());
        }

        [TestMethod]
        public async Task AddOption()
        {
            var votingItemId = HashHelper.ConcatAndCompute(
                HashHelper.ComputeFrom(_votingRegisterInput),
                HashHelper.ComputeFrom(InitAccount.ConvertAddress()));

            var input = new AddOptionInput
            {
                VotingItemId = votingItemId,
                Option = "Vote"
            };
            var result = await _voteSub.AddOption.SendAsync(input);
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }
        
        [TestMethod]
        public async Task AddOptions()
        {
            var votingItemId = HashHelper.ConcatAndCompute(
                HashHelper.ComputeFrom(_votingRegisterInput),
                HashHelper.ComputeFrom(InitAccount.ConvertAddress()));

            var input = new AddOptionsInput
            {
                VotingItemId = votingItemId,
                Options =
                {
                    "Accept","Reject","Abstain"
                }
            };
            var result = await _voteSub.AddOptions.SendAsync(input);
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        [TestMethod]
        public async Task RemoveOption()
        {
            var votingItemId = HashHelper.ConcatAndCompute(
                HashHelper.ComputeFrom(_votingRegisterInput),
                HashHelper.ComputeFrom(InitAccount.ConvertAddress()));

            var info = await _voteSub.GetVotingItem.CallAsync(new GetVotingItemInput
            {
                VotingItemId = votingItemId
            });

            if (info.Options.Contains("Vote"))
            {
                var result = await _voteSub.RemoveOption.SendAsync(new RemoveOptionInput
                {
                    VotingItemId = votingItemId,
                    Option = "Vote"
                });
                result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            }

            info = await _voteSub.GetVotingItem.CallAsync(new GetVotingItemInput
            {
                VotingItemId = votingItemId
            });
            info.Options.ShouldNotContain("Vote");
        }


        [TestMethod]
        public async Task Vote()
        {
            _tokenContract.TransferBalance(InitAccount, TestAccount, 1000_000000000, "ELF");
            _tokenContract.TransferBalance(InitAccount, TestAccount, 1000_000000000, Symbol);
            var beforeVote = _tokenContract.GetUserBalance(TestAccount, Symbol);
            var votingItemId = HashHelper.ConcatAndCompute(
                HashHelper.ComputeFrom(_votingRegisterInput),
                HashHelper.ComputeFrom(InitAccount.ConvertAddress()));
            var voteInput = new VoteInput
            {
                Voter = InitAccount.ConvertAddress(),
                VoteId = HashHelper.ComputeFrom("VOTE"),
                Amount = 1000,
                VotingItemId = votingItemId,
                Option = "Vote",
                IsChangeTarget = true
            };
            var result = await _testVoteSub.Vote.SendAsync(voteInput);
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var log = result.TransactionResult.Logs.First(l => l.Name.Contains(nameof(Voted))).NonIndexed;
            var votedInfo = Voted.Parser.ParseFrom(log);
            votedInfo.Amount.ShouldBe(1000);
            Logger.Info(votedInfo);

            var afterVote = _tokenContract.GetUserBalance(TestAccount, Symbol);
            afterVote.ShouldBe(beforeVote - 1000);
        }

        [TestMethod]
        public async Task Withdraw()
        {
            var voteId = Hash.LoadFromHex("e51ba2497a195d054f373132a66ea5a3e69fc5c90ce08c824919e93a6706008f");
            var beforeBalance = _tokenContract.GetUserBalance(TestAccount, Symbol);

            var voteInfo = await _testVoteSub.GetVotingRecord.CallAsync(voteId);
            var amount = voteInfo.Amount;
            voteInfo.Voter.ShouldBe(TestAccount.ConvertAddress());
            voteInfo.IsWithdrawn.ShouldBeFalse();
            
            var input = new WithdrawInput
            {
                VoteId = voteId
            };
            var result = await _testVoteSub.Withdraw.SendAsync(input);
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            
            var afterBalance = _tokenContract.GetUserBalance(TestAccount, Symbol);
            beforeBalance.ShouldBe(afterBalance - amount);
            
            voteInfo = await _testVoteSub.GetVotingRecord.CallAsync(voteId);
            voteInfo.IsWithdrawn.ShouldBeTrue();
        }
    }
}