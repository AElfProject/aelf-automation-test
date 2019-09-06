﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Security;
using AElf.Automation.Common.Helpers;
using AElf.Cryptography.ECDSA;
using AElf.Types;
using log4net;
using Volo.Abp.Threading;

namespace AElf.Automation.Common.Managers
{
    public class AccountManager
    {
        private static readonly ILog Logger = Log4NetHelper.GetLogger();
        private readonly AElfKeyStore _keyStore;
        private List<string> _accounts;

        public AccountManager(AElfKeyStore keyStore)
        {
            _keyStore = keyStore;
            _accounts = AsyncHelper.RunSync(_keyStore.GetAccountsAsync);
        }

        public string NewAccount(string password = "")
        {
            if (password == "")
                password = Account.DefaultPassword;

            if (password == "")
                password = AskInvisible("password:");

            var keypair = AsyncHelper.RunSync(() => _keyStore.CreateAccountKeyPairAsync(password));
            var pubKey = keypair.PublicKey;
            var address = Address.FromPublicKey(pubKey);

            return address.GetFormatted();
        }

        public List<string> ListAccount()
        {
            _accounts = AsyncHelper.RunSync(_keyStore.GetAccountsAsync);
            return _accounts;
        }

        public bool UnlockAccount(string address, string password = "")
        {
            if (password == "")
                password = Account.DefaultPassword;

            if (Account.DefaultPassword == "")
                password = AskInvisible("password:");

            if (_accounts == null || _accounts.Count == 0)
            {
                Logger.Error("No account exist in key store.");
                return false;
            }

            if (!_accounts.Contains(address))
            {
                Logger.Error($"Account '{address}' does not exist.");
                return false;
            }

            var tryOpen = AsyncHelper.RunSync(() => _keyStore.UnlockAccountAsync(address, password, false));

            switch (tryOpen)
            {
                case KeyStoreErrors.WrongPassword:
                    Logger.Error("Incorrect password!");
                    break;
                case KeyStoreErrors.AccountAlreadyUnlocked:
                    return true;
                case KeyStoreErrors.None:
                    Logger.Info($"Account '{address}' successfully unlocked!");
                    return true;
                case KeyStoreErrors.WrongAccountFormat:
                    break;
                case KeyStoreErrors.AccountFileNotFound:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return false;
        }

        public string GetPublicKey(string address, string password = "")
        {
            if (password == "")
                password = Account.DefaultPassword;

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
                if (i.Key == ConsoleKey.Enter) break;

                if (i.Key == ConsoleKey.Backspace)
                {
                    if (pwd.Length > 0) pwd.RemoveAt(pwd.Length - 1);
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