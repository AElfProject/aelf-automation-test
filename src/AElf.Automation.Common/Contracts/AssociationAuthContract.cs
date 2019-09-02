using AElf.Automation.Common.Helpers;

namespace AElf.Automation.Common.Contracts
{
    public enum AssociationAuthMethod
    {
        //View
        GetOrganization,
        GetProposal,

        //Action
        CreateOrganization,
        Approve,
        CreateProposal,
    }

    public class AssociationAuthContract : BaseContract<AssociationAuthMethod>
    {
        public AssociationAuthContract(IApiHelper ch, string callAddress, string contractAddress) : 
            base(ch, contractAddress)
        {
            CallAddress = callAddress;
            UnlockAccount(CallAddress);
            Logger = Log4NetHelper.GetLogger();
        }
    }
}