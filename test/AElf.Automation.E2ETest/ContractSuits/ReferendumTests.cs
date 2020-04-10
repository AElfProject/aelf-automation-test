using System.Collections.Generic;
using System.Linq;
using Acs3;
using AElf.Contracts.Referendum;
using AElf.Types;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using Google.Protobuf.WellKnownTypes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace AElf.Automation.E2ETest.ContractSuits
{
    [TestClass]
    public class ReferendumTests : ContractTestBase
    {
        [TestMethod]
        public void ReferendumCreateTest()
        {
            var token = ContractManager.Token;
            var symbol = token.GetPrimaryTokenSymbol();
            var members = GetMembers(token, symbol);
            var proposer = ConfigNodes.First().Account.ConvertAddress();
            var referendum = ContractManager.Referendum;
            //create referendum organization
            var createInput = new CreateOrganizationInput
            {
                TokenSymbol = symbol,
                ProposalReleaseThreshold = new ProposalReleaseThreshold
                {
                    MaximalAbstentionThreshold = 1000,
                    MaximalRejectionThreshold = 1000,
                    MinimalApprovalThreshold = 2000,
                    MinimalVoteThreshold = 2000
                },
                ProposerWhiteList = new ProposerWhiteList {Proposers = {proposer}}
            };
            var result = referendum.ExecuteMethodWithResult(ReferendumMethod.CreateOrganization,
                createInput);
            var organizationAddress =
                Address.Parser.ParseFrom(ByteArrayHelper.HexStringToByteArray(result.ReturnValue));
            var calculateOrganization =
                referendum.CallViewMethod<Address>(ReferendumMethod.CalculateOrganizationAddress,
                    createInput);
            var organization =
                referendum.GetOrganization(organizationAddress);
            organization.ProposalReleaseThreshold.MaximalAbstentionThreshold.ShouldBe(1000);
            organization.ProposalReleaseThreshold.MaximalRejectionThreshold.ShouldBe(1000);
            organization.ProposalReleaseThreshold.MinimalApprovalThreshold.ShouldBe(2000);
            organization.ProposalReleaseThreshold.MinimalVoteThreshold.ShouldBe(2000);
            organization.ProposerWhiteList.Proposers.Contains(proposer).ShouldBeTrue();
            organization.OrganizationAddress.ShouldBe(calculateOrganization);

            // change organization info:
            var changeInput = new ProposalReleaseThreshold
            {
                MaximalAbstentionThreshold = 1000,
                MaximalRejectionThreshold = 1000,
                MinimalApprovalThreshold = 3000,
                MinimalVoteThreshold = 3000
            };
            //creat proposal
            var proposalId = referendum.CreateProposal(referendum.ContractAddress,
                nameof(ReferendumMethod.ChangeOrganizationThreshold), changeInput,
                organizationAddress, proposer.GetFormatted());
            var proposalInfo = referendum.CheckProposal(proposalId);
            proposalInfo.Proposer.ShouldBe(proposer);
            proposalInfo.ToBeReleased.ShouldBeFalse();
            proposalInfo.OrganizationAddress.ShouldBe(organizationAddress);
            proposalInfo.ContractMethodName.ShouldBe(nameof(ReferendumMethod.ChangeOrganizationThreshold));
            proposalInfo.ToAddress.ShouldBe(referendum.ContractAddress.ConvertAddress());
            proposalInfo.AbstentionCount.ShouldBe(0);
            proposalInfo.ApprovalCount.ShouldBe(0);
            proposalInfo.RejectionCount.ShouldBe(0);

            var memberResult =
                referendum.CallViewMethod<BoolValue>(ReferendumMethod.ValidateProposerInWhiteList,
                    new ValidateProposerInWhiteListInput
                    {
                        OrganizationAddress = organizationAddress,
                        Proposer = proposer
                    });
            memberResult.Value.ShouldBeTrue();

            // approve/abstention/rejection 
            var approveMember = members.First().ConvertAddress();
            var approveMemberBalance =
                token.GetUserBalance(approveMember.GetFormatted(), symbol);
            var approveToken = organization.ProposalReleaseThreshold.MinimalApprovalThreshold;
            var approveTokenResult = token.ApproveToken(approveMember.GetFormatted(), referendum.ContractAddress,
                approveToken, symbol);
            var approveTokenFee = approveTokenResult.GetDefaultTransactionFee();

            referendum.SetAccount(approveMember.GetFormatted());
            var approveResult = referendum.ExecuteMethodWithResult(ReferendumMethod.Approve, proposalId);
            approveResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var approveFee = approveResult.GetDefaultTransactionFee();
            var afterBalance = token.GetUserBalance(approveMember.GetFormatted(), symbol);
            afterBalance.ShouldBe(approveMemberBalance - approveTokenFee - approveFee - approveToken);

            var otherMiners = members.Where(m => !approveMember.GetFormatted().Equals(m)).ToList();
            var abstentionMember = otherMiners.First();
            var abstentionMemberBalance =
                token.GetUserBalance(abstentionMember, symbol);
            var abstainToken = organization.ProposalReleaseThreshold.MaximalAbstentionThreshold;
            approveTokenResult = token.ApproveToken(abstentionMember, referendum.ContractAddress,
                abstainToken,
                symbol);
            approveTokenFee = approveTokenResult.GetDefaultTransactionFee();

            referendum.SetAccount(abstentionMember);
            var abstentionResult = referendum.ExecuteMethodWithResult(ReferendumMethod.Abstain, proposalId);
            abstentionResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var abstainFee = abstentionResult.GetDefaultTransactionFee();
            var afterAbstainBalance = token.GetUserBalance(abstentionMember, symbol);
            afterAbstainBalance.ShouldBe(abstentionMemberBalance - approveTokenFee - abstainFee - abstainToken);

            var rejectionMembers = otherMiners.Where(r => !abstentionMember.Equals(r)).ToList();
            var rejectionMember = rejectionMembers.First();
            var rejectionMemberBalance =
                token.GetUserBalance(rejectionMember, symbol);
            var rejectToken = organization.ProposalReleaseThreshold.MaximalRejectionThreshold;
            approveTokenResult = token.ApproveToken(rejectionMember, referendum.ContractAddress,
                rejectToken, symbol);
            approveTokenFee = approveTokenResult.GetDefaultTransactionFee();

            referendum.SetAccount(rejectionMember);
            var rejectResult = referendum.ExecuteMethodWithResult(ReferendumMethod.Reject, proposalId);
            rejectResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var rejectFee = rejectResult.GetDefaultTransactionFee();
            var afterRejectBalance = token.GetUserBalance(rejectionMember, symbol);
            afterRejectBalance.ShouldBe(rejectionMemberBalance - approveTokenFee - rejectFee - rejectToken);

            proposalInfo = referendum.CheckProposal(proposalId);
            proposalInfo.ToBeReleased.ShouldBeTrue();

            //release 
            var releaseResult = referendum.ReleaseProposal(proposalId, proposer.GetFormatted());
            releaseResult.Status.ShouldBe(TransactionResultStatus.Mined);

            organization =
                referendum.GetOrganization(organizationAddress);
            organization.ProposalReleaseThreshold.MaximalAbstentionThreshold.ShouldBe(1000);
            organization.ProposalReleaseThreshold.MaximalRejectionThreshold.ShouldBe(1000);
            organization.ProposalReleaseThreshold.MinimalApprovalThreshold.ShouldBe(3000);
            organization.ProposalReleaseThreshold.MinimalVoteThreshold.ShouldBe(3000);

            //reclaimVoteToken
            referendum.SetAccount(approveMember.GetFormatted());
            var beforeBalance = token.GetUserBalance(approveMember.GetFormatted(), symbol);
            var approveReclaim = referendum.ExecuteMethodWithResult(ReferendumMethod.ReclaimVoteToken, proposalId);
            approveReclaim.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var reclaimFee = approveReclaim.GetDefaultTransactionFee();
            var afterApproveReclaimBalance = token.GetUserBalance(approveMember.GetFormatted(), symbol);
            afterApproveReclaimBalance.ShouldBe(beforeBalance + approveToken - reclaimFee);

            referendum.SetAccount(abstentionMember);
            beforeBalance = token.GetUserBalance(abstentionMember, symbol);
            var abstainReclaim = referendum.ExecuteMethodWithResult(ReferendumMethod.ReclaimVoteToken, proposalId);
            abstainReclaim.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            reclaimFee = abstainReclaim.GetDefaultTransactionFee();
            var afterAbstainReclaimBalance = token.GetUserBalance(abstentionMember, symbol);
            afterAbstainReclaimBalance.ShouldBe(beforeBalance + abstainToken - reclaimFee);

            referendum.SetAccount(rejectionMember);
            beforeBalance = token.GetUserBalance(rejectionMember, symbol);
            var rejectReclaim = referendum.ExecuteMethodWithResult(ReferendumMethod.ReclaimVoteToken, proposalId);
            rejectReclaim.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            reclaimFee = rejectReclaim.GetDefaultTransactionFee();
            var afterRejectReclaimBalance = token.GetUserBalance(rejectionMember, symbol);
            afterRejectReclaimBalance.ShouldBe(beforeBalance + rejectToken - reclaimFee);

            //revert
            var revertInput = new ProposalReleaseThreshold
            {
                MaximalAbstentionThreshold = 1000,
                MaximalRejectionThreshold = 1000,
                MinimalApprovalThreshold = 2000,
                MinimalVoteThreshold = 2000
            };
            //creat proposal
            var revertProposalId = referendum.CreateProposal(referendum.ContractAddress,
                nameof(ReferendumMethod.ChangeOrganizationThreshold), revertInput,
                organizationAddress, proposer.GetFormatted());
            organization =
                referendum.GetOrganization(organizationAddress);
            approveToken = organization.ProposalReleaseThreshold.MinimalApprovalThreshold;
            token.ApproveToken(approveMember.GetFormatted(), referendum.ContractAddress, approveToken,
                symbol);
            referendum.SetAccount(approveMember.GetFormatted());
            var approveRevertResult = referendum.ExecuteMethodWithResult(ReferendumMethod.Approve, revertProposalId);
            approveRevertResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            referendum.SetAccount(proposer.GetFormatted());
            var revertRelease = referendum.ReleaseProposal(revertProposalId, proposer.GetFormatted());
            revertRelease.Status.ShouldBe(TransactionResultStatus.Mined);

            organization =
                referendum.GetOrganization(organizationAddress);
            organization.ProposalReleaseThreshold.MaximalAbstentionThreshold.ShouldBe(1000);
            organization.ProposalReleaseThreshold.MaximalRejectionThreshold.ShouldBe(1000);
            organization.ProposalReleaseThreshold.MinimalApprovalThreshold.ShouldBe(2000);
            organization.ProposalReleaseThreshold.MinimalVoteThreshold.ShouldBe(2000);
        }

        [TestMethod]
        public void ReferendumChangeWhiteList()
        {
            var members = ConfigNodes.Select(l => l.Account).ToList().Select(member => member.ConvertAddress());
            var proposer = ConfigNodes.First().Account.ConvertAddress();
            var referendum = ContractManager.Referendum;
            var token = ContractManager.Token;
            var symbol = token.GetPrimaryTokenSymbol();
            //create referendum organization
            var enumerable = members as Address[] ?? members.ToArray();
            var createInput = new CreateOrganizationInput
            {
                TokenSymbol = symbol,
                ProposalReleaseThreshold = new ProposalReleaseThreshold
                {
                    MaximalAbstentionThreshold = 300,
                    MaximalRejectionThreshold = 300,
                    MinimalApprovalThreshold = 500,
                    MinimalVoteThreshold = 500
                },
                ProposerWhiteList = new ProposerWhiteList {Proposers = {proposer}}
            };
            var result = referendum.ExecuteMethodWithResult(ReferendumMethod.CreateOrganization,
                createInput);
            var organizationAddress =
                Address.Parser.ParseFrom(ByteArrayHelper.HexStringToByteArray(result.ReturnValue));
            var existResult =
                referendum.CallViewMethod<BoolValue>(ReferendumMethod.ValidateOrganizationExist, organizationAddress);
            existResult.Value.ShouldBeTrue();
            var organization =
                referendum.GetOrganization(organizationAddress);
            organization.ProposerWhiteList.Proposers.Contains(proposer).ShouldBeTrue();

            var newProposer = ConfigNodes.Last().Account.ConvertAddress();
            var changeProposerInput = new ProposerWhiteList
            {
                Proposers = {newProposer}
            };
            var proposalId = referendum.CreateProposal(referendum.ContractAddress,
                nameof(ReferendumMethod.ChangeOrganizationProposerWhiteList), changeProposerInput, organizationAddress,
                proposer.GetFormatted());

            var approveMember = enumerable.First();
            var approveToken = organization.ProposalReleaseThreshold.MinimalApprovalThreshold;
            token.ApproveToken(approveMember.GetFormatted(), referendum.ContractAddress,
                approveToken, symbol);
            referendum.SetAccount(approveMember.GetFormatted());
            var approveResult = referendum.ExecuteMethodWithResult(ReferendumMethod.Approve, proposalId);
            approveResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var release = referendum.ReleaseProposal(proposalId, proposer.GetFormatted());
            release.Status.ShouldBe(TransactionResultStatus.Mined);
            organization =
                referendum.GetOrganization(organizationAddress);
            organization.ProposerWhiteList.Proposers.Contains(newProposer).ShouldBeTrue();

            //revert 
            var revertProposerInput = new ProposerWhiteList
            {
                Proposers = {proposer}
            };
            var revertProposalId = referendum.CreateProposal(referendum.ContractAddress,
                nameof(ReferendumMethod.ChangeOrganizationProposerWhiteList), revertProposerInput, organizationAddress,
                newProposer.GetFormatted());

            var otherApproveMember = enumerable.Last();
            var approveTokenResult = token.ApproveToken(otherApproveMember.GetFormatted(), referendum.ContractAddress,
                approveToken, symbol);
            approveTokenResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            referendum.SetAccount(otherApproveMember.GetFormatted());
            approveResult = referendum.ExecuteMethodWithResult(ReferendumMethod.Approve, revertProposalId);
            approveResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var revertRelease = referendum.ReleaseProposal(revertProposalId, newProposer.GetFormatted());
            revertRelease.Status.ShouldBe(TransactionResultStatus.Mined);
            organization =
                referendum.GetOrganization(organizationAddress);
            organization.ProposerWhiteList.Proposers.Contains(proposer).ShouldBeTrue();
        }

        private List<string> GetMembers(TokenContract token, string symbol)
        {
            var members = EnvCheck.GenerateOrGetTestUsers(10);
            foreach (var m in members)
            {
                var balance = token.GetUserBalance(m, symbol);
                if (balance >= 1000_00000000) continue;
                ContractManager.Token.TransferBalance(ContractManager.CallAddress, m, 1000_00000000,
                    symbol);
            }

            return members;
        }
    }
}