using System.IO;
using AElfChain.Common;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using AElf.Types;
using AElfChain.Common;
using Volo.Abp.Threading;

namespace AElf.Automation.GenerateNodesConfiguration
{
    public class ConfigAccount
    {
        private string _keyPath;
        private IKeyStore _keyStore;
        private NodeInfo _node;
        public ConfigAccount(NodeInfo node)
        {
            var dataPath = CommonHelper.GetCurrentDataDir();
            _keyPath = Path.Combine(dataPath, "keys");
            _keyStore = AElfKeyStore.GetKeyStore(dataPath);
            _node = node;
        }

        public string GenerateAccount()
        {
            var keypair = AsyncHelper.RunSync(() => _keyStore.CreateAccountKeyPairAsync(NodeOption.DefaultPassword));
            var pubKey = keypair.PublicKey;
            _node.PublicKey = pubKey.ToHex();

            var addr = Address.FromPublicKey(pubKey);
            _node.Account = addr.GetFormatted();
            
            return _node.Account;
        }

        public void CopyAccount()
        {
            var originPath = Path.Combine(_keyPath, $"{_node.Account}.json");
            var desPath = Path.Combine(CommonHelper.AppRoot, "results", _node.Name);
            var keysPath = Path.Combine(CommonHelper.AppRoot, "results", "keys");
            
            CommonHelper.CopyFiles(originPath, desPath);
            CommonHelper.CopyFiles(originPath, keysPath);
        }
    }
}