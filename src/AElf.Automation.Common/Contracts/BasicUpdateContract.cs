using AElf.Automation.Common.Managers;

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
        public BasicUpdateContract(INodeManager nodeManager, string callAddress, string contractAddress)
            : base(nodeManager, contractAddress)
        {
            SetAccount(callAddress);
        }

        public BasicUpdateContract(INodeManager nodeManager, string callAddress)
            : base(nodeManager, ContractFileName, callAddress)
        {
        }

        public static string ContractFileName => "AElf.Contracts.TestContract.BasicUpdate";
    }
}