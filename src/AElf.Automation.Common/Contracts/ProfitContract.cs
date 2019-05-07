using AElf.Automation.Common.Helpers;

namespace AElf.Automation.Common.Contracts
{
    public enum ProfitMethod
    {
        //action
        InitializeProfitContract,
        CreateProfitItem,
        RegisterSubProfitItem,
        AddWeight,
        SubWeight,
        AddWeights,
        ReleaseProfit,
        AddProfits,
        Profit,
        
        //view
        GetCreatedProfitItems,
        GetProfitItemVirtualAddress,
        GetReleasedProfitsInformation,
        GetProfitDetails,
        GetProfitItem
    }
    public class ProfitContract : BaseContract<ProfitMethod>
    {
        public ProfitContract(IApiHelper apiHelper, string callAddress) :
            base(apiHelper, "AElf.Contracts.Profit", callAddress)
        {
        }

        public ProfitContract(IApiHelper apiHelper, string callAddress, string contractAddress):
            base(apiHelper, contractAddress)
        {
            CallAddress = callAddress;
            UnlockAccount(CallAddress);
        }
    }
}