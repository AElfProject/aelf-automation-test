using AElfChain.Common.Managers;
using AElfChain.SDK.Models;
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
        ReclaimVoteToken
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

        public void InitializeReferendum()
        {
            var initializeResult = ExecuteMethodWithResult(ReferendumMethod.Initialize, new Empty());
            if (initializeResult is TransactionResultDto txDto) txDto.Status.ToLower().ShouldBe("mined");
        }
    }
}