using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;
using AElf.Cryptography;
using AElf.Common;
using System.Security.Cryptography;
using System.Linq;
using System.Threading;
using AElf.Automation.Common.Helpers;
using AElf.Types.CSharp;
using Google.Protobuf;
using ProtoBuf;
using Transaction = AElf.Automation.Common.Protobuf.Transaction;
using TransactionType = AElf.Automation.Common.Protobuf.TransactionType;
using Address = AElf.Automation.Common.Protobuf.Address;

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

        public Transaction CreateTransaction(string fromAddress, string genesisAddress, string incrementid,
            string methodName, byte[] serializedParams, TransactionType contracttransaction)
        {
            try
            {
                Transaction t = new Transaction();
                t.From = Address.Parse(fromAddress);
                t.To = Address.Parse(genesisAddress);
                t.IncrementId = Convert.ToUInt64(incrementid);
                t.MethodName = methodName;
                t.Params = serializedParams ?? ByteString.CopyFrom(ParamsPacker.Pack()).ToByteArray();
                t.Type = contracttransaction;
                _cmdInfo.Result = true;

                return t;
            }
            catch (Exception e)
            {
                _cmdInfo.ErrorMsg.Add($"Invalid transaction data: {e.Message}");
                return null;
            }
        }

        public Transaction SignTransaction(Transaction tx)
        {
            string addr = tx.From.GetFormatted();

            MemoryStream ms = new MemoryStream();
            Serializer.Serialize(ms, tx);
            tx.Sigs = new List<byte[]> { Sign(addr, ms.ToArray()) };
            return tx;
        }

        public byte[] Sign(string addr, byte[] txnData)
        {
            var kp = _keyStore.GetAccountKeyPair(addr);

            if (kp == null)
            {
                _cmdInfo.ErrorMsg.Add($"The following account is locked: {addr}");
                _cmdInfo.Result = false;
                return null;
            }

            // Sign the hash
            byte[] hash = SHA256.Create().ComputeHash(txnData);
            return CryptoHelpers.SignWithPrivateKey(kp.PrivateKey, hash);
        }

        public JObject ConvertTransactionRawTx(Transaction tx)
        {
            MemoryStream ms = new MemoryStream();
            Serializer.Serialize(ms, tx);

            byte[] b = ms.ToArray();
            string payload = b.ToHex();
            var reqParams = new JObject { ["rawTransaction"] = payload };

            return reqParams;
        }

        public Transaction ConvertFromJson(JObject j)
        {
            try
            {
                var tr = new Transaction
                {
                    From = Address.Parse(j["from"].ToString()),
                    To = Address.Parse(j["to"].ToString()),
                    MethodName = j["method"].ToObject<string>()
                };

                return tr;
            }
            catch (Exception e)
            {
                _logger.WriteError("Invalid transaction data.");
                _logger.WriteError($"Exception message: {e.Message}");

                return null;
            }
        }
    }

    public static class BlockMarkingHelper
    {
        private static DateTime _refBlockTime = DateTime.Now;
        private static ulong _cachedHeight;
        private static string _cachedHash;
        private static readonly ILogHelper Logger = LogHelper.GetLogHelper();

        public static Transaction AddBlockReference(this Transaction transaction, string rpcAddress)
        {
            var height = _cachedHeight;
            var hash = _cachedHash;
            if (height == default(ulong) || (DateTime.Now - _refBlockTime).TotalSeconds > 60)
            {
                height = ulong.Parse(GetBlkHeight(rpcAddress));
                hash = GetBlkHash(rpcAddress, height.ToString());
                _cachedHeight = height;
                _cachedHash = hash;
                _refBlockTime = DateTime.Now;
            }

            transaction.RefBlockNumber = height;
            transaction.RefBlockPrefix = ByteArrayHelpers.FromHexString(hash).Where((b, i) => i < 4).ToArray();
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
