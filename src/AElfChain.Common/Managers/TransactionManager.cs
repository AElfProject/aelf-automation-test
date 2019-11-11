using System;
using System.Linq;
using System.Threading;
using AElf;
using AElf.Cryptography;
using AElf.Types;
using AElfChain.Common.Helpers;
using AElfChain.Common.Utils;
using AElfChain.SDK;
using AElfChain.SDK.Models;
using Google.Protobuf;
using log4net;
using Volo.Abp.Threading;

namespace AElfChain.Common.Managers
{
    public class TransactionManager
    {
        private static readonly ILog Logger = Log4NetHelper.GetLogger();
        private readonly AElfKeyStore _keyStore;

        public TransactionManager(AElfKeyStore keyStore)
        {
            _keyStore = keyStore;
        }

        public Transaction CreateTransaction(string from, string to,
            string methodName, ByteString input)
        {
            try
            {
                var transaction = new Transaction
                {
                    From = from.ConvertAddress(),
                    To = to.ConvertAddress(),
                    MethodName = methodName,
                    Params = input ?? ByteString.Empty
                };

                return transaction;
            }
            catch (Exception e)
            {
                Logger.Error($"Invalid transaction data: {e.Message}");
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
                Logger.Error($"The following account is locked: {addr}");
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
            if (_cachedHeight == default || (DateTime.Now - _refBlockTime).TotalSeconds > 60 ||
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