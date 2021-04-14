using AElfChain.Common.Helpers;
using Newtonsoft.Json;

namespace AElf.Automation.ContractTransfer
{
    public class ConfigInfo
    {
        [JsonProperty("ServiceUrl")] public string Url { get; set; }
        [JsonProperty("InitAccount")] public string InitAccount { get; set; }
        [JsonProperty("Password")] public string Password { get; set; }
        [JsonProperty("ContractCount")] public long ContractCount { get; set; }
        [JsonProperty("UserCount")] public int UserCount { get; set; }
        [JsonProperty("TransactionCount")] public long TransactionCount { get; set; }

        public static ConfigInfo ReadInformation => ConfigHelper<ConfigInfo>.GetConfigInfo("contract-transfer-config.json");
    }
}