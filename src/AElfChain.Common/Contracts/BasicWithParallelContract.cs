using System.Linq;
using AElfChain.Common.Managers;

namespace AElfChain.Common.Contracts
{
    public enum BasicParallelMethod
    {
        InitialBasicFunctionWithParallelContract,
        UpdateBetLimit,
        UserPlayBet,
        LockToken,
        UnlockToken,
        ValidateOrigin,
        IncreaseWinMoney,
        IncreaseValue,
        IncreaseValueParallel,
        IncreaseValueWithInline,
        IncreaseValueWithPrePlugin,
        IncreaseValueWithPostPlugin,
        IncreaseValueWithInlineAndPostPlugin,
        IncreaseValueWithPlugin,
        IncreaseValueWithInlineAndPlugin,
        IncreaseValueParallelWithInlineAndPlugin,
        RemoveValue,
        RemoveValueFromInlineWithPlugin,
        RemoveValueFromPrePlugin,
        RemoveValueFromPostPlugin,
        RemoveValueParallelFromPostPlugin,
        RemoveValueWithPlugin,
        RemoveAfterSetValue,
        SetAfterRemoveValue,
        RemoveValueParallel,
        ComplexChangeWithDeleteValue1,
        ComplexChangeWithDeleteValue2,
        ComplexChangeWithDeleteValue3,

        QueryWinMoney,
        QueryRewardMoney,
        QueryUserWinMoney,
        QueryUserLoseMoney,
        QueryTwoUserWinMoney,
        GetValue
    }

    public class BasicWithParallelContract : BaseContract<BasicParallelMethod>
    {
        public BasicWithParallelContract(INodeManager nodeManager, string callAddress, string contractAddress)
            : base(nodeManager, contractAddress)
        {
            SetAccount(callAddress);
        }

        public BasicWithParallelContract(INodeManager nodeManager, string callAddress)
            : base(nodeManager, ContractFileName, callAddress)
        {
        }

        public static BasicWithParallelContract GetOrDeployBasicWithParallelContract(INodeManager nodeManager,
            string callAddress)
        {
            var genesis = nodeManager.GetGenesisContract();
            var addressList = genesis.QueryCustomContractByMethodName("InitialBasicFunctionWithParallelContract");
            if (addressList.Count == 0)
            {
                var contract = new BasicWithParallelContract(nodeManager, callAddress);
                return contract;
            }

            return new BasicWithParallelContract(nodeManager, callAddress, addressList.First().GetFormatted());
        }

        public static string ContractFileName => "AElf.Contracts.TestContract.BasicFunctionWithParallel";
    }
}