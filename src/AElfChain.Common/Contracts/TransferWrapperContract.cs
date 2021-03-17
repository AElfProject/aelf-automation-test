using AElfChain.Common.Managers;

namespace AElfChain.Common.Contracts
{
    public enum TransferWrapperMethod
    {
        ThroughContractTransfer
    }

    public class TransferWrapperContract : BaseContract<TransferWrapperMethod>
    {
        public TransferWrapperContract(INodeManager nodeManager, string callAddress, string contractAddress) :
            base(nodeManager, contractAddress)
        {
            SetAccount(callAddress);
        }

        public TransferWrapperContract(INodeManager nodeManager, string callAddress)
            : base(nodeManager, ContractFileName, callAddress)
        {
        }
        public static string ContractFileName => "AElf.Contracts.TransferWrapperContract";

    }
}