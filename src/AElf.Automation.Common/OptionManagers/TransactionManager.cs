using System;
using System.Linq;
using AElf.Cryptography;
using System.Threading;
using AElf.Automation.Common.Helpers;
using AElf.Automation.Common.WebApi;
using AElf.Automation.Common.WebApi.Dto;
using AElf.Types;
using Google.Protobuf;
using Volo.Abp.Threading;

namespace AElf.Automation.Common.OptionManagers
{
    public class TransactionManager
    {
        private readonly AElfKeyStore _keyStore;
        private CommandInfo _cmdInfo;
        private AccountManager _accountManager;
        private readonly ILogHelper _logger = LogHelper.GetLogHelper();

        public TransactionManager(AElfKeyStore keyStore, string chainId)
        {
            _keyStore = keyStore;
            _accountManager = new AccountManager(keyStore, chainId);
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
                    From = Address.Parse(from),
                    To = Address.Parse(to),
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
            var txData = tx.GetHash().DumpByteArray();
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
                From = Address.Parse(commandInfo.From),
                To = Address.Parse(commandInfo.To),
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
            var height = _cachedHeight;
            var hash = _cachedHash;
            if (height == default(long) || (DateTime.Now - _refBlockTime).TotalSeconds > 30 ||
                !_chainId.Equals(chainId))
            {
                _chainId = chainId;
                height = GetBlkHeight(rpcAddress);
                hash = GetBlkHash(rpcAddress, height);
                _cachedHeight = height;
                _cachedHash = hash;
                _refBlockTime = DateTime.Now;
            }

            transaction.RefBlockNumber = height;
            transaction.RefBlockPrefix =
                ByteString.CopyFrom(ByteArrayHelper.FromHexString(hash).Where((b, i) => i < 4).ToArray());
            return transaction;
        }

        private static long GetBlkHeight(string baseUrl, int requestTimes = 4)
        {
            while (true)
            {
                requestTimes--;
                var webApi = new WebApiService(baseUrl);
                var height = AsyncHelper.RunSync(webApi.GetBlockHeight);
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
                var webApi = new WebApiService(baseUrl);
                var blockInfo = AsyncHelper.RunSync(() => webApi.GetBlockByHeight(height));
                if (blockInfo != null && blockInfo != new BlockDto()) return blockInfo.BlockHash;

                if (requestTimes < 0) throw new Exception("Get Block hash failed exception.");
                Thread.Sleep(500);
            }
        }
    }
}