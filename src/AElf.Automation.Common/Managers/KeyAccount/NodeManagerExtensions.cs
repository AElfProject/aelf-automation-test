using AElf.Automation.Common.Contracts;

namespace AElf.Automation.Common.Managers
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