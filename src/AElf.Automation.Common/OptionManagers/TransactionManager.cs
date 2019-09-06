using System;
using System.Linq;
using System.Threading;
using AElf.Automation.Common.Helpers;
using AElf.Cryptography;
using AElf.Types;
using AElfChain.SDK;
using AElfChain.SDK.Models;
using Google.Protobuf;
using log4net;
using Volo.Abp.Threading;

namespace AElf.Automation.Common.OptionManagers
{
    public class TransactionManager
    {
        private readonly AElfKeyStore _keyStore;
        private CommandInfo _cmdInfo;
        private AccountManager _accountManager;
        private static readonly ILog Logger = Log4NetHelper.GetLogger();

        public TransactionManager(AElfKeyStore keyStore, string chainId)
        {
            _keyStore = keyStore;
            _accountManager = new AccountManager(keyStore);
        }

        public TransactionManager(AElfKeyStore keyStore, CommandInfo ci)
        {
            _keyStore = keyStore;
            _cmdInfo = ci;
        }

        public void SetCmdInfo(CommandInfo ci)
        {
            _cmdInfo = ci;
        }

        public Transaction CreateTransaction(string from, string to,
            string methodName, ByteString input)
        {
            try
            {
                var transaction = new Transaction
                {
                    From = AddressHelper.Base58StringToAddress(from),
                    To = AddressHelper.Base58StringToAddress(to),
                    MethodName = methodName,
                    Params = input ?? ByteString.Empty
                };

                _cmdInfo.Result = true;

                return transaction;
            }
            catch (Exception e)
            {
                _cmdInfo.ErrorMsg = $"Invalid transaction data: {e.Message}";
                return null;
            }
        }

        public Transaction SignTransaction(Transaction tx)
        {
            var txData = tx.GetHash().ToByteArray();
            tx.Signature = Sign(tx.From.GetFormatted(), txData);
            return tx;
        }

        public ByteString Sign(string addr, byte[] txData)
        {
            var kp = _keyStore.GetAccountKeyPair(addr);

            if (kp == null)
            {
                _cmdInfo.ErrorMsg = $"The following account is locked: {addr}";
                _cmdInfo.Result = false;
                return null;
            }

            // Sign the hash
            var signature = CryptoHelper.SignWithPrivateKey(kp.PrivateKey, txData);
            return ByteString.CopyFrom(signature);
        }

        public string ConvertTransactionRawTxString(Transaction tx)
        {
            return tx.ToByteArray().ToHex();
        }

        public Transaction ConvertFromCommandInfo(CommandInfo commandInfo)
        {
            var tr = new Transaction
            {
                From = AddressHelper.Base58StringToAddress(commandInfo.From),
                To = AddressHelper.Base58StringToAddress(commandInfo.To),
                MethodName = commandInfo.ContractMethod
            };

            return tr;
        }
    }

    public static class BlockMarkingHelper
    {
        private static DateTime _refBlockTime = DateTime.Now;
        private static long _cachedHeight;
        private static string _cachedHash;
        private static string _chainId = string.Empty;

        public static Transaction AddBlockReference(this Transaction transaction, string rpcAddress,
            string chainId = "AELF")
        {
            if (_cachedHeight == default(long) || (DateTime.Now - _refBlockTime).TotalSeconds > 60 ||
                !_chainId.Equals(chainId))
            {
                _chainId = chainId;
                _cachedHeight = GetBlkHeight(rpcAddress);
                _cachedHash = GetBlkHash(rpcAddress, _cachedHeight);
                _refBlockTime = DateTime.Now;
            }

            transaction.RefBlockNumber = _cachedHeight;
            transaction.RefBlockPrefix =
                ByteString.CopyFrom(ByteArrayHelper.HexStringToByteArray(_cachedHash).Where((b, i) => i < 4).ToArray());
            return transaction;
        }

        private static long GetBlkHeight(string baseUrl, int requestTimes = 4)
        {
            while (true)
            {
                requestTimes--;
                var client = AElfChainClient.GetClient(baseUrl);
                var height = AsyncHelper.RunSync(client.GetBlockHeightAsync);
                if (height != 0) return height;

                if (requestTimes < 0) throw new Exception("Get Block height failed exception.");
                Thread.Sleep(500);
            }
        }

        private static string GetBlkHash(string baseUrl, long height, int requestTimes = 4)
        {
            while (true)
            {
                requestTimes--;
                var client = AElfChainClient.GetClient(baseUrl);
                var blockInfo = AsyncHelper.RunSync(() => client.GetBlockByHeightAsync(height));
                if (blockInfo != null && blockInfo != new BlockDto()) return blockInfo.BlockHash;

                if (requestTimes < 0) throw new Exception("Get Block hash failed exception.");
                Thread.Sleep(500);
            }
        }
    }
}