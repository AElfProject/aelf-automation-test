using AElfChain.Common.Helpers;
using Newtonsoft.Json;

namespace AElf.Automation.BasicTransaction
{
    public class ConfigInfo
    {
        [JsonProperty("ServiceUrl")] public string Url { get; set; }
        [JsonProperty("InitAccount")] public string InitAccount { get; set; }
        [JsonProperty("Password")] public string Password { get; set; }
        [JsonProperty("TransferAmount")] public long TransferAmount { get; set; }
        public static ConfigInfo ReadInformation => ConfigHelper<ConfigInfo>.GetConfigInfo("base-config.json");
    }
}