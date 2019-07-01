using AElf.Automation.Common.Helpers;
using AElf.Automation.Common.WebApi.Dto;
using AElf.Contracts.Resource.FeeReceiver;
using AElf.Types;
using Shouldly;

namespace AElf.Automation.Common.Contracts
{
    public enum FeeReceiverMethod
    {
        //action
        Initialize,
        Withdraw,
        WithdrawAll,
        Burn,

        //view
        GetElfTokenAddress,
        GetFoundationAddress,
        GetOwedToFoundation
    }

    public class FeeReceiverContract : BaseContract<FeeReceiverMethod>
    {
        public FeeReceiverContract(IApiHelper apiHelper, string callAddress, string feeReceiverAddress) :
            base(apiHelper, feeReceiverAddress)
        {
            CallAddress = callAddress;
            UnlockAccount(CallAddress);
        }

        public FeeReceiverContract(IApiHelper apiHelper, string callAddress)
            : base(apiHelper, "AElf.Contracts.Resource.FeeReceiver", callAddress)
        {
        }

        public void InitializeFeeReceiver(Address tokenAddress, Address foundationAddress)
        {
            var initializeResult = ExecuteMethodWithResult(FeeReceiverMethod.Initialize, new InitializeInput
            {
                ElfTokenAddress = tokenAddress,
                FoundationAddress = foundationAddress
            });
            if (initializeResult.InfoMsg is TransactionResultDto txDto)
            {
                txDto.Status.ShouldBe("Mined");
            }
        }
    }
}