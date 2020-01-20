using Acs7;
using AElf.Contracts.CrossChain;
using AElf.Types;
using AElfChain.Common.Managers;
using Google.Protobuf.WellKnownTypes;
using Volo.Abp.Threading;

namespace AElfChain.Common.Contracts
{
    public enum CrossChainContractMethod
    {
        //Action
        RequestSideChainCreation,
        Recharge,
        DisposeSideChain,
        RecordCrossChainData,
        ReleaseSideChainCreation,
        AdjustIndexingFeePrice,
        ChangeCrossChainIndexingController,
        ChangeSideChainLifetimeController,

        //View
        GetChainStatus,
        GetSideChainHeight,
        GetParentChainHeight,
        GetParentChainId,
        LockedBalance,
        VerifyTransaction,
        GetBoundParentChainHeightAndMerklePathByHeight,
        GetSideChainCreator,
        GetSideChainIndexingFeePrice,
        GetSideChainBalance,
        GetSideChainIndexingFeeController,
        GetSideChainLifetimeController,
        GetCrossChainIndexingController,
        GetChainInitializationData
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

        public Address GetSideChainCreator(int chainId, string caller = null)
        {
            var tester = GetTestStub<CrossChainContractContainer.CrossChainContractStub>(caller);
            var address = AsyncHelper.RunSync(() =>
                tester.GetSideChainCreator.CallAsync(new SInt32Value {Value = chainId}));

            Logger.Info($"Chain {chainId} creator is {address}");

            return address;
        }

        public long GetSideChainBalance(int chainId)
        {
            return CallViewMethod<SInt64Value>(
                CrossChainContractMethod.GetSideChainBalance, new SInt32Value {Value = chainId}).Value;
        }

        public CrossChainMerkleProofContext GetCrossChainMerkleProofContext(long blockHeight)
        {
            return CallViewMethod<CrossChainMerkleProofContext>(
                CrossChainContractMethod.GetBoundParentChainHeightAndMerklePathByHeight, new SInt64Value
                {
                    Value = blockHeight
                });
        }

        public long GetParentChainHeight()
        {
            return CallViewMethod<SInt64Value>(
                CrossChainContractMethod.GetParentChainHeight, new Empty()).Value;
        }

        public long GetSideChainHeight(int chainId)
        {
            return CallViewMethod<SInt64Value>(
                CrossChainContractMethod.GetSideChainHeight, new SInt32Value {Value = chainId}).Value;
        }

        public long GetSideChainIndexingFeePrice(int chainId)
        {
            return CallViewMethod<SInt64Value>(
                CrossChainContractMethod.GetSideChainIndexingFeePrice, new SInt32Value {Value = chainId}).Value;
        }

        public AuthorityStuff GetCrossChainIndexingController()
        {
            return CallViewMethod<AuthorityStuff>(
                CrossChainContractMethod.GetCrossChainIndexingController, new Empty());
        }

        public AuthorityStuff GetSideChainLifetimeController()
        {
            return CallViewMethod<AuthorityStuff>(
                CrossChainContractMethod.GetSideChainLifetimeController, new Empty());
        }

        public GetSideChainIndexingFeeControllerOutput GetSideChainIndexingFeeController(int chainId)
        {
            return CallViewMethod<GetSideChainIndexingFeeControllerOutput>(
                CrossChainContractMethod.GetSideChainIndexingFeeController, new SInt32Value {Value = chainId});
        }

        public ChainInitializationData GetChainInitializationData(int chainId)
        {
            return CallViewMethod<ChainInitializationData>(
                CrossChainContractMethod.GetChainInitializationData, new SInt32Value {Value = chainId});
        }
    }
}