using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Automation.Common.Helpers;
using AElf.Cryptography;
using AElfChain.AccountService.KeyAccount;
using log4net;
using Volo.Abp.Threading;

namespace AElfChain.AccountService
{
    public class AccountManager : IAccountManager
    {
        private readonly AElfKeyStore _keyStore;
        private readonly List<string> _accounts;

        public ILog Logger { get; set; }

        public static IAccountManager GetAccountManager(string dataPath = "")
        {
            var option = new AccountOption
            {
                DataPath = dataPath == "" ? CommonHelper.GetCurrentDataDir() : dataPath
            };

            return new AccountManager(option);
        }

        private AccountManager(AccountOption option)
        {
            _keyStore = new AElfKeyStore(option.DataPath);
            _accounts = AsyncHelper.RunSync(_keyStore.GetAccountsAsync);
        }

        public async Task<List<string>> ListAccount()
        {
            return await _keyStore.GetAccountsAsync();
        }

        public async Task<bool> AccountIsExist(string account)
        {
            if (_accounts == null)
                await ListAccount();

            return _accounts != null && _accounts.Contains(account);
        }

        public async Task<AccountInfo> NewAccountAsync(string password)
        {
            var keypair = await _keyStore.CreateAccountKeyPairAsync(password);

            return new AccountInfo(keypair.PrivateKey, keypair.PublicKey);
        }

        public async Task<bool> UnlockAccountAsync(string account, string password = "123", bool notimeout = true)
        {
            var result = false;
            if (_accounts == null || _accounts.Count == 0)
            {
                Logger.Error($"Error: the account '{account}' does not exist.");
                return false;
            }

            if (!_accounts.Contains(account))
            {
                Logger.Error($"Error: the account '{account}' does not exist.");
                return false;
            }

            var tryOpen = await _keyStore.UnlockAccountAsync(account, password, notimeout);

            switch (tryOpen)
            {
                case KeyStoreErrors.WrongPassword:
                    Logger.Error("Error: incorrect password!");
                    break;
                case KeyStoreErrors.AccountAlreadyUnlocked:
                    Logger.Info("Account already unlocked!");
                    result = true;
                    break;
                case KeyStoreErrors.None:
                    result = true;
                    break;
                case KeyStoreErrors.WrongAccountFormat:
                    break;
                case KeyStoreErrors.AccountFileNotFound:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return result;
        }

        public async Task<AccountInfo> GetAccountInfoAsync(string account, string password = "123")
        {
            var kp = _keyStore.GetAccountKeyPair(account) ?? await _keyStore.ReadKeyPairAsync(account, password);

            return new AccountInfo(kp.PrivateKey, kp.PublicKey);
        }

        public async Task<byte[]> SignAsync(AccountInfo accountInfo, byte[] data)
        {
            var signature = CryptoHelper.SignWithPrivateKey(accountInfo.PrivateKeys, data);
            return await Task.FromResult(signature);
        }

        public async Task<byte[]> SignAsync(string account, string password, byte[] data)
        {
            var accountInfo = await GetAccountInfoAsync(account, password);

            return await SignAsync(accountInfo, data);
        }
    }
}