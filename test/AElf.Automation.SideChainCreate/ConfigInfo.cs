using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace AElf.Automation.SideChainCreate
{
    public class ConfigInfo
    {
        [JsonProperty("TestEnvironment")] public string TestEnvironment { get; set; }
        [JsonProperty("EnvironmentInfo")] public List<EnvironmentInfo> EnvironmentInfos { get; set; }
        [JsonProperty("ApproveTokenAmount")] public long ApproveTokenAmount { get; set; }
        [JsonProperty("SideChainInfo")] public List<SideChainInfo> SideChainInfos { get; set; }
    }

    public class EnvironmentInfo
    {
        [JsonProperty("Environment")] public string Environment { get; set; }
        [JsonProperty("Creator")] public string Creator { get; set; }
        [JsonProperty("ConfigFile")] public string ConfigFile { get; set; }
        [JsonProperty("Password")] public string Password { get; set; }
    }

    public class SideChainInfo
    {
        [JsonProperty("IndexingPrice")] public long IndexingPrice { get; set; }
        [JsonProperty("LockedTokenAmount")] public long LockedTokenAmount { get; set; }
        [JsonProperty("IsPrivilegePreserved")] public bool IsPrivilegePreserved { get; set; }
        [JsonProperty("NativeSymbol")] public string TokenSymbol { get; set; }
    }

    public static class ConfigHelper
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

                var configFile = Path.Combine(Directory.GetCurrentDirectory(), "create.json");
                _jsonContent = File.ReadAllText(configFile);
                _instance = JsonConvert.DeserializeObject<ConfigInfo>(_jsonContent);
            }

            return _instance;
        }
    }
}