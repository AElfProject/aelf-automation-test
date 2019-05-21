using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AElf.Automation.Common.Contracts;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.Election;
using AElf.Contracts.Profit;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Automation.ScenariosExecution.Scenarios
{
    public class NodeScenario : BaseScenario
    {
        public ElectionContract Election { get; }
        public ConsensusContract Consensus { get; }
        public ProfitContract Profit { get; }
        public TokenContract Token { get; }

        public Dictionary<ProfitType, Hash> ProfitItemIds { get; }

        private long _termNumber = 1;

        public NodeScenario()
        {
            InitializeScenario();
            Election = Services.ElectionService;
            Consensus = Services.ConsensusService;
            Profit = Services.ProfitService;
            Token = Services.TokenService;

            //Get Profit items
            Profit.GetProfitItemIds(Election.ContractAddress);
            ProfitItemIds = Profit.ProfitItemIds;
        }

        public void RunNodeScenario()
        {
            ExecuteContinuousTasks(new Action[]
            {
                NodeAnnounceElectionAction,
                NodeTakeProfitAction,
                NodeQuitElectionAction,
                NodeGetHistoryBalanceAction
            }, true, 10);
        }

        public void NodeAnnounceElectionAction()
        {
            var candidates = Election.CallViewMethod<PublicKeysList>(ElectionMethod.GetCandidates, new Empty());
            var publicKeysList = candidates.Value.Select(o => o.ToByteArray().ToHex()).ToList();
            var count = 0;
            foreach (var fullNode in FullNodes)
            {
                if (publicKeysList.Contains(fullNode.PublicKey))
                    continue;
                var election = Election.GetNewTester(fullNode.Account, fullNode.Password);
                election.ExecuteMethodWithResult(ElectionMethod.AnnounceElection, new Empty());
                count++;
                if(count==2)
                    break;
            }
            
            Thread.Sleep(30 * 1000);
        }

        public void NodeQuitElectionAction()
        {
            var candidates = Election.CallViewMethod<PublicKeysList>(ElectionMethod.GetCandidates, new Empty());
            var candidatesKeysList = candidates.Value.Select(o => o.ToByteArray().ToHex()).ToList();
            if (candidatesKeysList.Count < 2)
            {
                Logger.WriteInfo("Only one candidate, don't quit election.");
                return;
            }

            var currentMiners = Consensus.CallViewMethod<MinerList>(ConsensusMethod.GetCurrentMinerList, new Empty());
            var minerKeysList = currentMiners.PublicKeys.Select(o => o.ToByteArray().ToHex()).ToList();
            foreach (var fullNode in FullNodes)
            {
                if (!candidatesKeysList.Contains(fullNode.PublicKey) || minerKeysList.Contains(fullNode.PublicKey))
                    continue;
                var election = Election.GetNewTester(fullNode.Account, fullNode.Password);
                election.ExecuteMethodWithResult(ElectionMethod.QuitElection, new Empty());
                break;
            }
            
            Thread.Sleep(30 * 1000);
        }

        public void NodeTakeProfitAction()
        {
            var termNumber = Consensus.GetCurrentTermInformation();
            if (termNumber == _termNumber) return;
            _termNumber = termNumber;

            var id = GenerateRandomNumber(0, FullNodes.Count - 1);
            var node = FullNodes[id];

            var basicProfit =
                Profit.GetProfitDetails(node.Account, ProfitItemIds[ProfitType.BasicMinerReward]);
            var voteWeightProfit =
                Profit.GetProfitDetails(node.Account, ProfitItemIds[ProfitType.VotesWeightReward]);
            var reElectionProfit =
                Profit.GetProfitDetails(node.Account, ProfitItemIds[ProfitType.ReElectionReward]);
            var backupProfit =
                Profit.GetProfitDetails(node.Account, ProfitItemIds[ProfitType.BackSubsidy]);

            if (!basicProfit.Equals(new ProfitDetails()))
            {
                Logger.WriteInfo($"40% basic generate block profit balance: {basicProfit}");
                TakeProfit(node.Account, ProfitItemIds[ProfitType.BasicMinerReward]);
            }

            if (!voteWeightProfit.Equals(new ProfitDetails()))
            {
                Logger.WriteInfo($"10% vote weight profit balance: {voteWeightProfit}");
                TakeProfit(node.Account, ProfitItemIds[ProfitType.VotesWeightReward]);
            }

            if (!reElectionProfit.Equals(new ProfitDetails()))
            {
                Logger.WriteInfo($"10% re election profit balance: {reElectionProfit}");

                TakeProfit(node.Account, ProfitItemIds[ProfitType.ReElectionReward]);
            }

            if (!backupProfit.Equals(new ProfitDetails()))
            {
                Logger.WriteInfo($"20% backup node profit balance: {backupProfit}");
                TakeProfit(node.Account, ProfitItemIds[ProfitType.BackSubsidy]);
            }

            Logger.WriteInfo(string.Empty);
            
            Thread.Sleep(10 * 1000);
        }

        public void NodeGetHistoryBalanceAction()
        {
            var termNumber = Consensus.GetCurrentTermInformation();
            if (_termNumber == termNumber)
                return;
            
            Logger.WriteInfo($"Current term number is: {termNumber}");
            _termNumber = termNumber;

            GetLastTermBalanceInformation(termNumber); 
            GetCandidateHistoryInformation();
        }

        private void GetLastTermBalanceInformation(long termNumber)
        {
            var treasuryAddress = Profit.GetTreasuryAddress(ProfitItemIds[ProfitType.Treasury]);
            var treasuryBalance = Token.GetUserBalance(treasuryAddress.GetFormatted());

            var balanceMessage = $"\r\nTerm number: {termNumber}" +
                                 $"\r\nTreasury balance is {treasuryBalance}";
            foreach (var (key, value) in ProfitItemIds)
            {
                if(key == ProfitType.Treasury) continue;
                var address = Profit.GetProfitItemVirtualAddress(value, termNumber-1);
                var balance = Token.GetUserBalance(address.GetFormatted());
                balanceMessage += $"\r\n{key} balance is {balance}";
            }
            Logger.WriteInfo(balanceMessage);
        }

        private void GetCandidateHistoryInformation()
        {
            foreach (var fullNode in FullNodes)
            {
                var candidateResult = Election.GetCandidateInformation(fullNode.Account);
                if(candidateResult.AnnouncementTransactionId == Hash.Empty) continue;
                
                var historyMessage = $"\r\nCandidate: {fullNode.Account}\r\n" +
                    $"PublicKey: {candidateResult.PublicKey}\r\n" + 
                    $"Term: {candidateResult.Terms}\r\n" +
                    $"ContinualAppointmentCount: {candidateResult.ContinualAppointmentCount}\r\n" + 
                    $"ProducedBlocks: {candidateResult.ProducedBlocks}\r\n" +
                    $"MissedTimeSlots: {candidateResult.MissedTimeSlots}\r\n" +
                    $"AnnouncementTransactionId: {candidateResult.AnnouncementTransactionId}";
                Logger.WriteInfo(historyMessage);
            }
        }
        private void TakeProfit(string account, Hash profitId)
        {
            var beforeBalance = Token.GetUserBalance(account);

            //Get user profit amount
            var profitAmount = Profit.GetProfitAmount(account, profitId);
            Logger.WriteInfo($"ProfitAmount: user {account} profit amount is {profitAmount}");

            //Profit.SetAccount(account);
            var profit = Profit.GetNewTester(account);
            profit.ExecuteMethodWithResult(ProfitMethod.Profit, new ProfitInput
            {
                ProfitId = profitId
            });

            var afterBalance = Token.GetUserBalance(account);
            if (beforeBalance != afterBalance)
                Logger.WriteInfo($"Get profit from Id: {profitId}, value is: {afterBalance - beforeBalance}");
        }
    }
}