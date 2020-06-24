using System.Linq;
using Acs1;
using AElf.Contracts.Election;
using AElfChain.Common.Contracts;
using AElf.Types;
using AElfChain.Common.DtoExtension;
using Google.Protobuf.WellKnownTypes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace AElf.Automation.EconomicSystemTest
{
    [TestClass]
    public class NodeTests : ElectionTests
    {
        [TestInitialize]
        public void InitializeNodeTests()
        {
            Initialize();
        }

        [TestCleanup]
        public void CleanUpNodeTests()
        {
            TestCleanUp();
        }

        [TestMethod]
        public void Announcement_AllNodes_Scenario()
        {
            foreach (var nodeAddress in FullNodeAddress)
            {
                var result = Behaviors.AnnouncementElection(nodeAddress);
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            }
        }
        
        
        [TestMethod]
        public void AnnouncementNode()
        {
            Behaviors.TransferToken(InitAccount, FullNodeAddress[0], 10_1000_00000000);
            var result = Behaviors.AnnouncementElection(FullNodeAddress[0]);
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        }

        [TestMethod]
        public void NodeAnnounceElectionAction()
        {
            foreach (var user in FullNodeAddress)
            {
                Behaviors.TransferToken(InitAccount, user, 10_1000_00000000);
                var election = Behaviors.ElectionService.GetNewTester(user, "123");
                var electionResult = election.ExecuteMethodWithResult(ElectionMethod.AnnounceElection, new Empty());
                electionResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            }

            var candidateList = Behaviors.GetCandidates();
            _logger.Info($"{candidateList.Value.Count}");
            foreach (var publicKey in candidateList.Value)
                _logger.Info($"Candidate PublicKey: {publicKey.ToByteArray().ToHex()}");
        }


        [TestMethod]
        public void Get_Miners_Count()
        {
            var miners = Behaviors.GetMinersCount();
            miners.ShouldBe(3);
        }


        [TestMethod]
        public void SetMaximumMinersCount()
        {
            var amount = 10;
            var maximumBlocksCount = Behaviors.ConsensusService.GetMaximumMinersCount().Value;
            _logger.Info($"{maximumBlocksCount}");
            var consensus = Behaviors.ConsensusService;
            var input = new Int32Value {Value = amount};
            var result = Behaviors.AuthorityManager.ExecuteTransactionWithAuthority(consensus.ContractAddress,
                nameof(ConsensusMethod.SetMaximumMinersCount), input, InitAccount);
            result.Status.ShouldBe(TransactionResultStatus.Mined);
            maximumBlocksCount = Behaviors.ConsensusService.GetMaximumMinersCount().Value;
            maximumBlocksCount.ShouldBe(amount);
        }
        
        [TestMethod]
        public void SetMaximumMinersCountThroughAssociation()
        {
            var amount = 5;
            var maximumBlocksCount = Behaviors.ConsensusService.GetMaximumMinersCount().Value;
            _logger.Info($"{maximumBlocksCount}");
            var consensus = Behaviors.ConsensusService;
            var association = Behaviors.ContractManager.Association;
            var maximumMinersCountController = Behaviors.ConsensusService.GetMaximumMinersCountController();
            var associationOrganization = maximumMinersCountController.OwnerAddress;
            var input = new Int32Value {Value = amount};
            var proposer = association.GetOrganization(associationOrganization).ProposerWhiteList.Proposers.First();
            var proposalId = association.CreateProposal(consensus.ContractAddress,
                nameof(ConsensusMethod.SetMaximumMinersCount), input, associationOrganization,
                proposer.ToBase58());
            association.ApproveWithAssociation(proposalId, associationOrganization);
            var release = association.ReleaseProposal(proposalId, proposer.ToBase58());
            release.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            maximumBlocksCount = Behaviors.ConsensusService.GetMaximumMinersCount().Value;
            maximumBlocksCount.ShouldBe(amount);
        }

        [TestMethod]
        public void ChangeMaximumMinersCountController()
        {
            var parliament = Behaviors.ContractManager.Parliament;
            var association = Behaviors.ContractManager.Association;
            var consensus = Behaviors.ConsensusService;
            var genesisOwnerAddress = parliament.GetGenesisOwnerAddress();
            var maximumMinersCountController = Behaviors.ConsensusService.GetMaximumMinersCountController();
            var oldInput = new AuthorityInfo
            {
                ContractAddress = parliament.Contract,
                OwnerAddress = genesisOwnerAddress
            };
            var associationOrganization = Behaviors.AuthorityManager.CreateAssociationOrganization();
            var input = new AuthorityInfo
            {
                ContractAddress = association.Contract,
                OwnerAddress = associationOrganization
            };
            if (maximumMinersCountController.ContractAddress.Equals(parliament.Contract))
            {
                var result = Behaviors.AuthorityManager.ExecuteTransactionWithAuthority(consensus.ContractAddress,
                    nameof(ConsensusMethod.ChangeMaximumMinersCountController), input, InitAccount);
                result.Status.ShouldBe(TransactionResultStatus.Mined);
                maximumMinersCountController = Behaviors.ConsensusService.GetMaximumMinersCountController();
                maximumMinersCountController.ContractAddress.ShouldBe(association.Contract);
                maximumMinersCountController.OwnerAddress.ShouldBe(associationOrganization);
            }
            else if (maximumMinersCountController.ContractAddress.Equals(association.Contract))
            {
                var proposer = association.GetOrganization(associationOrganization).ProposerWhiteList.Proposers.First();
                var proposalId = association.CreateProposal(consensus.ContractAddress,
                    nameof(ConsensusMethod.ChangeMaximumMinersCountController), oldInput, associationOrganization,
                    proposer.ToBase58());
                association.ApproveWithAssociation(proposalId, associationOrganization);
                var release = association.ReleaseProposal(proposalId, proposer.ToBase58());
                release.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
                maximumMinersCountController = Behaviors.ConsensusService.GetMaximumMinersCountController();
                maximumMinersCountController.ContractAddress.ShouldBe(parliament.Contract);
                maximumMinersCountController.OwnerAddress.ShouldBe(genesisOwnerAddress);
            }
        }

        [TestMethod]
        public void GetMaximumMinersCount()
        {
            var maximumMinersCount = Behaviors.ConsensusService.GetMaximumMinersCount().Value;
            _logger.Info($"{maximumMinersCount}");
        }

        [TestMethod]
        [DataRow(0)]
        public void GetVotesInformationResult(int nodeId)
        {
            var records = Behaviors.GetElectorVoteWithAllRecords(UserList[nodeId]);
        }

        [TestMethod]
        public void GetVoteStatus()
        {
            var termNumber =
                Behaviors.ConsensusService.CallViewMethod<SInt64Value>(ConsensusMethod.GetCurrentTermNumber,
                    new Empty()).Value;
            var candidateList = Behaviors.GetCandidates();
            var voteMessage =
                $"TermNumber={termNumber}, candidates count is {candidateList.Value.Count}, got vote keys info: \r\n";
            foreach (var fullNode in candidateList.Value)
            {
                var candidateVote = Behaviors.ElectionService.CallViewMethod<CandidateVote>(
                    ElectionMethod.GetCandidateVote,
                    new StringValue
                    {
                        Value = fullNode.ToHex()
                    });
                if (candidateVote.Equals(new CandidateVote()))
                    continue;
                voteMessage +=
                    $" {fullNode.ToHex()} All tickets: {candidateVote.AllObtainedVotedVotesAmount}, Active tickets: {candidateVote.ObtainedActiveVotedVotesAmount}\r\n";
            }

            _logger.Info(voteMessage);
        }

        [TestMethod]
        public void GetVictories()
        {
            var victories = Behaviors.GetVictories();

            var publicKeys = victories.Value.Select(o => o.ToByteArray().ToHex()).ToList();

            publicKeys.Contains(Behaviors.NodeManager.GetAccountPublicKey(FullNodeAddress[0])).ShouldBeTrue();
            publicKeys.Contains(Behaviors.NodeManager.GetAccountPublicKey(FullNodeAddress[1])).ShouldBeTrue();
            publicKeys.Contains(Behaviors.NodeManager.GetAccountPublicKey(FullNodeAddress[2])).ShouldBeTrue();
        }

        [TestMethod]
        [DataRow(0)]
        public void QuitElection(int nodeId)
        {
            var beforeBalance = Behaviors.GetBalance(FullNodeAddress[nodeId]).Balance;
            var result = Behaviors.QuitElection(FullNodeAddress[nodeId]);
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var fee = result.GetDefaultTransactionFee();
            var afterBalance = Behaviors.GetBalance(FullNodeAddress[nodeId]).Balance;
            beforeBalance.ShouldBe(afterBalance - 100000_00000000L + fee);
        }

        [TestMethod]
        public void GetCandidates()
        {
            var candidates = Behaviors.GetCandidates();
            _logger.Info($"Candidate count: {candidates.Value.Count}");
            foreach (var candidate in candidates.Value) _logger.Info($"Candidate: {candidate.ToByteArray().ToHex()}");
        }

        [TestMethod]
        public void GetTreasurySchemes()
        {
            var treasury = Behaviors.Treasury;
            Behaviors.ProfitService.GetTreasurySchemes(treasury.ContractAddress);
        }

        [TestMethod]
        public void GetCandidateHistory()
        {
            foreach (var candidate in FullNodeAddress)
            {
                var candidateResult = Behaviors.GetCandidateInformation(candidate);
                _logger.Info("Candidate: ");
                _logger.Info($"PublicKey: {candidateResult.Pubkey}");
                _logger.Info($"Terms: {candidateResult.Terms}");
                _logger.Info($"ContinualAppointmentCount: {candidateResult.ContinualAppointmentCount}");
                _logger.Info($"ProducedBlocks: {candidateResult.ProducedBlocks}");
                _logger.Info($"MissedTimeSlots: {candidateResult.MissedTimeSlots}");
                _logger.Info($"AnnouncementTransactionId: {candidateResult.AnnouncementTransactionId}");
            }
        }
    }
}