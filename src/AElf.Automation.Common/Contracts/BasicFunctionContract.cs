using AElf.Automation.Common.Managers;

namespace AElf.Automation.Common.Contracts
{
    public enum FunctionMethod
    {
        InitialBasicFunctionContract,
        UpdateBetLimit,
        UserPlayBet,

        QueryWinMoney,
        QueryRewardMoney,
        QueryUserWinMoney,
        QueryUserLoseMoney
    }

    public class BasicFunctionContract : BaseContract<FunctionMethod>
    {
        public BasicFunctionContract(INodeManager nodeManager, string callAddress, string contractAddress)
            : base(nodeManager, contractAddress)
        {
            SetAccount(callAddress);
        }

        public BasicFunctionContract(INodeManager nodeManager, string callAddress)
            : base(nodeManager, ContractFileName, callAddress)
        {
        }

        public static string ContractFileName => "AElf.Contracts.TestContract.BasicFunction";
    }
}