using AElf.Automation.Common.Helpers;
using AElfChain.SDK.Models;
using Google.Protobuf.WellKnownTypes;
using Shouldly;

namespace AElf.Automation.Common.Contracts
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
        public ReferendumAuthContract(IApiHelper apiHelper, string callAddress) : base(apiHelper, "AElf.Contracts.ReferendumAuth",
            callAddress)
        {
        }

        public ReferendumAuthContract(IApiHelper apiHelper, string callAddress, string contractAddress) : base(apiHelper,
            contractAddress)
        {
            CallAddress = callAddress;
            UnlockAccount(CallAddress);
        }
        
        public void InitializeReferendum()
        {
            var initializeResult = ExecuteMethodWithResult(ReferendumMethod.Initialize, new Empty());
            if (initializeResult.InfoMsg is TransactionResultDto txDto)
            {
                txDto.Status.ToLower().ShouldBe("mined");
            }
        }
    }
}