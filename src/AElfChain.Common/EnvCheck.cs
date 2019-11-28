using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using AElfChain.SDK;
using log4net;

namespace AElfChain.Common
{
    public class EnvCheck
    {
        private static NodesInfo _config;
        private static readonly ILog Logger = Log4NetHelper.GetLogger();
        private static readonly string AccountDir = CommonHelper.GetCurrentDataDir();

        private static readonly EnvCheck Instance = new EnvCheck();
        
        public static EnvCheck GetDefaultEnvCheck()
        {
            return Instance;
        }
        
        private EnvCheck()
        {
            _config = NodeInfoHelper.Config;

            CheckInitialEnvironment();
            GetConfigNodesPublicKey();
        }

        private void CheckInitialEnvironment()
        {
            var allAccountsExist = CheckAllAccountsExist();
            if (!allAccountsExist)
                throw new Exception(
                    $"Node account file not found, should copy configured accounts to path: {AccountDir}");

            CheckAllNodesConnection();
        }

        public List<string> GenerateOrGetTestUsers(int count)
        {
            var url = _config.Nodes.First(o => o.Status).Endpoint;
            var webHelper = new NodeManager(url, AccountDir);

            var accounts = webHelper.ListAccounts();
            var testUsers = accounts.FindAll(o => !NodeInfoHelper.GetAccounts().Contains(o));
            if (testUsers.Count >= count) return testUsers.Take(count).ToList();

            var newAccounts = GenerateTestUsers(webHelper, count - testUsers.Count);
            testUsers.AddRange(newAccounts);
            return testUsers;
        }

        private List<string> GenerateTestUsers(INodeManager manager, int count)
        {
            var accounts = new List<string>();
            Parallel.For(0, count, i =>
            {
                var account = manager.NewAccount();
                accounts.Add(account);
            });

            return accounts;
        }

        private bool CheckAllAccountsExist()
        {
            foreach (var node in _config.Nodes)
            {
                var result = CheckAccountExist(node.Account);
                if (result)
                    continue;
                Logger.Error($"Node {node.Name} account key not found.");
                return false;
            }

            return true;
        }

        private void CheckAllNodesConnection()
        {
            Logger.Info("Check all node connection status.");
            _config.Nodes.ForEach(CheckNodeConnection);
        }

        private void CheckNodeConnection(Node node)
        {
            var service = AElfChainClient.GetClient(node.Endpoint);
            try
            {
                node.ApiService = service;
                var chainStatus = service.GetChainStatusAsync().Result;
                if (chainStatus == null) return;
                node.Status = true;
                var height = service.GetBlockHeightAsync().Result;
                Logger.Info($"Node {node.Name} [{node.Endpoint}] connection success, block height: {height}");
            }
            catch (Exception ex)
            {
                Logger.Error($"Node {node.Name} connection failed due to {ex.Message}");
            }
        }

        private bool CheckAccountExist(string account)
        {
            var path = Path.Combine(AccountDir, "keys", $"{account}.json");
            return File.Exists(path);
        }

        private static void GetConfigNodesPublicKey()
        {
            var accountManager = new AccountManager();
            _config.Nodes.ForEach(node =>
            {
                node.PublicKey = accountManager.GetPublicKey(node.Account, node.Password);
                Logger.Info($"Node: {node.Name}, PublicKey: {node.PublicKey}");
            });
        }
    }
}