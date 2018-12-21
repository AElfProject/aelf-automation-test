using AElf.Cryptography;
using System;
using System.IO;
using System.Net;
using System.Security;
using AElf.Common;
using AElf.Cryptography.ECDSA;

namespace AElf.Automation.Common.Extensions
{
    public class AccountManager
    {
        private AElfKeyStore _keyStore;
        private string _chainId;

        public AccountManager(AElfKeyStore keyStore, string chainId)
        {
            _keyStore = keyStore;
            _chainId = chainId;

        }

        public CommandInfo NewAccount(string password="")
        {
            var result = new CommandInfo("account new", "account");
            if (password == "")
                password = AskInvisible("password:");
            var keypair = _keyStore.Create(password, _chainId);
            var pubKey = keypair.PublicKey;

            var addr = Address.FromPublicKey(pubKey);
            if(addr !=null)
            {
                result.Result = true;
                string account = addr.GetFormatted();
                result.InfoMsg.Add("Account address: " + account);
            }

            return result;
        }

        public CommandInfo ListAccount()
        {
            var result = new CommandInfo("account list", "account");
            result.InfoMsg = _keyStore.ListAccounts();
            if (result.InfoMsg.Count != 0)
                result.Result = true;

            return result;
        }

        public CommandInfo UnlockAccount(string address, string password = "", string notimeout = "")
        {
            var result = new CommandInfo("account list", "account");
            if (password == "")
                password = AskInvisible("password:");
            result.Parameter = string.Format("{0} {1} {2}", address, password, notimeout);
            var accounts = _keyStore.ListAccounts();
            if (accounts == null || accounts.Count == 0)
            {
                result.ErrorMsg.Add("Error: the account '" + address + "' does not exist.");
                return result;
            }

            if (!accounts.Contains(address))
            {
                result.ErrorMsg.Add("Error: the account '" + address + "' does not exist.");
                return result;
            }

            bool timeout = (notimeout == "") ? true : false;
            var tryOpen = _keyStore.OpenAsync(address, password, timeout);

            if (tryOpen == AElfKeyStore.Errors.WrongPassword)
                result.ErrorMsg.Add("Error: incorrect password!");
            else if (tryOpen == AElfKeyStore.Errors.AccountAlreadyUnlocked)
            {
                result.InfoMsg.Add("Account already unlocked!");
                result.Result = true;
            }
            else if (tryOpen == AElfKeyStore.Errors.None)
            {
                result.InfoMsg.Add("Account successfully unlocked!");
                result.Result = true;
            }

            return result;
        }

        public ECKeyPair GetKeyPair(string addr)
        {
            ECKeyPair kp = _keyStore.GetAccountKeyPair(addr);
            return kp;
        }

        public static string GetDefaultDataDir()
        {
            try
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "aelf");
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);

                string keyPath = Path.Combine(path, "keys");
                if (!Directory.Exists(keyPath))
                    Directory.CreateDirectory(keyPath);

                return path;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private string AskInvisible(string prefix)
        {
            Console.WriteLine(prefix);

            var pwd = new SecureString();
            while (true)
            {
                ConsoleKeyInfo i = Console.ReadKey(true);
                if (i.Key == ConsoleKey.Enter)
                {
                    break;
                }
                else if (i.Key == ConsoleKey.Backspace)
                {
                    if (pwd.Length > 0)
                    {
                        pwd.RemoveAt(pwd.Length - 1);
                    }
                }
                else
                {
                    pwd.AppendChar(i.KeyChar);
                    //Console.Write("*");
                }
            }

            Console.WriteLine();

            return new NetworkCredential("", pwd).Password;
        }
    }
}
