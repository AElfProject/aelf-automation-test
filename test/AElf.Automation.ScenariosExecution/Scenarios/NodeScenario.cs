using System;
using System.Collections.Generic;
using System.Linq;
using AElf.Automation.Common.Contracts;
using AElf.Automation.Common.WebApi.Dto;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.Election;
using AElf.Contracts.Profit;
using AElf.Types;
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

            //Announcement
            NodeAnnounceElectionAction();
        }

        public void RunNodeScenario()
        {
            ExecuteContinuousTasks(new Action[]
            {
                NodeAnnounceElectionAction,
                NodeTakeProfitAction,
                NodeQuitElectionAction,
                NodeQueryInformationAction
            }, true, 10);
        }

        public void NodeScenarioJob()
        {
            ExecuteStandaloneTask(new Action[]
            {
                NodeAnnounceElectionAction,
                NodeTakeProfitAction,
                NodeQuitElectionAction,
                NodeQueryInformationAction
            });
        }

        private void NodeAnnounceElectionAction()
        {
            var candidates = Election.CallViewMethod<PublicKeysList>(ElectionMethod.GetCandidates, new Empty());
            var publicKeysList = candidates.Value.Select(o => o.ToByteArray().ToHex()).ToList();
            var count = 0;
            foreach (var fullNode in FullNodes)
            {
                if (publicKeysList.Contains(fullNode.PublicKey))
                    continue;
                var election = Election.GetNewTester(fullNode.Account, fullNode.Password);
                var electionResult = election.ExecuteMethodWithResult(ElectionMethod.AnnounceElection, new Empty());
                if (electionResult.InfoMsg is TransactionResultDto electionDto)
                {
                    if (electionDto.Status == "Mined")
                    {
                        count++;
                        Logger.WriteInfo($"User {fullNode.Account} announcement election success.");
                        UserScenario.GetCandidates(Election); //更新candidates列表
                    }
                }

                if (count == 3)
                    break;
            }
        }

        private void NodeQuitElectionAction()
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
                var quitResult = election.ExecuteMethodWithResult(ElectionMethod.QuitElection, new Empty());
                if (!(quitResult.InfoMsg is TransactionResultDto electionDto)) continue;
                if (electionDto.Status != "Mined") continue;
                Logger.WriteInfo($"User {fullNode.Account} quit election success.");
                UserScenario.GetCandidates(Election); //更新candidates列表
                break;
            }
        }

        private void NodeTakeProfitAction()
        {
            var termNumber = Consensus.GetCurrentTermInformation();
            if (termNumber == _termNumber) return;

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
        }

        private void NodeQueryInformationAction()
        {
            var termNumber = Consensus.GetCurrentTermInformation();
            if (_termNumber == termNumber)
                return;

            Logger.WriteInfo($"Current term number is: {termNumber}");
            _termNumber = termNumber;

            GetLastTermBalanceInformation(termNumber);
            GetCurrentMinersInformation(termNumber);
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
                if (key == ProfitType.Treasury) continue;
                var address = Profit.GetProfitItemVirtualAddress(value, termNumber - 1);
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
                if (candidateResult.AnnouncementTransactionId == Hash.Empty) continue;

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

        private void GetCurrentMinersInformation(long termNumber)
        {
            var miners = Consensus.CallViewMethod<MinerList>(ConsensusMethod.GetCurrentMinerList, new Empty());
            var minersPublicKeys = miners.PublicKeys.Select(o => o.ToByteArray().ToHex()).ToList();
            var minerArray = new List<string>();
            foreach (var bp in BpNodes)
            {
                if (minersPublicKeys.Contains(bp.PublicKey))
                    minerArray.Add(bp.Name);
            }

            foreach (var full in FullNodes)
            {
                if (minersPublicKeys.Contains(full.PublicKey))
                    minerArray.Add(full.Name);
            }

            Logger.WriteInfo($"TermNumber = {termNumber}, miners are: [{string.Join(",", minerArray)}]");
        }

        private void TakeProfit(string account, Hash profitId)
        {
            var beforeBalance = Token.GetUserBalance(account);

            //Get user profit amount
            var profitAmount = Profit.GetProfitAmount(account, profitId);
            if (profitAmount == 0)
                return;

            Logger.WriteInfo($"ProfitAmount: node {account} profit amount is {profitAmount}");
            //Profit.SetAccount(account);
            var profit = Profit.GetNewTester(account);
            profit.ExecuteMethodWithResult(ProfitMethod.Profit, new ProfitInput
            {
                ProfitId = profitId
            });

            var afterBalance = Token.GetUserBalance(account);
            if (beforeBalance != afterBalance)
                Logger.WriteInfo(
                    $"Profit success - node {account} get profit from Id: {profitId}, value is: {afterBalance - beforeBalance}");
        }
    }
}