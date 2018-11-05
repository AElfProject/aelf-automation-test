using System;
using System.IO;
using ProtoBuf;
using Newtonsoft.Json.Linq;
using AElf.Cryptography;
using AElf.Common;
using AElf.Cryptography.ECDSA;
using Transaction = AElf.Automation.Common.Protobuf.Transaction;
using TransactionType = AElf.Automation.Common.Protobuf.TransactionType;
using System.Security.Cryptography;
using System.Linq;
using System.Net;
using System.Threading;
using Signature = AElf.Automation.Common.Protobuf.Signature;
using AElf.Automation.Common.Helpers;
using NLog;
using ServiceStack;

namespace AElf.Automation.Common.Extensions
{
    public class TransactionManager
    {
        private AElfKeyStore _keyStore;
        private CommandInfo _cmdInfo;
        private AccountManager _accountManager;
        private ILogHelper Logger = LogHelper.GetLogHelper();

        public TransactionManager(AElfKeyStore keyStore)
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

        public Transaction CreateTransaction(string elementAt, string genesisAddress, string incrementid,
            string methodName, byte[] serializedParams, TransactionType contracttransaction)
        {
            try
            {
                Transaction t = new Transaction();
                t.From = ByteArrayHelpers.FromHexString(elementAt);
                t.To = ByteArrayHelpers.FromHexString(genesisAddress);
                t.IncrementId = Convert.ToUInt64(incrementid);
                t.MethodName = methodName;
                t.Params = serializedParams;
                t.type = contracttransaction;
                _cmdInfo.Result = true;

                return t;
            }
            catch (Exception e)
            {
                _cmdInfo.ErrorMsg.Add("Invalid transaction data: " +e.Message);
                return null;
            }
        }

        public Transaction SignTransaction(Transaction tx)
        {
            string addr = tx.From.Value.ToHex();

            //ECKeyPair kp = _keyStore.GetAccountKeyPair(addr);
            ECKeyPair kp = _accountManager.GetKeyPair(addr);

            if (kp == null)
            {
                Logger.WriteInfo("The following account is locked:" + addr);
                return null;
            }

            MemoryStream ms = new MemoryStream();
            Serializer.Serialize(ms, tx);

            byte[] b = ms.ToArray();
            byte[] toSig = SHA256.Create().ComputeHash(b);

            // Sign the hash
            ECSigner signer = new ECSigner();
            ECSignature signature = signer.Sign(kp, toSig);

            // Update the signature
            tx.Sig = new Signature {R = signature.R, S = signature.S, P = kp.PublicKey.Q.GetEncoded()};
            return tx;
        }

        public JObject ConvertTransactionRawTx(Transaction tx)
        {
            MemoryStream ms = new MemoryStream();
            Serializer.Serialize(ms, tx);

            byte[] b = ms.ToArray();
            string payload = b.ToHex();
            var reqParams = new JObject { ["rawtx"] = payload };

            return reqParams;
        }

        public Transaction ConvertFromJson(JObject j)
        {
            try
            {
                Transaction tr = new Transaction();
                tr.From = ByteArrayHelpers.FromHexString(j["from"].ToString());
                tr.To = ByteArrayHelpers.FromHexString(j["to"].ToString());
                tr.IncrementId = j["incr"].ToObject<ulong>();
                tr.MethodName = j["method"].ToObject<string>();
                return tr;
            }
            catch (Exception e)
            {
                Logger.WriteError("Invalid transaction data.");
                Logger.WriteError("Exception message: " + e.Message);

                return null;
            }
        }
    }

    public static class BlockMarkingHelper
    {
        private static DateTime _refBlockTime = DateTime.Now;
        private static ulong _cachedHeight;
        private static string _cachedHash;
        private static ILogHelper Logger = LogHelper.GetLogHelper();

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
            string returnCode = string.Empty;
            var resp = reqhttp.PostRequest("get_block_height", "{}", out returnCode);
            Logger.WriteInfo("Query block height status: {0}, return message: {1}", returnCode, resp);
            if (returnCode != "OK")
            {
                if (requestTimes >= 0)
                {
                    Thread.Sleep(1000);
                    return GetBlkHeight(rpcAddress, requestTimes);
                }
                throw new Exception("Get Block height failed exception.");
            }
            var jObj = JObject.Parse(resp);
            return jObj["result"]["result"]["block_height"].ToString();
        }

        private static string GetBlkHash(string rpcAddress, string height, int requestTimes = 4)
        {
            requestTimes--;
            var reqhttp = new RpcRequestManager(rpcAddress);
            string returnCode = string.Empty;
            var resp = reqhttp.PostRequest("get_block_info", "{\"block_height\":\""+ height +"\"}", out returnCode);
            Logger.WriteInfo("Query block info status: {0}, return message: {1}", returnCode, resp);
            if (returnCode != "OK")
            {
                if (requestTimes >= 0)
                {
                    Thread.Sleep(1000);
                    return GetBlkHash(rpcAddress, height, requestTimes);
                }
                throw new Exception("Get Block hash failed exception.");
            }
            var jObj = JObject.Parse(resp);
            return jObj["result"]["result"]["Blockhash"].ToString();
        }
    }
}
