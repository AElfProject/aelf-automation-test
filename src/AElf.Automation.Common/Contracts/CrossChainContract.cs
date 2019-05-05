using AElf.Automation.Common.Helpers;

namespace AElf.Automation.Common.Contracts
{
    public enum CrossChainContractMethod
    {
        //Action
        RequestChainCreation,
        WithdrawRequest,
        CreateSideChain,
        Recharge,
        RequestChainDisposal,
        DisposeSideChain,
                
        //View
        GetChainStatus,
        GetSideChainHeight,
        GetParentChainHeight,
        GetParentChainId,
        LockedBalance,
        VerifyTransaction,
        GetBoundParentChainHeightAndMerklePathByHeight
    }
    public class CrossChainContract : BaseContract<CrossChainContractMethod>
    {
        public CrossChainContract(RpcApiHelper ch, string account) :
            base(ch, "AElf.Contracts.CrossChain", account)
        {
        }
    
        public CrossChainContract(RpcApiHelper ch, string callAddress, string contractAbi):
            base(ch, contractAbi)
        {
            CallAddress = callAddress;
            UnlockAccount(CallAddress);
        }
    }
}