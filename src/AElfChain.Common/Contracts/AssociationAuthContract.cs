using AElfChain.Common.Managers;

namespace AElfChain.Common.Contracts
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
        public AssociationAuthContract(INodeManager nodeManager, string callAddress, string electionAddress)
            : base(nodeManager, electionAddress)
        {
            SetAccount(callAddress);
        }

        public AssociationAuthContract(INodeManager nodeManager, string callAddress)
            : base(nodeManager, "AElf.Contracts.AssociationAuth", callAddress)
        {
        }
    }
}