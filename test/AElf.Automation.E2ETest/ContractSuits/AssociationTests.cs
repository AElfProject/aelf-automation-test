using System.Linq;
using Acs3;
using AElf.Contracts.Association;
using AElf.Types;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using Google.Protobuf.WellKnownTypes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace AElf.Automation.E2ETest.ContractSuits
{
    [TestClass]
    public class AssociationTests : ContractTestBase
    {
        [TestMethod]
        public void AssociationCreateTest()
        {
            var members = ConfigNodes.Select(l => l.Account).ToList().Select(member => member.ConvertAddress());
            var proposer = ConfigNodes.First().Account.ConvertAddress();
            var association = ContractManager.Association;
            //create association organization
            var enumerable = members as Address[] ?? members.ToArray();
            var createInput = new CreateOrganizationInput
            {
                ProposalReleaseThreshold = new ProposalReleaseThreshold
                {
                    MaximalAbstentionThreshold = 1,
                    MaximalRejectionThreshold = 1,
                    MinimalApprovalThreshold = 2,
                    MinimalVoteThreshold = 4
                },
                ProposerWhiteList = new ProposerWhiteList {Proposers = {proposer}},
                OrganizationMemberList = new OrganizationMemberList {OrganizationMembers = {enumerable}}
            };
            var result = association.ExecuteMethodWithResult(AssociationMethod.CreateOrganization,
                createInput);
            var organizationAddress =
                Address.Parser.ParseFrom(ByteArrayHelper.HexStringToByteArray(result.ReturnValue));
            var calculateOrganization =
                association.CallViewMethod<Address>(AssociationMethod.CalculateOrganizationAddress,
                    createInput);
            var organization =
                association.GetOrganization(organizationAddress);
            organization.ProposalReleaseThreshold.MaximalAbstentionThreshold.ShouldBe(1);
            organization.ProposalReleaseThreshold.MaximalRejectionThreshold.ShouldBe(1);
            organization.ProposalReleaseThreshold.MinimalApprovalThreshold.ShouldBe(2);
            organization.ProposalReleaseThreshold.MinimalVoteThreshold.ShouldBe(4);
            organization.OrganizationMemberList.OrganizationMembers.Count.ShouldBe(enumerable.Length);
            organization.ProposerWhiteList.Proposers.Contains(proposer).ShouldBeTrue();
            organization.OrganizationAddress.ShouldBe(calculateOrganization);

            // change organization info:
            var changeInput = new ProposalReleaseThreshold
            {
                MaximalAbstentionThreshold = 1,
                MaximalRejectionThreshold = 1,
                MinimalApprovalThreshold = 1,
                MinimalVoteThreshold = 3
            };
            //creat proposal
            var proposalId = association.CreateProposal(association.ContractAddress,
                nameof(AssociationMethod.ChangeOrganizationThreshold), changeInput,
                organizationAddress, proposer.GetFormatted());
            var proposalInfo = association.CheckProposal(proposalId);
            proposalInfo.Proposer.ShouldBe(enumerable.First());
            proposalInfo.ToBeReleased.ShouldBeFalse();
            proposalInfo.OrganizationAddress.ShouldBe(organizationAddress);
            proposalInfo.ContractMethodName.ShouldBe(nameof(AssociationMethod.ChangeOrganizationThreshold));
            proposalInfo.ToAddress.ShouldBe(association.ContractAddress.ConvertAddress());
            proposalInfo.AbstentionCount.ShouldBe(0);
            proposalInfo.ApprovalCount.ShouldBe(0);
            proposalInfo.RejectionCount.ShouldBe(0);

            var memberResult =
                association.CallViewMethod<BoolValue>(AssociationMethod.ValidateProposerInWhiteList,
                    new ValidateProposerInWhiteListInput
                    {
                        OrganizationAddress = organizationAddress,
                        Proposer = proposer
                    });
            memberResult.Value.ShouldBeTrue();

            // approve/abstention/rejection 
            var minimalApprovalThreshold = organization.ProposalReleaseThreshold.MinimalApprovalThreshold;
            var approveMember = enumerable.Take((int) minimalApprovalThreshold).ToList();
            foreach (var member in approveMember)
            {
                association.SetAccount(member.GetFormatted());
                var approveResult = association.ExecuteMethodWithResult(AssociationMethod.Approve, proposalId);
                approveResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            }

            var otherMiners = enumerable.Where(m => !approveMember.Contains(m)).ToList();
            var abstentionMember = otherMiners.First();
            association.SetAccount(abstentionMember.GetFormatted());
            var abstentionResult = association.ExecuteMethodWithResult(AssociationMethod.Abstain, proposalId);
            abstentionResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var rejectionMembers = otherMiners.Where(r => !abstentionMember.Equals(r)).ToList();
            var rejectionMember = rejectionMembers.First();
            association.SetAccount(rejectionMember.GetFormatted());
            var rejectResult = association.ExecuteMethodWithResult(AssociationMethod.Reject, proposalId);
            rejectResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            proposalInfo = association.CheckProposal(proposalId);
            proposalInfo.ToBeReleased.ShouldBeTrue();
            proposalInfo.AbstentionCount.ShouldBe(1);
            proposalInfo.ApprovalCount.ShouldBe(approveMember.Count);
            proposalInfo.RejectionCount.ShouldBe(1);

            //release 
            var releaseResult = association.ReleaseProposal(proposalId, proposer.GetFormatted());
            releaseResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            organization =
                association.GetOrganization(organizationAddress);
            organization.ProposalReleaseThreshold.MaximalAbstentionThreshold.ShouldBe(1);
            organization.ProposalReleaseThreshold.MaximalRejectionThreshold.ShouldBe(1);
            organization.ProposalReleaseThreshold.MinimalApprovalThreshold.ShouldBe(1);
            organization.ProposalReleaseThreshold.MinimalVoteThreshold.ShouldBe(3);

            //revert
            var revertInput = new ProposalReleaseThreshold
            {
                MaximalAbstentionThreshold = 1,
                MaximalRejectionThreshold = 1,
                MinimalApprovalThreshold = 2,
                MinimalVoteThreshold = 4
            };
            //creat proposal
            var revertProposalId = association.CreateProposal(association.ContractAddress,
                nameof(AssociationMethod.ChangeOrganizationThreshold), revertInput,
                organizationAddress, proposer.GetFormatted());
            association.ApproveWithAssociation(revertProposalId, organizationAddress);
            association.SetAccount(proposer.GetFormatted());
            var revertRelease = association.ReleaseProposal(revertProposalId, proposer.GetFormatted());
            revertRelease.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            organization =
                association.GetOrganization(organizationAddress);
            organization.ProposalReleaseThreshold.MaximalAbstentionThreshold.ShouldBe(1);
            organization.ProposalReleaseThreshold.MaximalRejectionThreshold.ShouldBe(1);
            organization.ProposalReleaseThreshold.MinimalApprovalThreshold.ShouldBe(2);
            organization.ProposalReleaseThreshold.MinimalVoteThreshold.ShouldBe(4);
        }

        [TestMethod]
        public void AssociationChangeWhiteList()
        {
            var association = ContractManager.Association;
            var members = ConfigNodes.Select(l => l.Account).ToList().Select(member => member.ConvertAddress());
            var proposer = ConfigNodes.First().Account.ConvertAddress();
            //create association organization
            var enumerable = members as Address[] ?? members.ToArray();
            var createInput = new CreateOrganizationInput
            {
                ProposalReleaseThreshold = new ProposalReleaseThreshold
                {
                    MaximalAbstentionThreshold = 1,
                    MaximalRejectionThreshold = 1,
                    MinimalApprovalThreshold = 1,
                    MinimalVoteThreshold = 4
                },
                ProposerWhiteList = new ProposerWhiteList {Proposers = {proposer}},
                OrganizationMemberList = new OrganizationMemberList {OrganizationMembers = {enumerable}}
            };
            var result = association.ExecuteMethodWithResult(AssociationMethod.CreateOrganization,
                createInput);
            var organizationAddress =
                Address.Parser.ParseFrom(ByteArrayHelper.HexStringToByteArray(result.ReturnValue));
            var existResult =
                association.CallViewMethod<BoolValue>(AssociationMethod.ValidateOrganizationExist, organizationAddress);
            existResult.Value.ShouldBeTrue();
            var organization =
                association.GetOrganization(organizationAddress);
            organization.OrganizationMemberList.OrganizationMembers.Count.ShouldBe(enumerable.Length);
            organization.ProposerWhiteList.Proposers.Contains(proposer).ShouldBeTrue();

            var newProposer = ConfigNodes.Last().Account.ConvertAddress();
            var changeProposerInput = new ProposerWhiteList
            {
                Proposers = {newProposer}
            };
            var proposalId = association.CreateProposal(association.ContractAddress,
                nameof(AssociationMethod.ChangeOrganizationProposerWhiteList), changeProposerInput, organizationAddress,
                proposer.GetFormatted());
            association.ApproveWithAssociation(proposalId, organizationAddress);
            association.SetAccount(proposer.GetFormatted());
            var release = association.ReleaseProposal(proposalId, proposer.GetFormatted());
            release.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            organization =
                association.GetOrganization(organizationAddress);
            organization.ProposerWhiteList.Proposers.Contains(newProposer).ShouldBeTrue();

            var changeMemberInput = new OrganizationMemberList
            {
                OrganizationMembers = {enumerable.Take(5).ToList()}
            };
            var changeMemberProposalId = association.CreateProposal(association.ContractAddress,
                nameof(AssociationMethod.ChangeOrganizationMember), changeMemberInput, organizationAddress,
                newProposer.GetFormatted());
            association.ApproveWithAssociation(changeMemberProposalId, organizationAddress);
            association.SetAccount(newProposer.GetFormatted());
            var changeMemberRelease = association.ReleaseProposal(changeMemberProposalId, newProposer.GetFormatted());
            changeMemberRelease.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            organization =
                association.GetOrganization(organizationAddress);
            organization.OrganizationMemberList.OrganizationMembers.Count.ShouldBe(5);

            //revert 
            var revertProposerInput = new ProposerWhiteList
            {
                Proposers = {proposer}
            };
            var revertProposalId = association.CreateProposal(association.ContractAddress,
                nameof(AssociationMethod.ChangeOrganizationProposerWhiteList), revertProposerInput, organizationAddress,
                newProposer.GetFormatted());
            association.ApproveWithAssociation(revertProposalId, organizationAddress);
            association.SetAccount(newProposer.GetFormatted());
            var revertRelease = association.ReleaseProposal(revertProposalId, newProposer.GetFormatted());
            revertRelease.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            organization =
                association.GetOrganization(organizationAddress);
            organization.ProposerWhiteList.Proposers.Contains(proposer).ShouldBeTrue();

            var revertMemberInput = new OrganizationMemberList
            {
                OrganizationMembers = {enumerable}
            };
            var revertMemberProposalId = association.CreateProposal(association.ContractAddress,
                nameof(AssociationMethod.ChangeOrganizationMember), revertMemberInput, organizationAddress,
                proposer.GetFormatted());
            association.ApproveWithAssociation(revertMemberProposalId, organizationAddress);
            association.SetAccount(proposer.GetFormatted());
            var revertMemberRelease = association.ReleaseProposal(revertMemberProposalId, proposer.GetFormatted());
            revertMemberRelease.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            organization =
                association.GetOrganization(organizationAddress);
            organization.OrganizationMemberList.OrganizationMembers.Count.ShouldBe(enumerable.Length);
        }
    }
}