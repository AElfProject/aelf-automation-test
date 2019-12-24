using AElf.Contracts.CrossChain;
using AElf.Types;
using AElfChain.Common.Managers;
using Volo.Abp.Threading;

namespace AElfChain.Common.Contracts
{
    public enum CrossChainContractMethod
    {
        //Action
        RequestSideChainCreation,
        Recharge,
        RequestChainDisposal,
        DisposeSideChain,
        RecordCrossChainData,
        ReleaseSideChainCreation,

        //View
        GetChainStatus,
        GetSideChainHeight,
        GetParentChainHeight,
        GetParentChainId,
        LockedBalance,
        VerifyTransaction,
        GetBoundParentChainHeightAndMerklePathByHeight,
        GetSideChainCreator
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
        
        public Address GetSideChainCreator(int chainId,string caller = null)
        {
            var tester = GetTestStub<CrossChainContractContainer.CrossChainContractStub>(caller);
            var address = AsyncHelper.RunSync(() => tester.GetSideChainCreator.CallAsync(new SInt32Value{Value = chainId}));
            
            Logger.Info($"Chain {chainId} creator is {address}");

            return address;
        }

        public static string ContractFileName => "AElf.Contracts.CrossChain";
    }
}