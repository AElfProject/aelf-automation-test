using System;
using System.Linq;
using Acs3;
using AElf.Contracts.Parliament;
using AElf.CSharp.Core.Extension;
using AElf.Types;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using Google.Protobuf;
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
                    MaximalAbstentionThreshold = 2500,
                    MaximalRejectionThreshold = 2500,
                    MinimalApprovalThreshold = 7500,
                    MinimalVoteThreshold = 10000
                },
                ProposerAuthorityRequired = true,
                ParliamentMemberProposingAllowed = true
            };
            var miners = ContractManager.Authority.GetCurrentMiners();
            parliament.SetAccount(miners.First());
            var result = parliament.ExecuteMethodWithResult(ParliamentMethod.CreateOrganization,
                createInput);
            var organizationAddress =
                Address.Parser.ParseFrom(ByteArrayHelper.HexStringToByteArray(result.ReturnValue));
            var calculateOrganization =
                parliament.CallViewMethod<Address>(ParliamentMethod.CalculateOrganizationAddress,
                    createInput);

            var organization =
                parliament.GetOrganization(organizationAddress);
            organization.ProposalReleaseThreshold.MaximalAbstentionThreshold.ShouldBe(2500);
            organization.ProposalReleaseThreshold.MaximalRejectionThreshold.ShouldBe(2500);
            organization.ProposalReleaseThreshold.MinimalApprovalThreshold.ShouldBe(7500);
            organization.ProposalReleaseThreshold.MinimalVoteThreshold.ShouldBe(10000);
            organization.ProposerAuthorityRequired.ShouldBeTrue();
            organization.ParliamentMemberProposingAllowed.ShouldBeTrue();
            organization.OrganizationAddress.ShouldBe(calculateOrganization);

            // change organization info:
            var changeInput = new ProposalReleaseThreshold
            {
                MaximalAbstentionThreshold = 5000,
                MaximalRejectionThreshold = 5000,
                MinimalApprovalThreshold = 5000,
                MinimalVoteThreshold = 10000
            };
            //creat proposal
            var proposalId = parliament.CreateProposal(parliament.ContractAddress,
                nameof(ParliamentMethod.ChangeOrganizationThreshold), changeInput, organizationAddress, miners.First());
            var proposalInfo = parliament.CheckProposal(proposalId);
            proposalInfo.Proposer.ShouldBe(miners.First().ConvertAddress());
            proposalInfo.ToBeReleased.ShouldBeFalse();
            proposalInfo.OrganizationAddress.ShouldBe(organizationAddress);
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
            var approveMiners = miners.Take((int) approveCount).ToList();
            foreach (var miner in approveMiners)
            {
                parliament.SetAccount(miner);
                var approveResult = parliament.ExecuteMethodWithResult(ParliamentMethod.Approve, proposalId);
                approveResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            }

            var otherMiners = miners.Where(m => !approveMiners.Contains(m)).ToList();
            var abstentionMiner = otherMiners.First();
            parliament.SetAccount(abstentionMiner);
            var abstentionResult = parliament.ExecuteMethodWithResult(ParliamentMethod.Abstain, proposalId);
            abstentionResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var rejectionMiners = otherMiners.Where(r => !abstentionMiner.Contains(r)).ToList();
            foreach (var rm in rejectionMiners)
            {
                parliament.SetAccount(rm);
                var rejectResult = parliament.ExecuteMethodWithResult(ParliamentMethod.Reject, proposalId);
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
                parliament.GetOrganization(organizationAddress);
            organization.ProposalReleaseThreshold.MaximalAbstentionThreshold.ShouldBe(5000);
            organization.ProposalReleaseThreshold.MaximalRejectionThreshold.ShouldBe(5000);
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
                nameof(ParliamentMethod.ChangeOrganizationThreshold), revertInput, organizationAddress, miners.First());
            // approve/abstention/rejection 
            parliament.MinersApproveProposal(revertProposalId, miners);
            var releaseRevert = parliament.ReleaseProposal(revertProposalId, miners.First());
            releaseRevert.Status.ShouldBe(TransactionResultStatus.Mined);
            organization =
                parliament.GetOrganization(organizationAddress);
            organization.ProposalReleaseThreshold.MaximalAbstentionThreshold.ShouldBe(3000);
        }

        [TestMethod]
        public void ParliamentChangeWhiteList_False()
        {
            var parliament = ContractManager.ParliamentAuth;
            var miners = ContractManager.Authority.GetCurrentMiners();
            //create parliament organization
            var createInput = new CreateOrganizationInput
            {
                ProposalReleaseThreshold = new ProposalReleaseThreshold
                {
                    MaximalAbstentionThreshold = 1000,
                    MaximalRejectionThreshold = 1000,
                    MinimalApprovalThreshold = 3000,
                    MinimalVoteThreshold = 3000
                },
                ProposerAuthorityRequired = true,
                ParliamentMemberProposingAllowed = true
            };
            var proposer = miners.First();
            parliament.SetAccount(proposer);
            var result = parliament.ExecuteMethodWithResult(ParliamentMethod.CreateOrganization,
                createInput);
            var organizationAddress =
                Address.Parser.ParseFrom(ByteArrayHelper.HexStringToByteArray(result.ReturnValue));
            var proposalWhiteList =
                parliament.CallViewMethod<ProposerWhiteList>(
                    ParliamentMethod.GetProposerWhiteList, new Empty());
            proposalWhiteList.ShouldBe(new ProposerWhiteList());

            var changeInput = new ProposerWhiteList
            {
                Proposers = {miners.First().ConvertAddress()}
            };
            parliament.SetAccount(proposer);
            var returnProposal = parliament.ExecuteMethodWithResult(ParliamentMethod.CreateProposal,
                new CreateProposalInput
                {
                    ToAddress = parliament.Contract,
                    ContractMethodName = nameof(ParliamentMethod.ChangeOrganizationProposerWhiteList),
                    ExpiredTime = KernelHelper.GetUtcNow().AddMinutes(10),
                    OrganizationAddress = organizationAddress,
                    Params = changeInput.ToByteString()
                });
            var proposalId = Hash.Parser.ParseFrom(ByteArrayHelper.HexStringToByteArray(returnProposal.ReturnValue));
            var proposalInfo = parliament.CheckProposal(proposalId);
            proposalInfo.Proposer.ShouldBe(proposer.ConvertAddress());

            parliament.MinersApproveProposal(proposalId, miners);
            var release = parliament.ExecuteMethodWithResult(ParliamentMethod.Release, proposalId);
            release.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Failed);
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
                parliament.CallViewMethod<ProposerWhiteList>(
                    ParliamentMethod.GetProposerWhiteList, new Empty());
            proposalWhiteList.ShouldBe(new ProposerWhiteList());
            var miners = ContractManager.Authority.GetCurrentMiners();

            var changeInput = new ProposerWhiteList
            {
                Proposers = {miners.First().ConvertAddress()}
            };

            var proposalId = parliament.CreateProposal(parliament.ContractAddress,
                nameof(ParliamentMethod.ChangeOrganizationProposerWhiteList), changeInput, defaultAddress,
                miners.First());
            parliament.MinersApproveProposal(proposalId, miners);
            parliament.SetAccount(miners.First());
            var release = parliament.ReleaseProposal(proposalId, miners.First());
            release.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        [TestMethod]
        public void ParliamentChangeReleaseThreshold()
        {
            var parliament = ContractManager.ParliamentAuth;
            var defaultAddress = parliament.GetGenesisOwnerAddress();
            var existResult =
                parliament.CallViewMethod<BoolValue>(ParliamentMethod.ValidateOrganizationExist, defaultAddress);
            existResult.Value.ShouldBeTrue();
            var changeInput = new ProposalReleaseThreshold
            {
                MaximalAbstentionThreshold = 1000,
                MaximalRejectionThreshold = 1000,
                MinimalApprovalThreshold = 1000,
                MinimalVoteThreshold = 1000
            };
            //creat proposal
            var miners = ContractManager.Authority.GetCurrentMiners();
            var proposalId = parliament.CreateProposal(parliament.ContractAddress,
                nameof(ParliamentMethod.ChangeOrganizationThreshold), changeInput, defaultAddress, miners.First());
            parliament.MinersApproveProposal(proposalId, miners);
            parliament.SetAccount(miners.First());
            var release = parliament.ReleaseProposal(proposalId, miners.First());
            release.Status.ShouldBe(TransactionResultStatus.Mined);
        }
    }
}