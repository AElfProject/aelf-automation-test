using AElf.Automation.Common.Helpers;

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
        public BasicFunctionContract(IApiHelper apiHelper, string callAddress, string contractAddress)
            : base(apiHelper, contractAddress)
        {
            CallAddress = callAddress;
            UnlockAccount(CallAddress);
        }

        public BasicFunctionContract(IApiHelper apiHelper, string callAddress)
            : base(apiHelper, "AElf.Contracts.TestContract.BasicFunction", callAddress)
        {
        }

        public static string ContractFileName => "AElf.Contracts.TestContract.BasicFunction";
    }
}