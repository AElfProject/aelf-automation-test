using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace AElf.Automation.SideChainTests
{
    public class SideChainInfos
    {
        [JsonProperty("SideChainUrl")] public string SideChainUrl { get; set; }
    }

    public class MainChainInfos
    {
        [JsonProperty("MainChainUrl")] public string MainChainUrl { get; set; }
        [JsonProperty("Account")] public string Account { get; set; }
        [JsonProperty("Password")] public string Password { get; set; }
    }

    public class ConfigInfo
    {
        [JsonProperty("MainChainInfos")] public MainChainInfos MainChainInfos { get; set; }
        [JsonProperty("SideChainInfos")] public List<SideChainInfos> SideChainInfos { get; set; }
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

                var configFile = Path.Combine(Directory.GetCurrentDirectory(), "config.json");
                _jsonContent = File.ReadAllText(configFile);
                _instance = JsonConvert.DeserializeObject<ConfigInfo>(_jsonContent);
            }

            return _instance;
        }
    }
}