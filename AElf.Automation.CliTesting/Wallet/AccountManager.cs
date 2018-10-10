using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using AElf.Automation.CliTesting.Command;
using AElf.Automation.CliTesting.Parsing;
using AElf.Automation.CliTesting.Screen;
using AElf.Automation.CliTesting.Wallet.Exceptions;
using AElf.Common;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using ProtoBuf;
using Transaction = AElf.Automation.CliTesting.Data.Protobuf.Transaction;

namespace AElf.Automation.CliTesting.Wallet
{
    public class AccountManager
    {
        private const string NewCmdName = "new";
        private const string ListAccountsCmdName = "list";
        private const string UnlockAccountCmdName = "unlock";

        private AElfKeyStore _keyStore;
        private ScreenManager _screenManager;

        public AccountManager(AElfKeyStore keyStore, ScreenManager screenManager)
        {
            _screenManager = screenManager;
            _keyStore = keyStore;
        }

        public readonly List<string> SubCommands = new List<string>()
        {
            NewCmdName,
            ListAccountsCmdName,
            UnlockAccountCmdName
        };

        private string Validate(CmdParseResult parsedCmd)
        {
            if (parsedCmd.Args.Count == 0)
                return CliCommandDefinition.InvalidParamsError;

            if (parsedCmd.Args.ElementAt(0).Equals(UnlockAccountCmdName, StringComparison.OrdinalIgnoreCase))
            {
                if (parsedCmd.Args.Count < 2)
                    return CliCommandDefinition.InvalidParamsError;
            }

            if (!SubCommands.Contains(parsedCmd.Args.ElementAt(0)))
                return CliCommandDefinition.InvalidParamsError;

            return null;
        }

        public void ProcessCommand(CmdParseResult parsedCmd)
        {
            string validationError = Validate(parsedCmd);

            if (validationError != null)
            {
                _screenManager.PrintError(validationError);
                return;
            }

            string subCommand = parsedCmd.Args.ElementAt(0);

            if (subCommand.Equals(NewCmdName, StringComparison.OrdinalIgnoreCase))
            {
                if (parsedCmd.Args.Count == 2)
                {
                    string password = parsedCmd.Args.ElementAt(1);
                    CreateNewAccount(password);
                }
                else
                {
                    CreateNewAccount();
                }
            }
            else if (subCommand.Equals(ListAccountsCmdName, StringComparison.OrdinalIgnoreCase))
            {
                ListAccounts();
            }
            else if (subCommand.Equals(UnlockAccountCmdName, StringComparison.OrdinalIgnoreCase))
            {
                if (parsedCmd.Args.Count == 2)
                {
                    UnlockAccount(parsedCmd.Args.ElementAt(1));
                }
                else if (parsedCmd.Args.Count == 3)
                {
                    UnlockAccount(parsedCmd.Args.ElementAt(1), parsedCmd.Args.ElementAt(2));
                }
                else if (parsedCmd.Args.Count == 4)
                {
                    UnlockAccount(parsedCmd.Args.ElementAt(1), parsedCmd.Args.ElementAt(2), false);
                }
                else
                {
                    _screenManager.PrintError("wrong arguments.");
                }
            }
        }


        private void UnlockAccount(string address, bool timeout = true)
        {
            var accounts = _keyStore.ListAccounts();

            if (accounts == null || accounts.Count <= 0)
            {
                _screenManager.PrintError("error: the account '" + address + "' does not exist.");
                return;
            }

            if (!accounts.Contains(address))
            {
                _screenManager.PrintError("account does not exist!");
                return;
            }

            var password = _screenManager.AskInvisible("password: ");
            var tryOpen = _keyStore.OpenAsync(address, password, timeout);

            if (tryOpen == AElfKeyStore.Errors.WrongPassword)
                _screenManager.PrintError("incorrect password!");
            else if (tryOpen == AElfKeyStore.Errors.AccountAlreadyUnlocked)
                _screenManager.PrintError("account already unlocked!");
            else if (tryOpen == AElfKeyStore.Errors.None)
                _screenManager.PrintLine("account successfully unlocked!");
        }

        private void UnlockAccount(string address, string password, bool timeout = true)
        {
            var accounts = _keyStore.ListAccounts();

            if (accounts == null || accounts.Count <= 0)
            {
                _screenManager.PrintError("error: the account '" + address + "' does not exist.");
                return;
            }

            if (!accounts.Contains(address))
            {
                _screenManager.PrintError("account does not exist!");
                return;
            }

            var tryOpen = _keyStore.OpenAsync(address, password, timeout);

            if (tryOpen == AElfKeyStore.Errors.WrongPassword)
                _screenManager.PrintError("incorrect password!");
            else if (tryOpen == AElfKeyStore.Errors.AccountAlreadyUnlocked)
                _screenManager.PrintError("account already unlocked!");
            else if (tryOpen == AElfKeyStore.Errors.None)
                _screenManager.PrintLine("account successfully unlocked!");
        }

        private void CreateNewAccount()
        {
            var password = _screenManager.AskInvisible("password: ");
            var keypair = _keyStore.Create(password);
            if (keypair != null)
                _screenManager.PrintLine("account successfully created!");
        }

        private void CreateNewAccount(string password)
        {
            //var password = _screenManager.AskInvisible("password: ");
            var keypair = _keyStore.Create(password);
            if (keypair != null)
                _screenManager.PrintLine("account successfully created!");
        }

        private void ListAccounts()
        {
            List<string> accnts = _keyStore.ListAccounts();

            for (int i = 0; i < accnts.Count; i++)
            {
                _screenManager.PrintLine("account #" + i + " : " + accnts.ElementAt(i));
            }

            if (accnts.Count == 0)
                _screenManager.PrintLine("no accounts available");
        }

        public ECKeyPair GetKeyPair(string addr)
        {
            ECKeyPair kp = _keyStore.GetAccountKeyPair(addr);
            return kp;
        }

        /*public Transaction SignTransaction(JObject t)
        {
            Transaction tr = new Transaction();

            try
            {
                tr.From = ByteArrayHelpers.FromHexString(addr);
                tr.To = Convert.FromBase64String(t["to"].ToString());
                tr.IncrementId = t["incr"].ToObject<ulong>();
                tr.MethodName = t["method"].ToObject<string>();
                var p = Convert.FromBase64String(t["params"].ToString());
                tr.Params = p.Length == 0 ? null : p;

                SignTransaction(tr);

                return tr;
            }
            catch (Exception e)
            {
                ;
            }
            
            return null;
        }*/

        public Transaction SignTransaction(Transaction tx)
        {
            string addr = tx.From.Value.ToHex();

            ECKeyPair kp = _keyStore.GetAccountKeyPair(addr);

            if (kp == null)
                throw new AccountLockedException(addr);

            MemoryStream ms = new MemoryStream();
            Serializer.Serialize(ms, tx);

            byte[] b = ms.ToArray();
            byte[] toSig = SHA256.Create().ComputeHash(b);

            // Sign the hash
            ECSigner signer = new ECSigner();
            ECSignature signature = signer.Sign(kp, toSig);

            // Update the signature
            tx.R = signature.R;
            tx.S = signature.S;

            tx.P = kp.PublicKey.Q.GetEncoded();

            return tx;
        }
    }
}