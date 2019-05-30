using System;
using System.IO;
using System.Net;
using System.Security;
using AElf.Automation.Common.Helpers;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using AElf.Types;

namespace AElf.Automation.Common.OptionManagers
{
    public class AccountManager
    {
        private readonly AElfKeyStore _keyStore;
        private readonly string _chainId;

        public AccountManager(AElfKeyStore keyStore, string chainId)
        {
            _keyStore = keyStore;
            _chainId = chainId;
        }

        public CommandInfo NewAccount(string password = "")
        {
            var result = new CommandInfo(ApiMethods.AccountList);
            if (password == "")
                password = AskInvisible("password:");
            result.Parameter = password;
            var keypair = _keyStore.CreateAsync(password, _chainId).Result;
            var pubKey = keypair.PublicKey;

            var addr = Address.FromPublicKey(pubKey);
            if (addr != null)
            {
                result.Result = true;
                string account = addr.GetFormatted();
                result.InfoMsg = account;
            }

            return result;
        }

        public CommandInfo ListAccount()
        {
            var result = new CommandInfo(ApiMethods.AccountList);
            result.InfoMsg = _keyStore.ListAccountsAsync().Result;
            if (result.InfoMsg != null)
                result.Result = true;

            return result;
        }

        public CommandInfo UnlockAccount(string address, string password = "", string notimeout = "")
        {
            var result = new CommandInfo(ApiMethods.AccountUnlock);
            if (password == "")
                password = AskInvisible("password:");
            result.Parameter = $"{address} {password} {notimeout}";
            var accounts = _keyStore.ListAccountsAsync().Result;
            if (accounts == null || accounts.Count == 0)
            {
                result.ErrorMsg = "Error: the account '" + address + "' does not exist.";
                return result;
            }

            if (!accounts.Contains(address))
            {
                result.ErrorMsg = "Error: the account '" + address + "' does not exist.";
                return result;
            }

            var timeout = notimeout == "";
            var tryOpen = _keyStore.OpenAsync(address, password, timeout).Result;

            if (tryOpen == AElfKeyStore.Errors.WrongPassword)
                result.ErrorMsg = "Error: incorrect password!";
            else if (tryOpen == AElfKeyStore.Errors.AccountAlreadyUnlocked)
            {
                result.InfoMsg = "Account already unlocked!";
                result.Result = true;
            }
            else if (tryOpen == AElfKeyStore.Errors.None)
            {
                result.InfoMsg = "Account successfully unlocked!";
                result.Result = true;
            }

            return result;
        }

        public string GetPublicKey(string address, string password = "123")
        {
            var keyPair = GetKeyPair(address);
            return keyPair.PublicKey.ToHex();
        }

        public static string GetDefaultDataDir()
        {
            try
            {
                var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "aelf");
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);

                var keyPath = Path.Combine(path, "keys");
                if (!Directory.Exists(keyPath))
                    Directory.CreateDirectory(keyPath);

                return path;
            }
            catch (Exception)
            {
                return null;
            }
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