using Acs3;
using AElf;
using AElf.Client.Dto;
using AElf.Contracts.Association;
using AElf.Kernel;
using AElf.Sdk.CSharp;
using AElf.Types;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Managers;
using Google.Protobuf;
using Shouldly;
using Volo.Abp.Threading;

namespace AElfChain.Common.Contracts
{
    public enum AssociationMethod
    {
        //View
        GetOrganization,
        GetProposal,
        CalculateOrganizationAddress,

        //Action
        CreateOrganization,
        Approve,
        CreateProposal,
        Release,
        Abstain,
        Reject
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
                ExpiredTime = TimestampHelper.GetUtcNow().AddDays(1),
                OrganizationAddress = organizationAddress
            };
            SetAccount(proposer);
            var proposal = ExecuteMethodWithResult(AssociationMethod.CreateProposal, createProposalInput);
            proposal.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var returnValue = proposal.ReadableReturnValue.Replace("\"", "");
            Logger.Info($"Proposal {returnValue} created success by {proposer}.");
            var proposalId =
                HashHelper.HexStringToHash(returnValue);

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
            return AddressHelper.Base58StringToAddress(
                ExecuteMethodWithResult(AssociationMethod.CreateOrganization, input).ReadableReturnValue
                    .Replace("\"", ""));
        }
        
        public Organization GetOrganization(Address organization)
        {
            return 
                CallViewMethod<Organization>(AssociationMethod.GetOrganization, organization);
        }
        
        public TransactionResultDto Approve(Hash proposalId, string caller)
        {
            SetAccount(caller);
            return ExecuteMethodWithResult(AssociationMethod.Approve, proposalId);
        }
        
        public TransactionResultDto Abstain(Hash proposalId, string caller)
        {
            SetAccount(caller);
            return ExecuteMethodWithResult(AssociationMethod.Abstain, proposalId);
        }
        
        public TransactionResultDto Reject(Hash proposalId, string caller)
        {
            SetAccount(caller);
            return ExecuteMethodWithResult(AssociationMethod.Reject, proposalId);
        }
    }
}