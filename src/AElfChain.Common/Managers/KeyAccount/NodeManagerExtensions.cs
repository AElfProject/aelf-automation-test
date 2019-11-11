using AElfChain.Common.Contracts;

namespace AElfChain.Common.Managers
{
    public static class NodeManagerExtensions
    {
        //contract
        public static GenesisContract GetGenesisContract(this INodeManager nodeManager, string account = "")
        {
            var genesis = GenesisContract.GetGenesisContract(nodeManager, account);

            return genesis;
        }
    }
}