using System;
using System.Linq;
using Acs3;
using AElf.Contracts.Parliament;
using AElf.Types;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using Google.Protobuf.WellKnownTypes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace AElf.Automation.E2ETest.ContractSuits
{
    [TestClass]
    public class ParliamentTests : ContractTestBase
    {
        [TestMethod]
        public void ParliamentCreateTest()
        {
            var parliament = ContractManager.ParliamentAuth;
            //create parliament organization
            var createInput = new CreateOrganizationInput
            {
                ProposalReleaseThreshold = new ProposalReleaseThreshold
                {
                    MaximalAbstentionThreshold = 3000,
                    MaximalRejectionThreshold = 3000,
                    MinimalApprovalThreshold = 5000,
                    MinimalVoteThreshold = 10000
                },
                ProposerAuthorityRequired = true,
                ParliamentMemberProposingAllowed = true
            };
            var result = parliament.ExecuteMethodWithResult(ParliamentMethod.CreateOrganization,
                createInput);
            var organizationAddress = result.ReadableReturnValue.Replace("\"", "");
            var calculateOrganization =
                parliament.CallViewMethod<Address>(ParliamentMethod.CalculateOrganizationAddress,
                    createInput);

            var organization =
                parliament.GetOrganization(organizationAddress.ConvertAddress());
            organization.ProposalReleaseThreshold.MaximalAbstentionThreshold.ShouldBe(3000);
            organization.ProposalReleaseThreshold.MaximalRejectionThreshold.ShouldBe(3000);
            organization.ProposalReleaseThreshold.MinimalApprovalThreshold.ShouldBe(5000);
            organization.ProposalReleaseThreshold.MinimalVoteThreshold.ShouldBe(10000);
            organization.ProposerAuthorityRequired.ShouldBeTrue();
            organization.ParliamentMemberProposingAllowed.ShouldBeTrue();
            organization.OrganizationAddress.ShouldBe(calculateOrganization);

            // change organization info:
            var changeInput = new ProposalReleaseThreshold
                {
                    MaximalAbstentionThreshold = 2000,
                    MaximalRejectionThreshold = 3000,
                    MinimalApprovalThreshold = 5000,
                    MinimalVoteThreshold = 10000
                };
            var miners = ContractManager.Authority.GetCurrentMiners();
            //creat proposal
            var proposalId = parliament.CreateProposal(parliament.ContractAddress,
                nameof(ParliamentMethod.ChangeOrganizationThreshold), changeInput, organizationAddress.ConvertAddress(),miners.First());
            var proposalInfo = parliament.CheckProposal(proposalId);
            proposalInfo.Proposer.ShouldBe(miners.First().ConvertAddress());
            proposalInfo.ToBeReleased.ShouldBeFalse();
            proposalInfo.OrganizationAddress.ShouldBe(organizationAddress.ConvertAddress());
            proposalInfo.ContractMethodName.ShouldBe(nameof(ParliamentMethod.ChangeOrganizationThreshold));
            proposalInfo.ToAddress.ShouldBe(parliament.ContractAddress.ConvertAddress());
            proposalInfo.AbstentionCount.ShouldBe(0);
            proposalInfo.ApprovalCount.ShouldBe(0);
            proposalInfo.RejectionCount.ShouldBe(0);
            
            var memberResult =
                parliament.CallViewMethod<BoolValue>(ParliamentMethod.ValidateAddressIsParliamentMember,
                    miners.First().ConvertAddress());
            memberResult.Value.ShouldBeTrue();

            // approve/abstention/rejection 
            var minimalApprovalThreshold = organization.ProposalReleaseThreshold.MinimalApprovalThreshold;
            var approveCount = Math.Ceiling(miners.Count * minimalApprovalThreshold / (double) 10000);
            var approveMiners = miners.Take((int)approveCount).ToList();
            foreach (var miner in approveMiners)
            {
                parliament.SetAccount(miner);
                var approveResult = parliament.ExecuteMethodWithResult(ParliamentMethod.Approve,proposalId);
                approveResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            }

            var otherMiners = miners.Where(m => !approveMiners.Contains(m)).ToList();
            var abstentionMiner = otherMiners.First();
            parliament.SetAccount(abstentionMiner);
            var abstentionResult = parliament.ExecuteMethodWithResult(ParliamentMethod.Abstain,proposalId);
            abstentionResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            
            var rejectionMiners = otherMiners.Where(r => !abstentionMiner.Contains(r)).ToList();
            foreach (var rm in rejectionMiners)
            {
                parliament.SetAccount(rm);
                var rejectResult = parliament.ExecuteMethodWithResult(ParliamentMethod.Reject,proposalId);
                rejectResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            }
            
            proposalInfo = parliament.CheckProposal(proposalId);
            proposalInfo.ToBeReleased.ShouldBeTrue();
            proposalInfo.AbstentionCount.ShouldBe(1);
            proposalInfo.ApprovalCount.ShouldBe(approveMiners.Count);
            proposalInfo.RejectionCount.ShouldBe(rejectionMiners.Count);
            
            //release 
            var releaseResult = parliament.ReleaseProposal(proposalId, miners.First());
            releaseResult.Status.ShouldBe(TransactionResultStatus.Mined);
            
            organization =
                parliament.GetOrganization(organizationAddress.ConvertAddress());
            organization.ProposalReleaseThreshold.MaximalAbstentionThreshold.ShouldBe(2000);
            organization.ProposalReleaseThreshold.MaximalRejectionThreshold.ShouldBe(3000);
            organization.ProposalReleaseThreshold.MinimalApprovalThreshold.ShouldBe(5000);
            organization.ProposalReleaseThreshold.MinimalVoteThreshold.ShouldBe(10000);
            organization.ProposerAuthorityRequired.ShouldBeTrue();
            organization.ParliamentMemberProposingAllowed.ShouldBeTrue();
            
            //revert
            var revertInput = new ProposalReleaseThreshold
            {
                MaximalAbstentionThreshold = 3000,
                MaximalRejectionThreshold = 3000,
                MinimalApprovalThreshold = 5000,
                MinimalVoteThreshold = 10000
            };
            parliament.SetAccount(miners.First());
            var revertProposalId = parliament.CreateProposal(parliament.ContractAddress,
                nameof(ParliamentMethod.ChangeOrganizationThreshold), revertInput, organizationAddress.ConvertAddress(),miners.First());
            // approve/abstention/rejection 
            parliament.MinersApproveProposal(revertProposalId, miners);
            var releaseRevert = parliament.ReleaseProposal(revertProposalId, miners.First());
            releaseRevert.Status.ShouldBe(TransactionResultStatus.Mined);
            organization =
                parliament.GetOrganization(organizationAddress.ConvertAddress());
            organization.ProposalReleaseThreshold.MaximalAbstentionThreshold.ShouldBe(3000);
        }
        
        [TestMethod]
        public void ParliamentChangeWhiteList()
        {
            var parliament = ContractManager.ParliamentAuth;
            var defaultAddress = parliament.GetGenesisOwnerAddress();
            var existResult =
                parliament.CallViewMethod<BoolValue>(ParliamentMethod.ValidateOrganizationExist, defaultAddress);
            existResult.Value.ShouldBeTrue();
            var proposalWhiteList =
                parliament.CallViewMethod<GetProposerWhiteListContextOutput>(
                    ParliamentMethod.GetProposerWhiteListContext, new Empty());
            proposalWhiteList.ShouldBe(new GetProposerWhiteListContextOutput());
            var miners = ContractManager.Authority.GetCurrentMiners();

            var changeInput = new ProposerWhiteList
            {
                Proposers = { miners.First().ConvertAddress()}
            };

            var proposalId = parliament.CreateProposal(parliament.ContractAddress,
                nameof(ParliamentMethod.ChangeOrganizationProposerWhiteList), changeInput,defaultAddress,miners.First());
            parliament.MinersApproveProposal(proposalId,miners);
            parliament.SetAccount(miners.First());
            var release = parliament.ReleaseProposal(proposalId,miners.First());
            release.Status.ShouldBe(TransactionResultStatus.Mined);
            
            proposalWhiteList =
                parliament.CallViewMethod<GetProposerWhiteListContextOutput>(
                    ParliamentMethod.GetProposerWhiteListContext, new Empty());
            proposalWhiteList.Proposers.Contains(miners.First().ConvertAddress()).ShouldBeTrue();
            
            //revert
            var revertInput = new ProposerWhiteList
            {
                Proposers = {}
            };
            var revertProposalId = parliament.CreateProposal(parliament.ContractAddress,
                nameof(ParliamentMethod.ChangeOrganizationProposerWhiteList), revertInput,defaultAddress,miners.First());
            parliament.MinersApproveProposal(revertProposalId,miners);
            parliament.SetAccount(miners.First());
            var revertRelease = parliament.ReleaseProposal(revertProposalId,miners.First());
            revertRelease.Status.ShouldBe(TransactionResultStatus.Mined);
            proposalWhiteList =
                parliament.CallViewMethod<GetProposerWhiteListContextOutput>(
                    ParliamentMethod.GetProposerWhiteListContext, new Empty());
            proposalWhiteList.ShouldBe(new GetProposerWhiteListContextOutput());
        }
    }
}