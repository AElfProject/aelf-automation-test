using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace AElf.Automation.SideChain.Verification
{
    public class EnvironmentInfo
    {
        [JsonProperty("MainChainInfos")] public MainChainInfos MainChainInfos { get; set; }
        [JsonProperty("SideChainInfos")] public List<SideChainInfos> SideChainInfos { get; set; }
        [JsonProperty("Environment")] public string Environment { get; set; }
        [JsonProperty("Config")] public string Config { get; set; }
    }

    public class MainChainInfos
    {
        [JsonProperty("MainChainUrl")] public string MainChainUrl { get; set; }
        [JsonProperty("Account")] public string Account { get; set; }
        [JsonProperty("Password")] public string Password { get; set; }
    }

    public class SideChainInfos
    {
        [JsonProperty("SideChainUrl")] public string SideChainUrl { get; set; }
    }

    public class TestCase
    {
        [JsonProperty("name")] public string Name { get; set; }
        [JsonProperty("enable")] public bool Enable { get; set; }
    }

    public class ConfigInfo
    {
        [JsonProperty("TestEnvironment")] public string TestEnvironment { get; set; }
        [JsonProperty("EnvironmentInfo")] public List<EnvironmentInfo> EnvironmentInfos { get; set; }
        [JsonProperty("TestCases")] public List<TestCase> TestCases { get; set; }
        [JsonProperty("CreateTokenNumber")] public int CreateTokenNumber { get; set; }

        [JsonProperty("VerifySideChainNumber")]
        public int VerifySideChainNumber { get; set; }

        [JsonProperty("VerifyBlockNumber")] public int VerifyBlockNumber { get; set; }
        [JsonProperty("TransferAccount")] public int TransferAccount { get; set; }
    }

    public static class ConfigInfoHelper
    {
        private static ConfigInfo _instance;
        private static string _jsonContent;
        private static readonly object LockObj = new object();

        public static ConfigInfo Config => GetConfigInfo();

        private static ConfigInfo GetConfigInfo()
        {
            lock (LockObj)
            {
                if (_instance != null) return _instance;

                var configFile = Path.Combine(Directory.GetCurrentDirectory(), "cross-chain-config.json");
                _jsonContent = File.ReadAllText(configFile);
                _instance = JsonConvert.DeserializeObject<ConfigInfo>(_jsonContent);
            }

            return _instance;
        }
    }
}