using System;
using System.Collections.Generic;
using AElfChain.Common.Helpers;
using Newtonsoft.Json;

namespace AElf.Automation.RpcPerformance
{
    public class RpcConfig
    {
        [JsonProperty("GroupCount")] public int GroupCount { get; set; }
        [JsonProperty("TransactionCount")] public int TransactionCount { get; set; }
        [JsonProperty("UserCount")] public int UserCount { get; set; }

        [JsonProperty("EnableRandomTransaction")]
        public bool EnableRandomTransaction { get; set; }
        [JsonProperty("ServiceUrl")] public string ServiceUrl { get; set; }
        [JsonProperty("SentTxLimit")] public int SentTxLimit { get; set; }
        [JsonProperty("ExecuteMode")] public int ExecuteMode { get; set; }
        [JsonProperty("Timeout")] public int Timeout { get; set; }

        [JsonProperty("RandomSenderTransaction")]
        public bool RandomSenderTransaction { get; set; }
        
        [JsonProperty("RequestRandomEndpoint")]
        public static RpcConfig ReadInformation => ConfigHelper<RpcConfig>.GetConfigInfo("rpc-performance.json",false);
    }
}