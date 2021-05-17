using System.Collections.Generic;
using AElfChain.Common.Helpers;
using Newtonsoft.Json;

namespace AElf.Automation.AccountCheck
{
    public class TransferInfo
    { 
        [JsonProperty("TransferAmount")] public long TransferAmount {set; get; }
        [JsonProperty("ContractCount")] public int ContractCount { get; set; }
        [JsonProperty("IsNeedDeploy")] public bool IsNeedDeploy {set; get; }
        [JsonProperty("IsAddSystemContract")] public bool IsAddSystemContract {set; get; }

    }

    public class ContractInfo
    {
        [JsonProperty("Symbol")] public string TokenSymbol {set; get; }
        [JsonProperty("ContractAddress")] public string ContractAddress {set; get; }
    }

    public class ConfigInfo
    {
        [JsonProperty("TransferInfo")]public TransferInfo TransferInfo {set; get; }
        [JsonProperty("ContractInfo")] public List<ContractInfo> ContractInfos { get; set; }
        [JsonProperty("ServiceUrl")] public string ServiceUrl {set; get; }
        [JsonProperty("InitAccount")] public string InitAccount { get; set; }
        [JsonProperty("Password")] public string Password { get; set; }
        [JsonProperty("UserCount")] public int UserCount {set; get; }
        [JsonProperty("CheckType")] public string CheckType {set; get; }
        [JsonProperty("CheckTimes")] public int Times { set; get; }

        public static ConfigInfo ReadInformation => ConfigHelper<ConfigInfo>.GetConfigInfo("account-check-config.json");
    }
}