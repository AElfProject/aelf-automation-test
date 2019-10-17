using AElfChain.Common.Managers;

namespace AElfChain.Common.Contracts
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
        protected EventsContract(INodeManager nodeManager, string callAddress, string contractAddress)
            : base(nodeManager, contractAddress)
        {
            SetAccount(callAddress);
        }

        protected EventsContract(INodeManager nodeManager, string callAddress) :
            base(nodeManager, ContractFileName, callAddress)
        {
        }

        public static string ContractFileName => "AElf.Contracts.TestContract.Events";
    }
}