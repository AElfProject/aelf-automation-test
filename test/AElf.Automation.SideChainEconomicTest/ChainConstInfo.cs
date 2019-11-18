namespace AElf.Automation.SideChainEconomicTest
{
    public class ChainConstInfo
    {
        public const string ChainAccount = "28Y8JA1i2cN6oHvdv7EraXJr9a1gY6D1PpJXw9QtRMRwKcBQMK";

        //public const string ChainAccount = "2boUmCEXvK9BamvsQTr9VQ5bXPZSvd5utzB9nz3fwPmbJtoySh";
        public const string TestAccount = "1Gvy3WVMiBFV4ipnftjooxVaeoF6YT2uk66chgyhSDMoVGQy3";

        //main chain
        public const string MainChainUrl = "http://192.168.197.42:8000";

        //side chain A
        public const string SideChainUrlA = "http://192.168.197.23:8001";

        //side chain B
        public const string SideChainUrlB = "http://192.168.197.24:8002";

        //public const string MainChainUrl = "http://192.168.199.131:8000";
        public static int MainChainId = ChainHelper.ConvertBase58ToChainId("AELF");
        public static int SideChainIdA = ChainHelper.ConvertBase58ToChainId("2112");
        public static int SideChainIdB = ChainHelper.ConvertBase58ToChainId("2113");
    }
}