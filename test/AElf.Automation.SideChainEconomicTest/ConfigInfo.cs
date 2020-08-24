using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace AElf.Automation.SideChainEconomicTest
{
    public class SideChainInfos
    {
        [JsonProperty("Id")] public int Id { get; set; }
        [JsonProperty("SideChainUrl")] public string SideChainUrl { get; set; }
        [JsonProperty("Creator")] public string Creator { get; set; }
        [JsonProperty("Password")] public string Password { get; set; }
        [JsonProperty("Contract")] public string Contract { get; set; }
    }

    public class MainChainInfos
    {
        [JsonProperty("MainChainUrl")] public string MainChainUrl { get; set; }
        [JsonProperty("Creator")] public string Creator { get; set; }
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

                var configFile = Path.Combine(Directory.GetCurrentDirectory(), "side-economic-config.json");
                _jsonContent = File.ReadAllText(configFile);
                _instance = JsonConvert.DeserializeObject<ConfigInfo>(_jsonContent);
            }

            return _instance;
        }
    }
}