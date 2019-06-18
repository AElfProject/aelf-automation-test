using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        public string PublicKey { get; set; }

        public bool Status { get; set; } = false;

        public WebApiService ApiService { get; set; }
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

    public class ConfigInfo
    {
        [JsonProperty("BpNodes")] public List<Node> BpNodes { get; set; }

        [JsonProperty("FullNodes")] public List<Node> FullNodes { get; set; }

        [JsonProperty("TestCases")] public List<TestCase> TestCases { get; set; }

        [JsonProperty("UserCount")] public int UserCount { get; set; }

        [JsonProperty("Timeout")] public int Timeout { get; set; }

        [JsonProperty("SpecifyEndpoint")] public SpecifyEndpoint SpecifyEndpoint { get; set; }
    }

    public static class ConfigInfoHelper
    {
        private static ConfigInfo _instance = null;
        private static readonly object LockObj = new object();

        public static ConfigInfo Config => GetConfigInfo();

        public static List<string> GetAccounts()
        {
            var accounts = new List<string>();

            accounts.AddRange(Config.BpNodes.Select(o => o.Account));
            accounts.AddRange(Config.FullNodes.Select(o => o.Account));

            return accounts;
        }

        private static ConfigInfo GetConfigInfo()
        {
            lock (LockObj)
            {
                if (_instance != null) return _instance;

                var configFile = Path.Combine(Directory.GetCurrentDirectory(), "scenario-nodes.json");
                var content = File.ReadAllText(configFile);
                _instance = JsonConvert.DeserializeObject<ConfigInfo>(content);
            }

            return _instance;
        }
    }
}