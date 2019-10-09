using AElf.Automation.Common.Managers;

namespace AElf.Automation.Common.Contracts
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
        
        public static string ContractFileName => "AElf.Contracts.TestContract.BasicFunctionWithParallel";
    }
}