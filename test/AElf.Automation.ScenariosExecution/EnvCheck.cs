using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AElf.Automation.Common.OptionManagers;
using AElf.Automation.Common.Helpers;
using AElf.Automation.Common.WebApi;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nito.AsyncEx;

namespace AElf.Automation.ScenariosExecution
{
    public class EnvCheck
    {
        private string AccountDir { get; }
        private static ConfigInfo _config;
        private static readonly ILogHelper Logger = LogHelper.GetLogHelper();
        private static ContractServices Services { get; set; }

        public static EnvCheck GetDefaultEnvCheck()
        {
            return Instance;
        }

        private static readonly EnvCheck Instance = new EnvCheck();

        private EnvCheck()
        {
            AccountDir = CommonHelper.GetCurrentDataDir();
            _config = ConfigInfoHelper.Config;

            CheckInitialEnvironment();
        }

        private void CheckInitialEnvironment()
        {
            var allAccountsExist = CheckAllAccountsExist();
            Assert.IsTrue(allAccountsExist,
                $"Node account file not found, should copy configured accounts to path: {AccountDir}");

            CheckAllNodesConnection();
        }

        public List<string> GenerateOrGetTestUsers()
        {
            var specifyEndpoint = ConfigInfoHelper.Config.SpecifyEndpoint;
            var url = specifyEndpoint.Enable
                ? specifyEndpoint.ServiceUrl
                : _config.BpNodes.First(o => o.Status).ServiceUrl;
            var webHelper = new WebApiHelper(url, AccountDir);

            var accountCommand = webHelper.ListAccounts();

            if (!(accountCommand.InfoMsg is List<string> accounts))
                return GenerateTestUsers(webHelper, _config.UserCount);
            {
                var testUsers = accounts.FindAll(o => !ConfigInfoHelper.GetAccounts().Contains(o));
                if (testUsers.Count >= _config.UserCount) return testUsers.Take(_config.UserCount).ToList();

                var newAccounts = GenerateTestUsers(webHelper, _config.UserCount - testUsers.Count);
                testUsers.AddRange(newAccounts);
                return testUsers;
            }
        }

        public ContractServices GetContractServices()
        {
            if (Services != null)
                return Services;

            var specifyEndpoint = ConfigInfoHelper.Config.SpecifyEndpoint;
            var url = specifyEndpoint.Enable
                ? specifyEndpoint.ServiceUrl
                : _config.BpNodes.First(o => o.Status).ServiceUrl;
            Logger.Info($"All request sent to endpoint: {url}");
            var apiHelper = new WebApiHelper(url, AccountDir);

            GetConfigNodesPublicKey(apiHelper);

            Services = new ContractServices(apiHelper, GenerateOrGetTestUsers().First());

            return Services;
        }

        private List<string> GenerateTestUsers(IApiHelper helper, int count)
        {
            var accounts = new List<string>();
            Parallel.For(0, count, i =>
            {
                var command = new CommandInfo(ApiMethods.AccountNew, "123");
                command = helper.NewAccount(command);
                var account = command.InfoMsg.ToString();
                accounts.Add(account);
            });

            return accounts;
        }

        private bool CheckAllAccountsExist()
        {
            foreach (var bp in _config.BpNodes)
            {
                var result = CheckAccountExist(bp.Account);
                if (result)
                    continue;
                Logger.Error($"Node {bp.Name} account key not found.");
                return false;
            }

            foreach (var full in _config.FullNodes)
            {
                var result = CheckAccountExist(full.Account);
                if (result)
                    continue;
                Logger.Error($"Node {full.Name} account key not found.");
                return false;
            }

            return true;
        }

        private void CheckAllNodesConnection()
        {
            Logger.Info("Check all node connection status.");
            _config.BpNodes.ForEach(CheckNodeConnection);
            _config.FullNodes.ForEach(CheckNodeConnection);
        }

        private void CheckNodeConnection(Node node)
        {
            var service = new WebApiService(node.ServiceUrl);
            try
            {
                node.ApiService = service;
                var chainStatus = service.GetChainStatus().Result;
                if (chainStatus == null) return;
                node.Status = true;
                var height = service.GetBlockHeight().Result;
                Logger.Info($"Node {node.Name} [{node.ServiceUrl}] connection success, block height: {height}");
            }
            catch (Exception ex)
            {
                Logger.Error($"Node {node.Name} connection failed due to {ex.Message}");
            }
        }

        private bool CheckAccountExist(string account)
        {
            var path = Path.Combine(AccountDir, "keys", $"{account}.ak");
            return File.Exists(path);
        }

        private static void GetConfigNodesPublicKey(IApiHelper helper)
        {
            _config.BpNodes.ForEach(node =>
            {
                node.PublicKey = helper.GetPublicKeyFromAddress(node.Account, node.Password);
                Logger.Info($"Node: {node.Name}, PublicKey: {node.PublicKey}");
            });
            _config.FullNodes.ForEach(node =>
            {
                node.PublicKey = helper.GetPublicKeyFromAddress(node.Account, node.Password);
                Logger.Info($"Node: {node.Name}, PublicKey: {node.PublicKey}");
            });
        }
    }
}