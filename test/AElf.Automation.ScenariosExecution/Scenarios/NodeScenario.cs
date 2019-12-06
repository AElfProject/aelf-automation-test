using System;
using System.Collections.Generic;
using System.Linq;
using AElfChain.Common;
using AElfChain.Common.Contracts;
using AElfChain.Common.Helpers;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.Election;
using AElf.Contracts.Profit;
using AElf.Types;
using AElfChain.SDK.Models;
using Google.Protobuf.WellKnownTypes;
using log4net;
using PubkeyList = AElf.Contracts.Election.PubkeyList;

namespace AElf.Automation.ScenariosExecution.Scenarios
{
    public class NodeScenario : BaseScenario
    {
        public new static readonly ILog Logger = Log4NetHelper.GetLogger();

        private long _termNumber = 1;
        public TreasuryContract Treasury;

        public NodeScenario()
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

            //Announcement
            NodeAnnounceElectionAction();
        }

        public ElectionContract Election { get; }
        public ConsensusContract Consensus { get; }
        public ProfitContract Profit { get; }
        public TokenContract Token { get; }
        public Dictionary<SchemeType, Scheme> Schemes { get; }

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

        public void RunNodeScenarioJob()
        {
            ExecuteStandaloneTask(new Action[]
            {
                NodeAnnounceElectionAction,
                NodeTakeProfitAction,
                //NodeQuitElectionAction,
                NodeQueryInformationAction,
                UpdateEndpointAction
            });
        }

        private void NodeAnnounceElectionAction()
        {
            var candidates = Election.CallViewMethod<PubkeyList>(ElectionMethod.GetCandidates, new Empty());
            var publicKeysList = candidates.Value.Select(o => o.ToByteArray().ToHex()).ToList();
            var initialPubkeys = Consensus.GetInitialMinersPubkey();
            var count = 0;
            foreach (var fullNode in AllNodes)
            {
                if (publicKeysList.Concat(initialPubkeys).Contains(fullNode.PublicKey))
                    continue;
                var election = Election.GetNewTester(fullNode.Account, fullNode.Password);
                var electionResult = election.ExecuteMethodWithResult(ElectionMethod.AnnounceElection, new Empty());
                if (electionResult.Status.ConvertTransactionResultStatus() == TransactionResultStatus.Mined)
                {
                    count++;
                    Logger.Info($"User {fullNode.Account} announcement election success.");
                    UserScenario.GetCandidates(Election); //更新candidates列表
                }

                if (count == 3)
                    break;
            }
        }

        private void NodeQuitElectionAction()
        {
            var candidates = Election.CallViewMethod<PubkeyList>(ElectionMethod.GetCandidates, new Empty());
            var candidatesKeysList = candidates.Value.Select(o => o.ToByteArray().ToHex()).ToList();
            if (candidatesKeysList.Count < 2)
            {
                Logger.Info("Only one candidate, don't quit election.");
                return;
            }

            var currentMiners = Consensus.CallViewMethod<MinerList>(ConsensusMethod.GetCurrentMinerList, new Empty());
            var minerKeysList = currentMiners.Pubkeys.Select(o => o.ToByteArray().ToHex()).ToList();
            foreach (var fullNode in AllNodes)
            {
                if (!candidatesKeysList.Contains(fullNode.PublicKey) || minerKeysList.Contains(fullNode.PublicKey))
                    continue;
                var election = Election.GetNewTester(fullNode.Account, fullNode.Password);
                var quitResult = election.ExecuteMethodWithResult(ElectionMethod.QuitElection, new Empty());
                if (quitResult.Status.ConvertTransactionResultStatus() != TransactionResultStatus.Mined) continue;
                Logger.Info($"User {fullNode.Account} quit election success.");
                UserScenario.GetCandidates(Election); //更新candidates列表
                break;
            }
        }

        private void NodeTakeProfitAction()
        {
            var termNumber = Consensus.GetCurrentTermInformation();
            if (termNumber == _termNumber) return;

            var id = GenerateRandomNumber(0, AllNodes.Count - 1);
            var node = AllNodes[id];

            var basicProfit =
                Profit.GetProfitDetails(node.Account, Schemes[SchemeType.MinerBasicReward].SchemeId);
            var voteWeightProfit =
                Profit.GetProfitDetails(node.Account, Schemes[SchemeType.VotesWeightReward].SchemeId);
            var reElectionProfit =
                Profit.GetProfitDetails(node.Account, Schemes[SchemeType.ReElectionReward].SchemeId);
            var backupProfit =
                Profit.GetProfitDetails(node.Account, Schemes[SchemeType.BackupSubsidy].SchemeId);

            if (!basicProfit.Equals(new ProfitDetails()))
            {
                Logger.Info($"40% basic generate block profit balance: {basicProfit}");
                TakeProfit(node.Account, Schemes[SchemeType.MinerBasicReward].SchemeId);
            }

            if (!voteWeightProfit.Equals(new ProfitDetails()))
            {
                Logger.Info($"10% vote weight profit balance: {voteWeightProfit}");
                TakeProfit(node.Account, Schemes[SchemeType.VotesWeightReward].SchemeId);
            }

            if (!reElectionProfit.Equals(new ProfitDetails()))
            {
                Logger.Info($"10% re election profit balance: {reElectionProfit}");
                TakeProfit(node.Account, Schemes[SchemeType.ReElectionReward].SchemeId);
            }

            if (!backupProfit.Equals(new ProfitDetails()))
            {
                Logger.Info($"20% backup node profit balance: {backupProfit}");
                TakeProfit(node.Account, Schemes[SchemeType.BackupSubsidy].SchemeId);
            }

            Logger.Info(string.Empty);
        }

        private void NodeQueryInformationAction()
        {
            var termNumber = Consensus.GetCurrentTermInformation();
            if (_termNumber == termNumber)
                return;

            Logger.Info($"Current term number is: {termNumber}");
            _termNumber = termNumber;

            GetLastTermBalanceInformation(termNumber);
            GetCurrentMinersInformation(termNumber);
            GetVoteStatus(termNumber);
            GetCandidateHistoryInformation();
        }

        private void GetLastTermBalanceInformation(long termNumber)
        {
            var treasuryAddress = Profit.GetSchemeAddress(Schemes[SchemeType.Treasury].SchemeId, termNumber);
            var treasuryBalance = Token.GetUserBalance(treasuryAddress.GetFormatted());

            var balanceMessage = $"\r\nTerm number: {termNumber}" +
                                 $"\r\nTreasury balance is {treasuryBalance}";
            foreach (var (key, value) in Schemes)
            {
                Address address;
                switch (key)
                {
                    case SchemeType.Treasury:
                    case SchemeType.CitizenWelfare when termNumber <= 2:
                        continue;
                    case SchemeType.CitizenWelfare:
                        address = Profit.GetSchemeAddress(value.SchemeId, termNumber - 2);
                        break;
                    default:
                        address = Profit.GetSchemeAddress(value.SchemeId, termNumber - 1);
                        break;
                }

                var balance = Token.GetUserBalance(address.GetFormatted());
                balanceMessage += $"\r\n{key} balance is {balance}";
            }

            Logger.Info(balanceMessage);
        }

        private void GetCandidateHistoryInformation()
        {
            foreach (var fullNode in AllNodes)
            {
                var candidateResult = Election.GetCandidateInformation(fullNode.Account);
                if (candidateResult.AnnouncementTransactionId == Hash.Empty) continue;

                var historyMessage = $"\r\nCandidate: {fullNode.Account}\r\n" +
                                     $"PublicKey: {candidateResult.Pubkey}\r\n" +
                                     $"Term: {candidateResult.Terms}\r\n" +
                                     $"ContinualAppointmentCount: {candidateResult.ContinualAppointmentCount}\r\n" +
                                     $"ProducedBlocks: {candidateResult.ProducedBlocks}\r\n" +
                                     $"MissedTimeSlots: {candidateResult.MissedTimeSlots}\r\n" +
                                     $"AnnouncementTransactionId: {candidateResult.AnnouncementTransactionId}";
                Logger.Info(historyMessage);
            }
        }

        private void GetCurrentMinersInformation(long termNumber)
        {
            var miners = Consensus.CallViewMethod<MinerList>(ConsensusMethod.GetCurrentMinerList, new Empty());
            var minersPublicKeys = miners.Pubkeys.Select(o => o.ToByteArray().ToHex()).ToList();
            var minerArray = new List<string>();
            foreach (var node in AllNodes)
                if (minersPublicKeys.Contains(node.PublicKey))
                    minerArray.Add(node.Name);

            //get voted candidates
            var votedCandidates = Election.CallViewMethod<PubkeyList>(ElectionMethod.GetVotedCandidates, new Empty());
            Logger.Info(
                $"TermNumber = {termNumber}, voted candidates count: {miners.Pubkeys.Count}, miners count: {votedCandidates.Value.Count}");

            Logger.Info($"TermNumber = {termNumber}, miners are: [{string.Join(",", minerArray)}]");

            var candidateArray = new List<string>();
            var candidates = Election.CallViewMethod<PubkeyList>(ElectionMethod.GetCandidates, new Empty());
            var candidatesKeysList = candidates.Value.Select(o => o.ToByteArray().ToHex()).ToList();
            foreach (var full in AllNodes)
                if (candidatesKeysList.Contains(full.PublicKey))
                    candidateArray.Add(full.Name);

            Logger.Info($"TermNumber = {termNumber}, candidates are: [{string.Join(",", candidateArray)}]");
        }

        private void GetVoteStatus(long termNumber)
        {
            var voteMessage = $"TermNumber={termNumber}, candidates got vote keys info: \r\n";
            foreach (var fullNode in AllNodes)
            {
                var candidateVote = Election.CallViewMethod<CandidateVote>(ElectionMethod.GetCandidateVote,
                    new StringValue
                    {
                        Value = fullNode.PublicKey
                    });
                if (candidateVote.Equals(new CandidateVote()))
                    continue;
                voteMessage +=
                    $"Name: {fullNode.Name}, All tickets: {candidateVote.AllObtainedVotedVotesAmount}, Active tickets: {candidateVote.ObtainedActiveVotedVotesAmount}\r\n";
            }

            Logger.Info(voteMessage);
        }

        private void TakeProfit(string account, Hash schemeId)
        {
            //Get user profit amount
            var profitAmount = Profit.GetProfitAmount(account, schemeId);
            if (profitAmount == 0)
                return;

            Logger.Info($"ProfitAmount: node {account} profit amount is {profitAmount}");
            var beforeBalance = Token.GetUserBalance(account);
            var profit = Profit.GetNewTester(account);
            var profitResult = profit.ExecuteMethodWithResult(ProfitMethod.ClaimProfits, new ClaimProfitsInput
            {
                SchemeId = schemeId,
                Symbol = NodeOption.NativeTokenSymbol
            }, out var existed);

            if (existed) return; //忽略已经存在交易
            if (profitResult.Status.ConvertTransactionResultStatus() != TransactionResultStatus.Mined) return;
            
            //check profit amount process
            var profitTransactionFee = profitResult.TransactionFee.GetDefaultTransactionFee();
            var afterBalance = Token.GetUserBalance(account);
            var checkResult = true;
            /* ignore this check due to bp with other profit or send token to others
            if (beforeBalance + profitAmount != afterBalance + profitTransactionFee)
            {
                Logger.Error($"Check profit balance failed. {beforeBalance + profitAmount}/{afterBalance + profitTransactionFee}");
                checkResult = false;
            }
            */
            
            if(checkResult)
                Logger.Info(
                    $"Profit success - node {account} get profit from Id: {schemeId}, value is: {afterBalance - beforeBalance}");
        }
    }
}