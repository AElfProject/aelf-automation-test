using System.IO;
using Newtonsoft.Json;

namespace AElf.Automation.CheckTxStatus
{
    public class ConfigInfo
    {
        [JsonProperty("Url")] public string Url { get; set; }
        [JsonProperty("Account")] public string Account { get; set; }
        [JsonProperty("Password")] public string Password { get; set; }
        [JsonProperty("VerifyBlockNumber")] public int VerifyBlockNumber { get; set; }
        [JsonProperty("StartBlock")] public int StartBlock { get; set; }
        [JsonProperty("ExceptContract")] public string ExceptContract { get; set; }
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

                var configFile = Path.Combine(Directory.GetCurrentDirectory(), "check-config.json");
                _jsonContent = File.ReadAllText(configFile);
                _instance = JsonConvert.DeserializeObject<ConfigInfo>(_jsonContent);
            }

            return _instance;
        }
    }
}