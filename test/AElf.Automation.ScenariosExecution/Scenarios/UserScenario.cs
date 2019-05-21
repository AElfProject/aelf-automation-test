using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AElf.Automation.Common.Contracts;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.Election;
using AElf.Contracts.Profit;
using Google.Protobuf.Collections;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Automation.ScenariosExecution.Scenarios
{
    public class UserScenario : BaseScenario
    {
        public ElectionContract Election { get; }
        public ConsensusContract Consensus { get; }
        public ProfitContract Profit { get; }
        public TokenContract Token { get; }
        public List<string> Testers { get; }

        public Dictionary<ProfitType, Hash> ProfitItemIds { get; }
        private List<string> candidates;

        public UserScenario()
        {
            InitializeScenario();
            Election = Services.ElectionService;
            Consensus = Services.ConsensusService;
            Profit = Services.ProfitService;
            Token = Services.TokenService;

            //Get Profit items
            Profit.GetProfitItemIds(Election.ContractAddress);
            ProfitItemIds = Profit.ProfitItemIds;

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
        
        public void UserVotesAction()
        {
            GetCandidates();
            if (candidates.Count < 2)
                return;
            
            var times = GenerateRandomNumber(10, 20);
            for (var i = 0; i < times; i++)
            {
                var id = GenerateRandomNumber(0, Testers.Count-1);
                UserVote(Testers[id]);
                
                Thread.Sleep(3 * 1000);
            }
        }

        public void TakeVotesProfitAction()
        {
            GetCandidates();
            
            var times = GenerateRandomNumber(5, 10);
            for (var i = 0; i < times; i++)
            {
                var id = GenerateRandomNumber(0, Testers.Count - 1);
                TakeUserProfit(Testers[id]);
                
                Thread.Sleep(10 * 1000);
            }
        }

        private void TakeUserProfit(string account)
        {
            var profitId = ProfitItemIds[ProfitType.CitizenWelfare];
            var voteProfit =
                Profit.GetProfitDetails(account, profitId);
            if (voteProfit.Equals(new ProfitDetails())) return;
            Logger.WriteInfo($"20% user vote profit details: {voteProfit}");
            
            //Get user profit amount
            var profitAmount = Profit.GetProfitAmount(account, profitId);
            Logger.WriteInfo($"ProfitAmount: user {account} profit amount is {profitAmount}");
            
            var beforeBalance = Token.GetUserBalance(account);
            Profit.SetAccount(account);
            Profit.ExecuteMethodWithResult(ProfitMethod.Profit, new ProfitInput
            {
                ProfitId = profitId
            });
            var afterBalance = Token.GetUserBalance(account);
            if(afterBalance != beforeBalance)
                Logger.WriteInfo($"Profit success - {account} get profit from Id: {profitId}, value is: {afterBalance - beforeBalance}");
        }

        private void UserVote(string account)
        {
            var id = GenerateRandomNumber(0, candidates.Count - 1);
            var lockTime = GenerateRandomNumber(90, 1080);
            var amount = GenerateRandomNumber(1, 5) * 10;

            UserVote(account, candidates[id], lockTime, amount);
        }
        
        private void UserVote(string account, string candidatePublicKey, int lockTime, long amount)
        {
            var beforeBalance = Token.GetUserBalance(account);
            if (beforeBalance < amount)
                return;
            
            Election.SetAccount(account);
            Election.ExecuteMethodWithResult(ElectionMethod.Vote, new VoteMinerInput
            {
                CandidatePublicKey = candidatePublicKey,
                Amount = amount,
                EndTimestamp = DateTime.UtcNow.Add(TimeSpan.FromDays(lockTime)).ToTimestamp()
            });

            var afterBalance = Token.GetUserBalance(account);
            if(beforeBalance == afterBalance + amount)
                Logger.WriteInfo($"Vote success - User: {account} vote candidate: {candidatePublicKey} with amount: {amount} lock time: {lockTime} days.");
        }

        private void GetCandidates()
        {
            var candidatePublicKeys = Election.CallViewMethod<Candidates>(ElectionMethod.GetCandidates, new Empty());
            candidates = candidatePublicKeys.PublicKeys.Select(o => o.ToByteArray().ToHex()).ToList();
        }
    }
}