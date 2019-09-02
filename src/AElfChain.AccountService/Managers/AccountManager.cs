using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Automation.Common.Helpers;
using AElf.Automation.Common.Utils;
using AElf.Cryptography;
using AElf.Types;
using AElfChain.AccountService.KeyAccount;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Threading;

namespace AElfChain.AccountService
{
    public class AccountManager : IAccountManager, ISingletonDependency
    {
        private readonly IKeyStore _keyStore;
        private readonly List<string> _accounts;
        private readonly List<AccountInfo> _accountInfos;

        public ILogger Logger { get; set; }

        public static IAccountManager GetAccountManager(string dataPath = "")
        {
            var option = new AccountOption
            {
                DataPath = dataPath == "" ? CommonHelper.GetCurrentDataDir() : dataPath
            };
            
            return new AccountManager(option);
        }

        public AccountManager(IKeyStore keyStore)
        {
            Logger = NullLogger<AccountManager>.Instance;
            
            _keyStore = keyStore;
            _accounts = AsyncHelper.RunSync(_keyStore.GetAccountsAsync);
            _accountInfos = new List<AccountInfo>();
        }
        
        public AccountManager(AccountOption option)
        {
            _keyStore = new AElfKeyStore(option.DataPath);
            _accounts = AsyncHelper.RunSync(_keyStore.GetAccountsAsync);
            _accountInfos = new List<AccountInfo>();
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
            var accountInfo = new AccountInfo(keypair.PrivateKey, keypair.PublicKey);
            await _keyStore.UnlockAccountAsync(accountInfo.Formatted, password);
            _accountInfos.Add(accountInfo);
            
            return accountInfo;
        }

        public async Task<bool> UnlockAccountAsync(string account, string password = AccountOption.DefaultPassword,
            bool notimeout = true)
        {
            var result = false;
            if (_accounts == null || _accounts.Count == 0)
            {
                Logger.LogError($"Error: the account '{account}' does not exist.");
                return false;
            }

            if (!_accounts.Contains(account))
            {
                Logger.LogError($"Error: the account '{account}' does not exist.");
                return false;
            }

            var tryOpen = await _keyStore.UnlockAccountAsync(account, password, notimeout);

            switch (tryOpen)
            {
                case KeyStoreErrors.WrongPassword:
                    Logger.LogError("Error: incorrect password!");
                    break;
                case KeyStoreErrors.AccountAlreadyUnlocked:
                    Logger.LogInformation("Account already unlocked!");
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

        public async Task<AccountInfo> GetAccountInfoAsync(string account,
            string password = AccountOption.DefaultPassword)
        {
            if (_accountInfos.Select(o => o.Formatted).Contains(account))
                return _accountInfos.First(o => o.Formatted == account);
            
            var kp = _keyStore.GetAccountKeyPair(account) ?? await _keyStore.ReadKeyPairAsync(account, password);

            var accountInfo = new AccountInfo(kp.PrivateKey, kp.PublicKey);
            _accountInfos.Add(accountInfo);

            return accountInfo;
        }

        public async Task<AccountInfo> GetRandomAccountInfoAsync()
        {
            if (_accountInfos.Count >= 1)
            {
                _accountInfos.Shuffle();
                return _accountInfos.FirstOrDefault();
            }
            
            _accounts.Shuffle();
            var account = _accounts.FirstOrDefault();
            
            var kp = _keyStore.GetAccountKeyPair(account) ??
                     await _keyStore.ReadKeyPairAsync(account, AccountOption.DefaultPassword);

            var accountInfo = new AccountInfo(kp.PrivateKey, kp.PublicKey);
            if(!_accountInfos.Select(o =>o.Formatted == account).Any())
                _accountInfos.Add(accountInfo);
            
            return accountInfo;
        }

        public async Task<Transaction> SignTransactionAsync(Transaction transaction)
        {
            var accountInfo = await GetAccountInfoAsync(transaction.From.GetFormatted());
            var txData = transaction.GetHash().ToByteArray();
            var signature = CryptoHelper.SignWithPrivateKey(accountInfo.PrivateKeys, txData);
            transaction.Signature = ByteString.CopyFrom(signature);

            return transaction;
        }
    }
}