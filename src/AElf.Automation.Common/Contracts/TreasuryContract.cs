using AElf.Automation.Common.Helpers;

namespace AElf.Automation.Common.Contracts
{
    public enum TreasuryMethod
    {
        //Action
        InitialTreasuryContract,
        InitialMiningRewardProfitItem,
        ReleaseMiningReward,
        Release,
        Donate,
        DonateAll,

        //View
        GetTreasurySchemeId
    }

    public class TreasuryContract : BaseContract<TreasuryMethod>
    {
        public TreasuryContract(IApiHelper apiHelper, string callAddress, string contractAddress) :
            base(apiHelper, contractAddress)
        {
            CallAddress = callAddress;
            UnlockAccount(CallAddress);
        }
    }
}