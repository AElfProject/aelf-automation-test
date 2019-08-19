using System;
using System.IO;
using AElf.Automation.Common.Helpers;
using AElf.Automation.Common.OptionManagers;
using AElf.Types;
using Volo.Abp.Threading;

namespace AElf.Automation.GenerateNodesConfiguration
{
    public class ConfigAccount
    {
        private string _keyPath;
        private IKeyStore _keyStore;
        private NodeOption _node;
        public ConfigAccount(NodeOption node)
        {
            var dataPath = CommonHelper.GetCurrentDataDir();
            _keyPath = Path.Combine(dataPath, "keys");
            _keyStore = new AElfKeyStore(dataPath);
            _node = node;
        }

        public string GenerateAccount()
        {
            var keypair = AsyncHelper.RunSync(() => _keyStore.CreateAccountKeyPairAsync("123"));
            var pubKey = keypair.PublicKey;
            _node.PublicKey = pubKey.ToHex();

            var addr = Address.FromPublicKey(pubKey);
            _node.Account = addr.GetFormatted();
            
            return _node.Account;
        }

        public void CopyAccount()
        {
            var originPath = Path.Combine(_keyPath, $"{_node.Account}.json");
            var desPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "results", _node.Name);
            CommonHelper.CopyFiles(originPath, desPath);
        }
    }
}