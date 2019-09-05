using AElf.Automation.Common.Managers;
using AElf.Contracts.Resource.FeeReceiver;
using AElf.Types;
using AElfChain.SDK.Models;
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
        public FeeReceiverContract(INodeManager nodeManager, string callAddress, string feeReceiverAddress)
            : base(nodeManager, feeReceiverAddress)
        {
            SetAccount(callAddress);
        }

        public FeeReceiverContract(INodeManager nodeManager, string callAddress)
            : base(nodeManager, "AElf.Contracts.Resource.FeeReceiver", callAddress)
        {
        }

        public void InitializeFeeReceiver(Address tokenAddress, Address foundationAddress)
        {
            var initializeResult = ExecuteMethodWithResult(FeeReceiverMethod.Initialize, new InitializeInput
            {
                ElfTokenAddress = tokenAddress,
                FoundationAddress = foundationAddress
            });

            initializeResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        }
    }
}