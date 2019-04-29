using System;
using Newtonsoft.Json.Linq;
using AElf.Cryptography;
using System.Linq;
using System.Threading;
using AElf.Automation.Common.Helpers;
using AElf.Kernel;
using Google.Protobuf;

namespace AElf.Automation.Common.Extensions
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
                _cmdInfo.ErrorMsg.Add($"Invalid transaction data: {e.Message}");
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
                _cmdInfo.ErrorMsg.Add($"The following account is locked: {addr}");
                _cmdInfo.Result = false;
                return null;
            }

            // Sign the hash
            var signature = CryptoHelpers.SignWithPrivateKey(kp.PrivateKey, txData);
            return ByteString.CopyFrom(signature);
        }

        public JObject ConvertTransactionRawTx(Transaction tx)
        {
            string payload = tx.ToByteArray().ToHex();
            var reqParams = new JObject { ["rawTransaction"] = payload };

            return reqParams;
        }
        
        public string ConvertTransactionRawTxString(Transaction tx)
        {
            return tx.ToByteArray().ToHex();
        }

        public Transaction ConvertFromJson(JObject jObject)
        {
            try
            {
                var tr = new Transaction
                {
                    From = Address.Parse(jObject["from"].ToString()),
                    To = Address.Parse(jObject["to"].ToString()),
                    MethodName = jObject["method"].ToObject<string>()
                };

                return tr;
            }
            catch (Exception)
            {
                return null;
            }
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
        private static readonly ILogHelper Logger = LogHelper.GetLogHelper();

        public static Transaction AddBlockReference(this Transaction transaction, string rpcAddress)
        {
            var height = _cachedHeight;
            var hash = _cachedHash;
            if (height == default(long) || (DateTime.Now - _refBlockTime).TotalSeconds > 60)
            {
                height = long.Parse(GetBlkHeight(rpcAddress));
                hash = GetBlkHash(rpcAddress, height.ToString());
                _cachedHeight = height;
                _cachedHash = hash;
                _refBlockTime = DateTime.Now;
            }

            transaction.RefBlockNumber = height;
            transaction.RefBlockPrefix = ByteString.CopyFrom(ByteArrayHelpers.FromHexString(hash).Where((b, i) => i < 4).ToArray());
            return transaction;
        }

        private static string GetBlkHeight(string rpcAddress, int requestTimes = 4)
        {
            requestTimes--;
            var reqhttp = new RpcRequestManager(rpcAddress);
            var resp = reqhttp.PostRequest("GetBlockHeight", "{}", out var returnCode);
            Logger.WriteInfo("Query block height status: {0}, return message: {1}", returnCode, resp);
            if (returnCode != "OK")
            {
                if (requestTimes >= 0)
                {
                    Thread.Sleep(500);
                    return GetBlkHeight(rpcAddress, requestTimes);
                }
                throw new Exception("Get Block height failed exception.");
            }
            var jObj = JObject.Parse(resp);
            return jObj["result"].ToString();
        }

        private static string GetBlkHash(string rpcAddress, string height, int requestTimes = 4)
        {
            requestTimes--;
            var reqhttp = new RpcRequestManager(rpcAddress);
            var resp = reqhttp.PostRequest("GetBlockInfo", "{\"blockHeight\":\""+ height +"\"}", out var returnCode);
            if (returnCode != "OK")
            {
                if (requestTimes >= 0)
                {
                    Thread.Sleep(500);
                    return GetBlkHash(rpcAddress, height, requestTimes);
                }
                throw new Exception("Get Block hash failed exception.");
            }
            var jObj = JObject.Parse(resp);
            return jObj["result"]["BlockHash"].ToString();
        }
    }
}
