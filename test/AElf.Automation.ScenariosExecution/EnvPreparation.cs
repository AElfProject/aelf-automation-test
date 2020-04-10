using System.IO;
using System.Linq;
using AElfChain.Common;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using log4net;

namespace AElf.Automation.ScenariosExecution
{
    public class EnvPreparation
    {
        private static NodesInfo _config;
        private static ContractServices _services;
        private static readonly ILog Logger = Log4NetHelper.GetLogger();
        private static readonly string AccountDir = CommonHelper.GetCurrentDataDir();
        private static readonly EnvPreparation Instance = new EnvPreparation();

        private EnvPreparation()
        {
            _config = NodeInfoHelper.Config;
        }

        public static EnvPreparation GetDefaultEnvCheck()
        {
            return Instance;
        }

        public ContractServices GetContractServices()
        {
            if (_services != null)
                return _services.CloneServices();

            var specifyEndpoint = ScenarioConfig.ReadInformation.SpecifyEndpoint;
            var url = specifyEndpoint.Enable
                ? specifyEndpoint.ServiceUrl
                : _config.Nodes.First(o => o.Status).Endpoint;
            Logger.Info($"All request sent to endpoint: {url}");
            var nodeManager = new NodeManager(url, AccountDir);

            var bpAccount = _config.Nodes.First().Account;
            _services = new ContractServices(nodeManager, bpAccount);

            return _services.CloneServices();
        }

        public static void PrepareTestAccounts()
        {
            var keyPath = Path.Combine(CommonHelper.GetCurrentDataDir(), "keys");
            var backupPath = Path.Combine(CommonHelper.GetCurrentDataDir(), "keys_backup");
            var files = Directory.GetFiles(keyPath);

            //clean old accounts exclude node accounts
            var nodeAccounts = NodeInfoHelper.Config.Nodes.Select(o => o.Account).ToList();
            foreach (var file in files)
            {
                var fileName = Path.GetFileNameWithoutExtension(file);
                if (nodeAccounts.Contains(fileName)) continue;
                CommonHelper.CopyFiles(file, backupPath);
                File.Delete(file);
                Logger.Info($"Delete account file: {file.Split('/').Last()}");
            }

            //copy test accounts
            var accountPath = CommonHelper.MapPath("test-data/keys");
            foreach (var file in Directory.GetFiles(accountPath))
            {
                CommonHelper.CopyFiles(file, keyPath);
                Logger.Info($"Copy tester account: {file.Split('/').Last()}");
            }

            //copy test contract
            var defaultDir = CommonHelper.GetDefaultDataDir();
            var testContracts = CommonHelper.MapPath("test-data/contracts");
            var contractPath = Path.Combine(defaultDir, "contracts");
            foreach (var file in Directory.GetFiles(testContracts))
            {
                CommonHelper.CopyFiles(file, contractPath);
                Logger.Info($"Copy tester contract: {file.Split('/').Last()}");
            }
        }
    }
}