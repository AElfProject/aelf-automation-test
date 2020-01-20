using AElfChain.Common.Managers;

namespace AElfChain.Common.Contracts
{
    public enum TokenHolderMethod
    {
        //Action
        CreateScheme,
        AddBeneficiary,
        RemoveBeneficiary,
        ContributeProfits,
        DistributeProfits,
        RegisterForProfits,
        Withdraw,
        ClaimProfits,

        //View
        GetScheme
    }

    public class TokenHolderContract : BaseContract<TokenHolderMethod>
    {
        public TokenHolderContract(INodeManager nodeManager, string callAddress) :
            base(nodeManager, "AElf.Contracts.TokenHolder", callAddress)
        {
        }

        public TokenHolderContract(INodeManager nodeManager, string callAddress, string contractAddress) :
            base(nodeManager, contractAddress)
        {
            SetAccount(callAddress);
        }
    }
}