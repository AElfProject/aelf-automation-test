using AElf.Automation.Common.Helpers;

namespace AElf.Automation.Common.Contracts
{
    public enum EventsMethod
    {
        //action
        InitializeEvents,  
        IssueOrder,
        DealOrder,
        CancelOrder,
        
        //view
        QueryIssueOrders,
        QueryDoneOrders,
        QueryCanceledOrders,
        QueryOrderById,
        QueryOrderSubOrders
    }
    
    public class EventsContract : BaseContract<EventsMethod>
    {
        protected EventsContract(IApiHelper apiHelper, string callAddress, string contractAddress) : 
            base(apiHelper, callAddress)
        {
            CallAddress = callAddress;
            UnlockAccount(CallAddress);
        }

        protected EventsContract(IApiHelper apiHelper, string callAddress) : 
            base(apiHelper, ContractFileName, callAddress)
        {
        }
        
        public static string ContractFileName => "AElf.Contracts.TestContract.Events";
    }
}