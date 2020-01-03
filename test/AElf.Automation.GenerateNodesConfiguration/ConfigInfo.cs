using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace AElf.Automation.NodesConfigGen
{
    public class NodeInfo
    {
        [JsonProperty("name")] public string Name { get; set; }
        [JsonProperty("ip")] public string IpAddress { get; set; }
        [JsonProperty("db_no")] public int DbNo { get; set; }
        [JsonProperty("api_port")] public int ApiPort { get; set; }
        [JsonProperty("net_port")] public int NetPort { get; set; }
        [JsonIgnore] public string Account { get; set; }
        [JsonIgnore] public string PublicKey { get; set; }
    }

    public class NodesInformation
    {
        [JsonProperty("BpNodes")] public List<NodeInfo> BpNodes { get; set; }
        [JsonProperty("FullNodes")] public List<NodeInfo> FullNodes { get; set; }
    }

    public static class ConfigInfoHelper
    {
        private static NodesInformation _instance;
        private static readonly object LockObj = new object();

        public static NodesInformation Config => GetConfigInfo();

        private static NodesInformation GetConfigInfo()
        {
            lock (LockObj)
            {
                if (_instance != null) return _instance;

                var configFile = Path.Combine(Directory.GetCurrentDirectory(), "nodes-generate.json");
                var content = File.ReadAllText(configFile);
                _instance = JsonConvert.DeserializeObject<NodesInformation>(content);
            }

            return _instance;
        }
    }
}