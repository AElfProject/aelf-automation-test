using System.IO;
using Newtonsoft.Json;

namespace AElf.Automation.RpcPerformance
{
    public class ConfigInfo
    {
        [JsonProperty("GroupCount")] public int GroupCount { get; set; }
        [JsonProperty("TransactionCount")] public int TransactionCount { get; set; }
        [JsonProperty("ServiceUrl")] public string ServiceUrl { get; set; }
        [JsonProperty("SelectTxLimit")] public int SelectTxLimit { get; set; }
        [JsonProperty("SentTxLimit")] public int SentTxLimit { get; set; }
        [JsonProperty("ExecuteMode")] public int ExecuteMode { get; set; }
        [JsonProperty("Timeout")] public int Timeout { get; set; }
        [JsonProperty("Conflict")] public bool Conflict { get; set; }
        [JsonProperty("ReadOnlyTransaction")] public bool ReadOnlyTransaction { get; set; }
    }

    public static class ConfigInfoHelper
    {
        private static ConfigInfo _instance;
        private static readonly object LockObj = new object();

        public static ConfigInfo Config => GetConfigInfo();

        private static ConfigInfo GetConfigInfo()
        {
            lock (LockObj)
            {
                if (_instance != null) return _instance;

                var configFile = Path.Combine(Directory.GetCurrentDirectory(), "rpc-performance.json");
                var content = File.ReadAllText(configFile);
                _instance = JsonConvert.DeserializeObject<ConfigInfo>(content);
            }

            return _instance;
        }
    }
}