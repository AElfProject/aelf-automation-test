using AElfChain.Common.Managers;

namespace AElfChain.Common.Contracts
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
        RecordCrossChainData,

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
        public CrossChainContract(INodeManager nm, string account) :
            base(nm, ContractFileName, account)
        {
        }

        public CrossChainContract(INodeManager nm, string callAddress, string contractAbi) :
            base(nm, contractAbi)
        {
            SetAccount(callAddress);
        }

        public static string ContractFileName => "AElf.Contracts.CrossChain";
    }
}