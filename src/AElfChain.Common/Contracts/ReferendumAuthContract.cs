using Acs3;
using AElf.Client.Dto;
using AElf.Contracts.Referendum;
using AElf.CSharp.Core.Extension;
using AElf.Types;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using Google.Protobuf;
using Shouldly;
using Volo.Abp.Threading;

namespace AElfChain.Common.Contracts
{
    public enum ReferendumMethod
    {
        //View
        GetOrganization,
        GetProposal,
        CalculateOrganizationAddress,
        ValidateOrganizationExist,
        ValidateProposerInWhiteList,

        //Action
        Initialize,
        CreateOrganization,
        Approve,
        CreateProposal,
        Release,
        ReclaimVoteToken,
        Abstain,
        Reject,
        ChangeOrganizationThreshold,
        ChangeOrganizationProposerWhiteList
    }

    public class ReferendumAuthContract : BaseContract<ReferendumMethod>
    {
        public ReferendumAuthContract(INodeManager nodeManager, string callAddress, string electionAddress)
            : base(nodeManager, electionAddress)
        {
            SetAccount(callAddress);
        }

        public ReferendumAuthContract(INodeManager nodeManager, string callAddress)
            : base(nodeManager, "AElf.Contracts.ReferendumAuth", callAddress)
        {
        }

        public Hash CreateProposal(string contractAddress, string method, IMessage input, Address organizationAddress,
            string caller = null)
        {
            var tester = GetTestStub<ReferendumContractContainer.ReferendumContractStub>(caller);
            var createProposalInput = new CreateProposalInput
            {
                ContractMethodName = method,
                ToAddress = contractAddress.ConvertAddress(),
                Params = input.ToByteString(),
                ExpiredTime = KernelHelper.GetUtcNow().AddMinutes(10),
                OrganizationAddress = organizationAddress
            };
            var proposal = AsyncHelper.RunSync(() => tester.CreateProposal.SendAsync(createProposalInput));
            proposal.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined,
                proposal.TransactionResult.TransactionId.ToHex);
            var proposalId = proposal.Output;
            Logger.Info($"Proposal {proposalId} created success by {caller ?? CallAddress}.");
            return proposalId;
        }

        public TransactionResult ReleaseProposal(Hash proposalId, string caller = null)
        {
            var tester = GetTestStub<ReferendumContractContainer.ReferendumContractStub>(caller);
            var result = AsyncHelper.RunSync(() => tester.Release.SendAsync(proposalId));
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            Logger.Info($"Proposal {proposalId} release success by {caller ?? CallAddress}");

            return result.TransactionResult;
        }

        public TransactionResultDto Approve(Hash proposalId, string caller)
        {
            SetAccount(caller);
            return ExecuteMethodWithResult(ReferendumMethod.Approve, proposalId);
        }

        public TransactionResultDto Abstain(Hash proposalId, string caller)
        {
            SetAccount(caller);
            return ExecuteMethodWithResult(ReferendumMethod.Abstain, proposalId);
        }

        public TransactionResultDto Reject(Hash proposalId, string caller)
        {
            SetAccount(caller);
            return ExecuteMethodWithResult(ReferendumMethod.Reject, proposalId);
        }

        public Organization GetOrganization(Address organization)
        {
            return CallViewMethod<Organization>(ReferendumMethod.GetOrganization, organization);
        }

        public ProposalOutput CheckProposal(Hash proposalId)
        {
            return CallViewMethod<ProposalOutput>(ReferendumMethod.GetProposal,
                proposalId);
        }
    }
}