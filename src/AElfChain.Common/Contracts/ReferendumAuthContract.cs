using Acs3;
using AElf.Client.Dto;
using AElf.Contracts.Referendum;
using AElf.Types;
using AElfChain.Common.Managers;
using Google.Protobuf.WellKnownTypes;
using Shouldly;

namespace AElfChain.Common.Contracts
{
    public enum ReferendumMethod
    {
        //View
        GetOrganization,
        GetProposal,

        //Action
        Initialize,
        CreateOrganization,
        Approve,
        CreateProposal,
        Release,
        ReclaimVoteToken,
        Abstain,
        Reject
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