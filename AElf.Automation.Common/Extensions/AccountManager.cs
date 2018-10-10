using AElf.Cryptography;
using System;
using System.Net;
using System.Security;
using AElf.Cryptography.ECDSA;

namespace AElf.Automation.Common.Extensions
{
    public class AccountManager
    {
        private AElfKeyStore _keyStore;

        public AccountManager(AElfKeyStore keyStore)
        {
            _keyStore = keyStore;
        }

        public CommandInfo NewAccount(string password="")
        {
            var result = new CommandInfo("account new", "account");
            if (password == "")
                password = AskInvisible("password:");
            var keypair = _keyStore.Create(password);
            if(keypair !=null)
            {
                result.Result = true;
                result.InfoMsg.Add("Account address: " + keypair.GetAddressHex());
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
