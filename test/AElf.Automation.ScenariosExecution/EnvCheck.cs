using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AElf.Automation.Common.OptionManagers;
using AElf.Automation.Common.Helpers;
using AElf.Automation.Common.WebApi;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AElf.Automation.ScenariosExecution
{
    public class EnvCheck
    {
        private string AccountDir { get; }
        private static ConfigInfo _config;
        private static readonly ILogHelper Logger = LogHelper.GetLogHelper();

        public EnvCheck()
        {
            AccountDir = AccountManager.GetDefaultDataDir();
            _config = ConfigInfoHelper.Config;
        }

        public void CheckInitialEnvironment()
        {
            var allAccountsExist = CheckAllAccountsExist();
            Assert.IsTrue(allAccountsExist, $"Node account file not found, should copy configured accounts to path: {AccountDir}");

            _ = CheckAllNodesConnection();
        }

        public List<string> GenerateOrGetTestUsers()
        {
            var baseUrl = _config.BpNodes.First(o => o.Status).ServiceUrl;
            var webHelper = new WebApiHelper(baseUrl, AccountDir);

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

        public ContractServices GetContractServices(string url = "")
        {
            if(url == "")
                url = _config.BpNodes.First(o => o.Status).ServiceUrl;
            var apiHelper = new WebApiHelper(url, AccountDir);
            
            GetConfigNodesPublicKey(apiHelper);
            
            return new ContractServices(apiHelper, GenerateOrGetTestUsers().First());
        }

 

        private static List<string> GenerateTestUsers(IApiHelper helper, int count)
        {
            
            var accounts = new List<string>();
            for (var i = 0; i < count; i++)
            {
                var command = new CommandInfo(ApiMethods.AccountNew, "123");
                command = helper.NewAccount(command);
                var account = command.InfoMsg.ToString();
                accounts.Add(account);
            }

            return accounts;
        }
        
        private bool CheckAllAccountsExist()
        {
            foreach (var bp in _config.BpNodes)
            {
                var result = CheckAccountExist(bp.Account);
                if (result)
                    continue;
                Logger.WriteError($"Node {bp.Name} account not found.");
                return false;
            }

            foreach (var full in _config.FullNodes)
            {
                var result = CheckAccountExist(full.Account);
                if (result)
                    continue;
                Logger.WriteError($"Node {full.Name} account not found.");
                return false;
            }

            return true;
        }

        private static bool CheckAllNodesConnection()
        {
            foreach (var node in _config.BpNodes)
            {
                var result = CheckNodeConnection(node);
                if(result) continue;
                return false;
            }
            foreach (var node in _config.BpNodes)
            {
                var result = CheckNodeConnection(node);
                if(result) continue;
                return false;
            }

            return true;
        }

        private static bool CheckNodeConnection(Node node)
        {
            var service = new WebApiService(node.ServiceUrl);
            try
            {
                var chainStatus = service.GetChainStatus().Result;
                if (chainStatus != null)
                {
                    node.Status = true;
                    node.ApiService = service;
                    return true;
                }
            }
            catch (Exception e)
            {
                Logger.WriteInfo($"Node {node.Name} connected failed due to exception: {e.Message}");
            }

            return false;
        }
            
        private bool CheckAccountExist(string account)
        {
            var path = Path.Combine(AccountDir, "keys", $"{account}.ak");
            return File.Exists(path);
        }

        private static void GetConfigNodesPublicKey(IApiHelper helper)
        {
            foreach (var node in _config.BpNodes)
            {
                node.PublicKey = helper.GetPublicKeyFromAddress(node.Account);
            }

            foreach (var node in _config.FullNodes)
            {
                node.PublicKey = helper.GetPublicKeyFromAddress(node.Account);
            }
        }
    }
}