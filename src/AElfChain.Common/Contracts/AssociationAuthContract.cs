using System.Linq;
using Acs3;
using AElf;
using AElf.Client.Dto;
using AElf.Contracts.Association;
using AElf.CSharp.Core.Extension;
using AElf.Types;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;

namespace AElfChain.Common.Contracts
{
    public enum AssociationMethod
    {
        //View
        GetOrganization,
        GetProposal,
        CalculateOrganizationAddress,
        ValidateProposerInWhiteList,
        ValidateOrganizationExist,

        //Action
        CreateOrganization,
        Approve,
        CreateProposal,
        Release,
        Abstain,
        Reject,
        ChangeOrganizationThreshold,
        ChangeOrganizationMember,
        ChangeOrganizationProposerWhiteList,
        ChangeMethodFeeController
    }

    public class AssociationAuthContract : BaseContract<AssociationMethod>
    {
        public AssociationAuthContract(INodeManager nodeManager, string callAddress, string electionAddress)
            : base(nodeManager, electionAddress)
        {
            SetAccount(callAddress);
        }

        public AssociationAuthContract(INodeManager nodeManager, string callAddress)
            : base(nodeManager, "AElf.Contracts.AssociationAuth", callAddress)
        {
        }

        public ProposalOutput CheckProposal(Hash proposalId)
        {
            return CallViewMethod<ProposalOutput>(AssociationMethod.GetProposal,
                proposalId);
        }

        public Hash CreateProposal(string contractAddress, string method, IMessage input, Address organizationAddress,
            string proposer)
        {
            var createProposalInput = new CreateProposalInput
            {
                ContractMethodName = method,
                ToAddress = contractAddress.ConvertAddress(),
                Params = input.ToByteString(),
                ExpiredTime = KernelHelper.GetUtcNow().AddDays(1),
                OrganizationAddress = organizationAddress
            };
            SetAccount(proposer);
            var proposal = ExecuteMethodWithResult(AssociationMethod.CreateProposal, createProposalInput);
            proposal.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var proposalId = Hash.Parser.ParseFrom(ByteArrayHelper.HexStringToByteArray(proposal.ReturnValue));
            Logger.Info($"Proposal {proposalId} created success by {proposer}.");

            return proposalId;
        }

        public void ApproveProposal(Hash proposalId, string member)
        {
            SetAccount(member);
            var transactionResult = ExecuteMethodWithResult(AssociationMethod.Approve, proposalId);
            transactionResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            Logger.Info($"Proposal {proposalId} approved success by {member}");
        }

        public TransactionResultDto ReleaseProposal(Hash proposalId, string proposer)
        {
            SetAccount(proposer);
            var transactionResult = ExecuteMethodWithResult(AssociationMethod.Release, proposalId);
            transactionResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            Logger.Info($"Proposal {proposalId} release success by {proposer}");
            return transactionResult;
        }

        public Address CalculateOrganizationAddress(IMessage input)
        {
            return CallViewMethod<Address>(AssociationMethod.CalculateOrganizationAddress, input);
        }

        public Address CreateOrganization(IMessage input)
        {
            var result = ExecuteMethodWithResult(AssociationMethod.CreateOrganization, input);
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var returnValue = result.ReturnValue;
            var organizationAddress = Address.Parser.ParseFrom(ByteArrayHelper.HexStringToByteArray(returnValue));
            return organizationAddress;
        }

        public Organization GetOrganization(Address organization)
        {
            return
                CallViewMethod<Organization>(AssociationMethod.GetOrganization, organization);
        }

        public string Approve(Hash proposalId, string caller)
        {
            SetAccount(caller);
            return ExecuteMethodWithTxId(AssociationMethod.Approve, proposalId);
        }

        public TransactionResultDto ApproveWithResult(Hash proposalId, string caller)
        {
            SetAccount(caller);
            return ExecuteMethodWithResult(AssociationMethod.Approve, proposalId);
        }

        public string Abstain(Hash proposalId, string caller)
        {
            SetAccount(caller);
            return ExecuteMethodWithTxId(AssociationMethod.Abstain, proposalId);
        }

        public string Reject(Hash proposalId, string caller)
        {
            SetAccount(caller);
            return ExecuteMethodWithTxId(AssociationMethod.Reject, proposalId);
        }

        public void ApproveWithAssociation(Hash proposalId, Address association)
        {
            var organization = CallViewMethod<Organization>(AssociationMethod.GetOrganization,
                association);
            var members = organization.OrganizationMemberList.OrganizationMembers.ToList();
            foreach (var member in members)
            {
                SetAccount(member.GetFormatted());
                var approve = ExecuteMethodWithResult(AssociationMethod.Approve, proposalId);
                approve.Status.ShouldBe("MINED");
                if (CheckProposal(proposalId).ToBeReleased) return;
            }
        }

        public BoolValue ValidateOrganizationExist(Address address)
        {
            return CallViewMethod<BoolValue>(AssociationMethod.ValidateOrganizationExist, address);
        }
    }
}