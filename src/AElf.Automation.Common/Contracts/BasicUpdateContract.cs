using AElf.Automation.Common.Helpers;

namespace AElf.Automation.Common.Contracts
{
    public enum UpdateMethod
    {
        InitialBasicUpdateContract,
        UpdateBetLimit,
        UserPlayBet,
        UpdateMortgage,
        UpdateStopBet,

        QueryWinMoney,
        QueryRewardMoney,
        QueryUserWinMoney,
        QueryUserLoseMoney,
        QueryBetStatus
    }

    public class BasicUpdateContract : BaseContract<UpdateMethod>
    {
        public BasicUpdateContract(IApiHelper apiHelper, string callAddress, string dividendsAddress)
            : base(apiHelper, dividendsAddress)
        {
            CallAddress = callAddress;
            UnlockAccount(CallAddress);
        }

        public BasicUpdateContract(IApiHelper apiHelper, string callAddress)
            : base(apiHelper, "AElf.Contracts.TestContract.BasicUpdate", callAddress)
        {
        }

        public static string ContractFileName => "AElf.Contracts.TestContract.BasicUpdate";
    }
}