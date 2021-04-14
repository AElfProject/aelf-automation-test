using System;
using System.Collections.Generic;
using AElfChain.Common.Helpers;
using Newtonsoft.Json;

namespace AElf.Automation.RpcPerformance
{
    public class RpcConfig
    {
        [JsonProperty("GroupCount")] public int GroupCount { get; set; }
        [JsonProperty("TransactionGroup")] public int TransactionGroup { get; set; }
        [JsonProperty("TransactionCount")] public int TransactionCount { get; set; }
        [JsonProperty("UserCount")] public int UserCount { get; set; }

        [JsonProperty("EnableRandomTransaction")]
        public bool EnableRandomTransaction { get; set; }
        [JsonProperty("ServiceUrl")] public string ServiceUrl { get; set; } 
        [JsonProperty("Timeout")] public int Timeout { get; set; }
        [JsonProperty("Duration")] public int Duration { get; set; }


        [JsonProperty("RandomSenderTransaction")]
        public bool RandomSenderTransaction { get; set; }

        [JsonProperty("ContractAddress")] public string ContractAddress { get; set; }
        [JsonProperty("TokenList")] public List<string> TokenList { get; set; }

        public static RpcConfig ReadInformation => ConfigHelper<RpcConfig>.GetConfigInfo("rpc-performance.json");
    }
}