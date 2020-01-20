using System;
using System.Linq;
using System.Threading;
using AElf;
using AElf.Cryptography;
using AElf.Types;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
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
                (_cachedHeight, _cachedHash) = GetBlockReference(rpcAddress);
                _refBlockTime = DateTime.Now;
            }

            transaction.RefBlockNumber = _cachedHeight;
            transaction.RefBlockPrefix =
                ByteString.CopyFrom(ByteArrayHelper.HexStringToByteArray(_cachedHash).Where((b, i) => i < 4).ToArray());
            return transaction;
        }

        private static (long, string) GetBlockReference(string baseUrl, int requestTimes = 4)
        {
            while (true)
                try
                {
                    var client = AElfClientExtension.GetClient(baseUrl);
                    var chainStatus = AsyncHelper.RunSync(client.GetChainStatusAsync);
                    if (chainStatus.LongestChainHeight - chainStatus.LastIrreversibleBlockHeight > 400)
                    {
                        Thread.Sleep(5000);
                        $"Warning: chain longest chain and lib interval {chainStatus.LastIrreversibleBlockHeight}=>{chainStatus.LongestChainHeight} over 400."
                            .WriteWarningLine();
                        continue;
                    }

                    //return (chainStatus.LastIrreversibleBlockHeight, chainStatus.LastIrreversibleBlockHash);
                    return (chainStatus.BestChainHeight, chainStatus.BestChainHash);
                }
                catch (Exception)
                {
                    requestTimes--;
                    if (requestTimes < 0) throw new Exception("Get chain status got failed exception.");
                    Thread.Sleep(500);
                }
        }
    }
}