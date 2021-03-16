using AElfChain.Common.Helpers;
using Newtonsoft.Json;

namespace AElf.Automation.BlockCheck
{
    public class ConfigInfo
    {
        [JsonProperty("Url")] public string Url { get; set; }
        [JsonProperty("VerifyBlockCount")] public long VerifyBlockCount { get; set; }
        [JsonProperty("StartBlock")] public long StartBlock { get; set; }
        
        [JsonProperty("VerifyTimes")] public long VerifyTimes { get; set; }

        [JsonProperty("VerifyOne")] public bool VerifyOne { get; set; }
        [JsonProperty("IncludeTransaction")] public bool IncludeTransaction { get; set; }

        public static ConfigInfo ReadInformation => ConfigHelper<ConfigInfo>.GetConfigInfo("block-check-config.json",false);
    }
}