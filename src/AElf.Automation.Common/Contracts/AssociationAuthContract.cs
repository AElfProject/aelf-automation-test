using AElf.Automation.Common.Helpers;
using AElf.Automation.Common.Managers;

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
        CreateProposal
    }

    public class AssociationAuthContract : BaseContract<AssociationAuthMethod>
    {
        public AssociationAuthContract(INodeManager nm, string callAddress, string contractAddress) :
            base(nm, contractAddress)
        {
            SetAccount(callAddress);
            Logger = Log4NetHelper.GetLogger();
        }
    }
}