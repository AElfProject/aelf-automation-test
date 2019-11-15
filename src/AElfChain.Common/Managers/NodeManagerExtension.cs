using AElf.Contracts.MultiToken;
using AElfChain.Common.Contracts;

namespace AElfChain.Common.Managers
{
    public static class NodeManagerExtension
    {
        public static bool IsMainChain(this INodeManager nodeManager)
        {
            var genesis = nodeManager.GetGenesisContract();
            var token = genesis.GetTokenContract();
            var primaryToken = token.GetPrimaryTokenSymbol();
            var nativeToken = token.GetNativeTokenSymbol();

            return primaryToken == nativeToken;
        }

        public static string GetPrimaryTokenSymbol(this INodeManager nodeManager)
        {
            var genesis = nodeManager.GetGenesisContract();
            var token = genesis.GetTokenContract();
            var primaryToken = token.GetPrimaryTokenSymbol();

            return primaryToken;
        }

        public static string GetNativeTokenSymbol(this INodeManager nodeManager)
        {
            var genesis = nodeManager.GetGenesisContract();
            var token = genesis.GetTokenContract();
            var nativeToken = token.GetNativeTokenSymbol();

            return nativeToken;
        }

        public static TokenInfo GetTokenInfo(this INodeManager nodeManager, string symbol)
        {
            var genesis = nodeManager.GetGenesisContract();
            var token = genesis.GetTokenContract();

            return token.GetTokenInfo(symbol);
        }
    }
}