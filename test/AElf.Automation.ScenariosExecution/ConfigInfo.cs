using System.Collections.Generic;
using System.IO;
using System.Linq;
using AElf.Automation.Common.Contracts;
using AElf.Automation.Common.WebApi;
using Newtonsoft.Json;

namespace AElf.Automation.ScenariosExecution
{
    public class Node
    {
        [JsonProperty("name")] public string Name { get; set; }
        [JsonProperty("service_url")] public string ServiceUrl { get; set; }
        [JsonProperty("account")] public string Account { get; set; }
        [JsonProperty("password")] public string Password { get; set; }
        [JsonIgnore] public string PublicKey { get; set; }
        [JsonIgnore] public bool Status { get; set; } = false;
        [JsonIgnore] public WebApiService ApiService { get; set; }
    }

    public class TestCase
    {
        [JsonProperty("case_name")] public string CaseName { get; set; }
        [JsonProperty("enable")] public bool Enable { get; set; }
    }

    public class SpecifyEndpoint
    {
        [JsonProperty("enable")] public bool Enable { get; set; }
        [JsonProperty("service_url")] public string ServiceUrl { get; set; }
    }

    public class ContractItem
    {
        [JsonProperty("name")] public string Name { get; set; }
        [JsonProperty("address")] public string Address { get; set; }
        [JsonProperty("code_hash")] public string CodeHash { get; set; }
        [JsonProperty("owner")] public string Owner { get; set; }
    }

    public class ContractsInfo
    {
        [JsonProperty("AutoUpdate")] public bool AutoUpdate { get; set; }
        [JsonProperty("Contracts")] public List<ContractItem> Contracts { get; set; }
    }

    public class ConfigInfo
    {
        [JsonProperty("BpNodes")] public List<Node> BpNodes { get; set; }
        [JsonProperty("FullNodes")] public List<Node> FullNodes { get; set; }
        [JsonProperty("TestCases")] public List<TestCase> TestCases { get; set; }
        [JsonProperty("UserCount")] public int UserCount { get; set; }
        [JsonProperty("Timeout")] public int Timeout { get; set; }
        [JsonProperty("SpecifyEndpoint")] public SpecifyEndpoint SpecifyEndpoint { get; set; }
        [JsonProperty("ContractsInfo")] public ContractsInfo ContractsInfo { get; set; }
    }

    public static class ConfigInfoHelper
    {
        private static ConfigInfo _instance;
        private static string _jsonContent;
        private static readonly object LockObj = new object();
        private static readonly string ConfigFile = Path.Combine(Directory.GetCurrentDirectory(), "scenario-nodes-local.json");

        public static ConfigInfo Config => GetConfigInfo();

        public static List<string> GetAccounts()
        {
            var accounts = new List<string>();

            accounts.AddRange(Config.BpNodes.Select(o => o.Account));
            accounts.AddRange(Config.FullNodes.Select(o => o.Account));

            return accounts;
        }

        public static bool UpdateConfig(ContractsInfo info)
        {
            if (_jsonContent == null)
                _jsonContent = File.ReadAllText(ConfigFile);

            var configInfo = JsonConvert.DeserializeObject<ConfigInfo>(_jsonContent);
            configInfo.ContractsInfo = info;

            _jsonContent = JsonConvert.SerializeObject(configInfo, Formatting.Indented);
            File.WriteAllText(ConfigFile, _jsonContent);

            return true;
        }

        private static ConfigInfo GetConfigInfo()
        {
            lock (LockObj)
            {
                if (_instance != null) return _instance;

                _jsonContent = File.ReadAllText(ConfigFile);
                _instance = JsonConvert.DeserializeObject<ConfigInfo>(_jsonContent);
            }

            return _instance;
        }
    }
}