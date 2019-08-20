using System.Collections.Generic;
using System.IO;
using System.Linq;
using AElf.Automation.Common.Contracts;
using AElf.Automation.Common.Helpers;
using Newtonsoft.Json;
using Volo.Abp.Threading;

namespace AElfChain.AccountService
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
            var accountManager = AccountManager.GetAccountManager();
            foreach (var node in Nodes)
            {
                //check account exist
                var exist = AsyncHelper.RunSync(()=>accountManager.AccountIsExist(node.Account));
                if (!exist)
                {
                    $"Account {node.Account} not exist, please copy account file into folder: {CommonHelper.GetCurrentDataDir()}/keys"
                        .WriteErrorLine();
                    throw new FileNotFoundException();
                }

                //get public key
                var accountInfo = AsyncHelper.RunSync(() => accountManager.GetAccountInfoAsync(node.Account, node.Password)); 
                node.PublicKey = accountInfo.PublicKeyHex;
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

                var configFile = CommonHelper.MapPath("nodes.json");
                _jsonContent = File.ReadAllText(configFile);
                _instance = JsonConvert.DeserializeObject<NodesInfo>(_jsonContent);
            }

            return _instance;
        }
    }
}