using System;
using System.Collections.Generic;
using System.Net;
using System.Security;
using AElf.Automation.Common.Helpers;
using AElf.Cryptography.ECDSA;
using AElf.Types;
using Volo.Abp.Threading;

namespace AElf.Automation.Common.OptionManagers
{
    public class AccountManager
    {
        private readonly AElfKeyStore _keyStore;
        private List<string> _accounts;

        public AccountManager(AElfKeyStore keyStore)
        {
            _keyStore = keyStore;
            _accounts = AsyncHelper.RunSync(_keyStore.GetAccountsAsync);
        }

        public CommandInfo NewAccount(string password = "")
        {
            var result = new CommandInfo(ApiMethods.AccountNew);
            if (password == "")
                password = AskInvisible("password:");
            result.Parameter = password;
            var keypair = AsyncHelper.RunSync(() => _keyStore.CreateAccountKeyPairAsync(password));
            var pubKey = keypair.PublicKey;

            var addr = Address.FromPublicKey(pubKey);
            if (addr == null)
            {
                return result;
            }

            result.Result = true;
            var account = addr.GetFormatted();
            result.InfoMsg = account;
            _accounts.Add(account);

            return result;
        }

        public CommandInfo ListAccount()
        {
            var result = new CommandInfo(ApiMethods.AccountList)
            {
                InfoMsg = AsyncHelper.RunSync(_keyStore.GetAccountsAsync)
            };
            if (result.InfoMsg == null) return result;
            result.Result = true;
            _accounts = (List<string>) result.InfoMsg;

            return result;
        }

        public CommandInfo UnlockAccount(string address, string password = "", string notimeout = "")
        {
            var result = new CommandInfo(ApiMethods.AccountUnlock);
            if (password == "")
                password = AskInvisible("password:");
            result.Parameter = $"{address} {password} {notimeout}";
            if (_accounts == null || _accounts.Count == 0)
            {
                result.ErrorMsg = "Error: the account '" + address + "' does not exist.";
                return result;
            }

            if (!_accounts.Contains(address))
            {
                result.ErrorMsg = "Error: the account '" + address + "' does not exist.";
                return result;
            }

            var timeout = notimeout == "";
            var tryOpen = AsyncHelper.RunSync(() => _keyStore.UnlockAccountAsync(address, password, timeout));

            switch (tryOpen)
            {
                case KeyStoreErrors.WrongPassword:
                    result.ErrorMsg = "Error: incorrect password!";
                    break;
                case KeyStoreErrors.AccountAlreadyUnlocked:
                    result.InfoMsg = "Account already unlocked!";
                    result.Result = true;
                    break;
                case KeyStoreErrors.None:
                    result.InfoMsg = "Account successfully unlocked!";
                    result.Result = true;
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

        public string GetPublicKey(string address, string password = "123")
        {
            UnlockAccount(address, password);
            var keyPair = GetKeyPair(address);
            return keyPair.PublicKey.ToHex();
        }

        public bool AccountIsExist(string address)
        {
            if (_accounts == null)
                ListAccount();

            return _accounts.Contains(address);
        }

        private ECKeyPair GetKeyPair(string addr)
        {
            var kp = _keyStore.GetAccountKeyPair(addr);
            return kp;
        }

        private static string AskInvisible(string prefix)
        {
            Console.WriteLine(prefix);

            var pwd = new SecureString();
            while (true)
            {
                var i = Console.ReadKey(true);
                if (i.Key == ConsoleKey.Enter)
                {
                    break;
                }

                if (i.Key == ConsoleKey.Backspace)
                {
                    if (pwd.Length > 0)
                    {
                        pwd.RemoveAt(pwd.Length - 1);
                    }
                }
                else
                {
                    pwd.AppendChar(i.KeyChar);
                }
            }

            Console.WriteLine();

            return new NetworkCredential("", pwd).Password;
        }
    }
}