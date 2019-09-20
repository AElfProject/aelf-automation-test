namespace AElf.Automation.Common
{
    public class NodeOption
    {
        public static string NativeTokenSymbol => NodeInfoHelper.Config.NativeTokenSymbol;
        public static bool IsMainChain => NodeInfoHelper.Config.ChainTypeInfo.IsMainChain;
        public static string ChainToken => NodeInfoHelper.Config.ChainTypeInfo.TokenSymbol;
        public static string DefaultPassword => NodeInfoHelper.Config.DefaultPassword;

        public static string GetTokenSymbol(string symbol)
        {
            return symbol == "" ? NativeTokenSymbol : symbol;
        }
    }
}