using System.Collections.Generic;
using System.IO;
using System.Linq;
using AElf.Automation.Common.Contracts;
using AElf.Automation.Common.Helpers;
using Newtonsoft.Json;

namespace AElf.Automation.Common.OptionManagers.Authority
{
    public class Node
    {
        [JsonProperty("name")] public string Name { get; set; }
        [JsonProperty("account")] public string Account { get; set; }
        [JsonProperty("password")] public string Password { get; set; }
        [JsonIgnore] public string PublicKey { get; set; }
    }

    public class NodesInfo
    {
        [JsonProperty("RequireAuthority")] public bool RequireAuthority { get; set; }
        [JsonProperty("Nodes")] public List<Node> Nodes { get; set; }
        [JsonProperty("IsMainChain")] public bool IsMainChain { get; set; }

        public void CheckNodesAccount()
        {
            var keyStore = new AElfKeyStore(CommonHelper.GetCurrentDataDir());
            var accountManager = new AccountManager(keyStore);
            foreach (var node in Nodes)
            {
                //check account exist
                var exist = accountManager.AccountIsExist(node.Account);
                if (!exist)
                {
                    $"Account {node.Account} not exist, please copy account file into folder: {CommonHelper.GetCurrentDataDir()}/keys"
                        .WriteErrorLine();
                    throw new FileNotFoundException();
                }

                //get public key
                var publicKey = accountManager.GetPublicKey(node.Account, node.Password);
                node.PublicKey = publicKey;
            }
        }

        public List<Node> GetMinerNodes(ConsensusContract consensus)
        {
            var miners = consensus.GetCurrentMiners();
            return Nodes.Where(o => miners.Contains(o.PublicKey)).ToList();
        }
    }

    public static class NodeInfoHelper
    {
        private static NodesInfo _instance;
        private static string _jsonContent;
        private static readonly object LockObj = new object();

        public static NodesInfo Config => GetConfigInfo();

        private static NodesInfo GetConfigInfo()
        {
            lock (LockObj)
            {
                if (_instance != null) return _instance;

                _jsonContent = File.ReadAllText(ConfigFile);
                _instance = JsonConvert.DeserializeObject<NodesInfo>(_jsonContent);
            }

            return _instance;
        }

        private static readonly string ConfigFile = CommonHelper.MapPath("nodes-sidechain.json");
    }
}