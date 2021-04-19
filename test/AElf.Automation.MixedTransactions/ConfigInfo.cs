using System.Collections.Generic;
using AElfChain.Common.Helpers;
using Newtonsoft.Json;

namespace AElf.Automation.MixedTransactions
{
    public class ContractInfo
    {
        [JsonProperty("ContractName")] public string ContractName { set; get; }
        [JsonProperty("IsNeedDeploy")] public bool IsNeedDeploy {set; get; }
        [JsonProperty("ContractCount")] public int ContractCount { set; get; }
        [JsonProperty("TokenInfos")] public List<TokenInfos> TokenInfos { get; set; }
    }

    public class TokenInfos
    {
        [JsonProperty("ContractAddress")] public string ContractAddress {set; get; }
        [JsonProperty("TokenSymbol")] public string TokenSymbol { get; set; }
    }

    public class ConfigInfo
    {
        [JsonProperty("ContractInfo")] public List<ContractInfo> ContractInfos { get; set; }
        [JsonProperty("ServiceUrl")] public string ServiceUrl {set; get; }
        [JsonProperty("InitAccount")] public string InitAccount { get; set; }
        [JsonProperty("Password")] public string Password { get; set; }
        [JsonProperty("TransactionGroup")] public int TransactionGroup {set; get; }

        [JsonProperty("VerifyCount")] public long VerifyCount {set; get; }
        [JsonProperty("TransactionCount")] public long TransactionCount {set; get; }
        [JsonProperty("NeedCreateToken")] public bool NeedCreateToken {set; get; }

        

        public static ConfigInfo ReadInformation => ConfigHelper<ConfigInfo>.GetConfigInfo("mixed-config.json");
    }
}