using System.IO;
using AElfChain.Common.Helpers;
using Newtonsoft.Json;

namespace AElf.Automation.CheckTxStatus
{ 
    public class ContractInfo
    {
        [JsonProperty("ContractAddress")] public string ContractAddress { get; set; }
        [JsonProperty("ExceptContract")] public string ExceptContract { get; set; }
    }
    
    public class ConfigInfo
    {
        [JsonProperty("Url")] public string Url { get; set; }
        [JsonProperty("Account")] public string Account { get; set; }
        [JsonProperty("Password")] public string Password { get; set; }
        [JsonProperty("VerifyBlockNumber")] public int VerifyBlockNumber { get; set; }
        [JsonProperty("StartBlock")] public int StartBlock { get; set; }
        [JsonProperty("ContractInfo")] public ContractInfo ContractInfo { get; set; }
        
        public static ConfigInfo ReadInformation => ConfigHelper<ConfigInfo>.GetConfigInfo("mixed-config.json",false);
    }
}