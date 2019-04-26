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
        GetProfitDetails
    }
    public class ProfitContract : BaseContract<ProfitMethod>
    {
        public ProfitContract(RpcApiHelper ch, string callAddress) :
            base(ch, "AElf.Contracts.Profit", callAddress)
        {
        }

        public ProfitContract(RpcApiHelper ch, string callAddress, string contractAddress):
            base(ch, contractAddress)
        {
            CallAddress = callAddress;
            UnlockAccount(CallAddress);
        }
    }
}