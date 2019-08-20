using AElf;
using AElf.Types;

namespace AElfChain.AccountService
{
    public class AccountInfo
    {
        public byte[] PrivateKeys { get; }
        public byte[] PublicKeys { get; }
        
        public string PublicKeyHex => PublicKeys.ToHex();

        public Address Account => GetAddress();
        
        public string Formatted => Account.GetFormatted();
        
        private Address _account;
        private Address GetAddress()
        {
            if(_account == null)
                _account = Address.FromPublicKey(PublicKeys);

            return _account;
        }

        public AccountInfo(byte[] privateKeys, byte[] publicKeys)
        {
            PrivateKeys = privateKeys;
            PublicKeys = publicKeys;
        }
    }
}