using AElf.Contracts.TestContract.DApp;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Managers;

namespace AElfChain.Common.Contracts
{
    public enum DAppMethod
    {
        //Action
        InitializeInput,
        SignUp,
        Deposit,
        Withdraw,
        Use
    }

    public class DAppContract : BaseContract<DAppContract>
    {
        public DAppContract(INodeManager nodeManager, string callAddress, string contractAddress)
            : base(nodeManager, contractAddress)
        {
            SetAccount(callAddress);
        }

        public DAppContract(INodeManager nodeManager, string callAddress)
            : base(nodeManager, ContractFileName, callAddress)
        {
        }

        public static string ContractFileName => "AElf.Contracts.TestContract.DApp";

        public DAppContainer.DAppStub GetDAppStub(string callAddress = null)
        {
            var caller = callAddress ?? CallAddress;
            var stub = new ContractTesterFactory(NodeManager);
            var contractStub =
                stub.Create<DAppContainer.DAppStub>(
                    ContractAddress.ConvertAddress(), caller);
            return contractStub;
        }
    }
}