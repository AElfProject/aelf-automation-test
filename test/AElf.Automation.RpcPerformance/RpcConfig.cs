using System;
using System.Collections.Generic;
using AElfChain.Common.Helpers;
using Newtonsoft.Json;

namespace AElf.Automation.RpcPerformance
{
    public class RandomTransactionOption
    {
        [JsonProperty("enable_random")] public bool EnableRandom { get; set; }
        [JsonProperty("endpoint_list")] public List<string> EndpointList { get; set; }

        public string GetRandomEndpoint(int rdNo = 0)
        {
            if (rdNo == 0)
            {
                var rd = new Random(Guid.NewGuid().GetHashCode());
                rdNo = rd.Next(0, EndpointList.Count);
            }
            var serviceUrl = EndpointList[rdNo];
            return serviceUrl.Contains("http://") ? serviceUrl : $"http://{serviceUrl}";
        }
    }

    public class NodeTransactionOption
    {
        [JsonProperty("enable_limit")] public bool EnableLimit { get; set; }

        [JsonProperty("max_transactions_select")]
        public int MaxTransactionSelect { get; set; }
    }

    public class ChainTypeOption
    {
        [JsonProperty("is_side_chain")] public bool IsSideChain { get; set; }
        [JsonProperty("main_chain_url")] public string MainChainUrl { get; set; }
    }

    public class RpcConfig
    {
        [JsonProperty("GroupCount")] public int GroupCount { get; set; }
        [JsonProperty("TransactionCount")] public int TransactionCount { get; set; }

        [JsonProperty("EnableRandomTransaction")]
        public bool EnableRandomTransaction { get; set; }

        [JsonProperty("ServiceUrl")] public string ServiceUrl { get; set; }
        [JsonProperty("SentTxLimit")] public int SentTxLimit { get; set; }
        [JsonProperty("ExecuteMode")] public int ExecuteMode { get; set; }
        [JsonProperty("Timeout")] public int Timeout { get; set; }

        [JsonProperty("RandomSenderTransaction")]
        public bool RandomSenderTransaction { get; set; }

        [JsonProperty("NodeTransactionLimit")] public NodeTransactionOption NodeTransactionOption { get; set; }

        [JsonProperty("RequestRandomEndpoint")]
        public RandomTransactionOption RandomEndpointOption { get; set; }

        [JsonProperty("ChainType")] public ChainTypeOption ChainTypeOption { get; set; }
        public static RpcConfig ReadInformation => ConfigHelper<RpcConfig>.GetConfigInfo("rpc-performance.json");
    }
}