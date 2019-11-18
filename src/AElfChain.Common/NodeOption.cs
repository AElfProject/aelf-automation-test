using System.Collections.Generic;

namespace AElfChain.Common
{
    public static class NodeOption
    {
        public static string NativeTokenSymbol => NodeInfoHelper.Config.NativeTokenSymbol;
        public static string DefaultPassword => NodeInfoHelper.Config.DefaultPassword;
        public static List<Node> AllNodes => NodeInfoHelper.Config.Nodes;

        public static string GetTokenSymbol(string symbol)
        {
            return symbol == "" ? NativeTokenSymbol : symbol;
        }
    }
}