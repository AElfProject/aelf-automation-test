using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace AElf.Automation.NetworkTest
{
    public class ConfigInfo
    {
        [JsonProperty("Type")] public string Type { get; set; }
        [JsonProperty("Nodes")] public List<Node> Nodes { get; set; }
    }

    public class Node
    {
        [JsonProperty("ListeningPort")] public string ListeningPort { get; set; }
    }

    public static class ConfigHelper
    {
        private static ConfigInfo _instance;
        private static string _jsonContent;
        private static readonly object LockObj = new object();

        public static ConfigInfo Config => GetConfigInfo();

        private static ConfigInfo GetConfigInfo()
        {
            lock (LockObj)
            {
                if (_instance != null) return _instance;

                var configFile = Path.Combine(Directory.GetCurrentDirectory(), "network.json");
                _jsonContent = File.ReadAllText(configFile);
                _instance = JsonConvert.DeserializeObject<ConfigInfo>(_jsonContent);
            }

            return _instance;
        }
    }
}