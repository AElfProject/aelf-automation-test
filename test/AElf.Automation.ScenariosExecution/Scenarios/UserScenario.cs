using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AElf.Automation.Common.Contracts;
using AElf.Automation.Common.Helpers;
using AElf.Automation.Common.WebApi.Dto;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.Election;
using AElf.Contracts.MultiToken;
using AElf.Contracts.Profit;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using log4net;

namespace AElf.Automation.ScenariosExecution.Scenarios
{
    public class UserScenario : BaseScenario
    {
        public TreasuryContract Treasury { get; }
        public ElectionContract Election { get; }
        public ConsensusContract Consensus { get; }
        public ProfitContract Profit { get; }
        public TokenContract Token { get; }
        public List<string> Testers { get; }
        public Dictionary<SchemeType, Scheme> Schemes { get; }

        private static List<string> _candidates;
        private static List<string> _candidatesExcludeMiners;
        
        public new static readonly ILog Logger = Log4NetHelper.GetLogger();

        public UserScenario()
        {
            InitializeScenario();
            Treasury = Services.TreasuryService;
            Election = Services.ElectionService;
            Consensus = Services.ConsensusService;
            Profit = Services.ProfitService;
            Token = Services.TokenService;

            //Get Profit items
            Profit.GetTreasurySchemes(Treasury.ContractAddress);
            Schemes = ProfitContract.Schemes;

            Testers = AllTesters.GetRange(50, 30);
        }

        public void RunUserScenario()
        {
            ExecuteContinuousTasks(new Action[]
            {
                UserVotesAction,
                TakeVotesProfitAction
            }, true, 30);
        }

        public void RunUserScenarioJob()
        {
            ExecuteStandaloneTask(new Action[]
            {
                UserVotesAction,
                TakeVotesProfitAction
            });
        }

        private void UserVotesAction()
        {
            GetCandidates(Election);
            GetCandidatesExcludeCurrentMiners();

            if (_candidates.Count < 2)
                return;

            var times = GenerateRandomNumber(3, 5);
            for (var i = 0; i < times; i++)
            {
                var id = GenerateRandomNumber(0, Testers.Count - 1);
                UserVote(Testers[id]);

                Thread.Sleep(10);
            }
        }

        private void TakeVotesProfitAction()
        {
            var times = GenerateRandomNumber(3, 5);
            for (var i = 0; i < times; i++)
            {
                var id = GenerateRandomNumber(0, Testers.Count - 1);
                TakeUserProfit(Testers[id]);

                Thread.Sleep(10);
            }
        }

        private void TakeUserProfit(string account)
        {
            var schemeId = Schemes[SchemeType.CitizenWelfare].SchemeId;
            var voteProfit =
                Profit.GetProfitDetails(account, schemeId);
            if (voteProfit.Equals(new ProfitDetails())) return;
            $"20% user vote profit for account: {account}.\r\nDetails number: {voteProfit.Details}".WriteSuccessLine();

            //Get user profit amount
            var profitAmount = Profit.GetProfitAmount(account, schemeId);
            if (profitAmount == 0)
                return;

            Logger.Info($"Profit amount: user {account} profit amount is {profitAmount}");
            //Profit.SetAccount(account);
            var profit = Profit.GetNewTester(account);
            var profitResult = profit.ExecuteMethodWithResult(ProfitMethod.ClaimProfits, new ClaimProfitsInput
            {
                SchemeId = schemeId,
                Symbol = "ELF"
            });

            if (!(profitResult.InfoMsg is TransactionResultDto profitDto)) return;
            if (profitDto.Status.ConvertTransactionResultStatus() == TransactionResultStatus.Mined)
                Logger.Info(
                    $"Profit success - user {account} get vote profit from Id: {schemeId}, value is: {profitAmount}");
        }

        private void UserVote(string account)
        {
            var lockTime = GenerateRandomNumber(3, 30) * 30;
            var amount = GenerateRandomNumber(1, 5) * 5;
            if (_candidatesExcludeMiners.Count != 0)
            {
                var id = GenerateRandomNumber(0, _candidatesExcludeMiners.Count - 1);
                UserVote(account, _candidatesExcludeMiners[id], lockTime, amount);
            }
            else
            {
                var id = GenerateRandomNumber(0, _candidates.Count - 1);
                UserVote(account, _candidates[id], lockTime, amount);
            }
        }

        private void UserVote(string account, string candidatePublicKey, int lockTime, long amount)
        {
            var beforeElfBalance = Token.GetUserBalance(account);
            var beforeVoteBalance = Token.GetUserBalance(account, "VOTE");
            if (beforeElfBalance < amount * 10000_0000) // balance not enough, bp transfer again
            {
                var token = Token.GetNewTester(BpNodes.First().Account);
                token.ExecuteMethodWithResult(TokenMethod.Transfer, new TransferInput
                {
                    Symbol = "ELF",
                    Amount = 10_0000_00000000,
                    To = AddressHelper.Base58StringToAddress(account),
                    Memo = $"Transfer for voting = {Guid.NewGuid()}"
                });
            }

            //Election.SetAccount(account);
            var election = Election.GetNewTester(account);
            election.ExecuteMethodWithResult(ElectionMethod.Vote, new VoteMinerInput
            {
                CandidatePubkey = candidatePublicKey,
                Amount = amount,
                EndTimestamp = DateTime.UtcNow.Add(TimeSpan.FromDays(lockTime))
                    .Add(TimeSpan.FromHours(1))
                    .ToTimestamp()
            });

            var afterElfBalance = Token.GetUserBalance(account);
            var afterVoteBalance = Token.GetUserBalance(account, "VOTE");
            if (beforeElfBalance == afterElfBalance + amount * 100000000 &&
                beforeVoteBalance == afterVoteBalance - amount)
                Logger.Info(
                    $"Vote success - {account} vote candidate: {candidatePublicKey} with amount: {amount} lock time: {lockTime} days.");
        }

        public static void GetCandidates(ElectionContract election)
        {
            var candidatePublicKeys = election.CallViewMethod<Candidates>(ElectionMethod.GetCandidates, new Empty());
            _candidates = candidatePublicKeys.Pubkeys.Select(o => o.ToByteArray().ToHex()).ToList();
        }

        private void GetCandidatesExcludeCurrentMiners()
        {
            //query current miners
            var miners = Consensus.CallViewMethod<MinerList>(ConsensusMethod.GetCurrentMinerList, new Empty());
            var minersPublicKeys = miners.Pubkeys.Select(o => o.ToByteArray().ToHex()).ToList();

            //query current candidates
            _candidatesExcludeMiners = new List<string>();
            _candidates.ForEach(o =>
            {
                if (!minersPublicKeys.Contains(o))
                {
                    _candidatesExcludeMiners.Add(o);
                }
            });
        }
    }
}