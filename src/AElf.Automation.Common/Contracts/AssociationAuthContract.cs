using AElf.Automation.Common.Helpers;
using AElf.Automation.Common.Managers;

namespace AElf.Automation.Common.Contracts
{
    public enum AssociationMethod
    {
        //View
        GetOrganization,
        GetProposal,

        //Action
        CreateOrganization,
        Approve,
        CreateProposal,
        Release
    }

    public class AssociationAuthContract : BaseContract<AssociationMethod>
    {
        public AssociationAuthContract(IApiHelper apiHelper, string callAddress) : base(apiHelper, "AElf.Contracts.AssociationAuth",
            callAddress)
        {
        }


        public AssociationAuthContract(IApiHelper apiHelper, string callAddress, string contractAddress) : base(apiHelper,
            contractAddress)
        {
            SetAccount(callAddress);
            Logger = Log4NetHelper.GetLogger();
        }
    }
}