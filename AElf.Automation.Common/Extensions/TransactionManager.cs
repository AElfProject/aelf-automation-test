using System;
using System.IO;
using ProtoBuf;
using Newtonsoft.Json.Linq;
using AElf.Kernel;
using AElf.Cryptography;
using AElf.Common.ByteArrayHelpers;
using AElf.Cryptography.ECDSA;
using Transaction = AElf.Automation.Common.Protobuf.Transaction;
using TransactionType = AElf.Automation.Common.Protobuf.TransactionType;
using System.Security.Cryptography;
using System.Collections.Generic;
using System.Linq;
using AElf.Common.Extensions;

namespace AElf.Automation.Common.Extensions
{
    public class TransactionManager
    {
        private AElfKeyStore _keyStore;
        private CommandInfo _cmdInfo;

        public TransactionManager(AElfKeyStore keyStore)
        {
            _keyStore = keyStore;
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

            ECKeyPair kp = _keyStore.GetAccountKeyPair(addr);

            if (kp == null)
            {
                Console.WriteLine("The following account is locked:" + addr);
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
            tx.R = signature.R;
            tx.S = signature.S;

            tx.P = kp.PublicKey.Q.GetEncoded();

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
                Console.WriteLine("Invalid transaction data.");
                Console.WriteLine("Exception message: " + e.Message);

                return null;
            }
        }
    }

    public static class BlockMarkingHelper
    {
        private static DateTime _refBlockTime = DateTime.Now;
        private static ulong _cachedHeight;
        private static string _cachedHash;

        public static Transaction AddBlockReference(this Transaction transaction, string rpcAddress)
        {
            var height = _cachedHeight;
            var hash = _cachedHash;
            if (height == default(ulong) || (DateTime.Now - _refBlockTime).Seconds > 60)
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

        private static string GetBlkHeight(string rpcAddress)
        {
            var reqhttp = new RpcRequestManager(rpcAddress);
            string returnCode = string.Empty;
            var resp = reqhttp.PostRequest("get_block_height", "{}", out returnCode);
            var jObj = JObject.Parse(resp);
            return jObj["result"]["result"]["block_height"].ToString();
        }

        private static string GetBlkHash(string rpcAddress, string height)
        {
            var reqhttp = new RpcRequestManager(rpcAddress);
            string returnCode = string.Empty;
            var resp = reqhttp.PostRequest("get_block_info", "{\"block_height\":\""+ height +"\"}", out returnCode);
            var jObj = JObject.Parse(resp);
            return jObj["result"]["result"]["Blockhash"].ToString();
        }
    }
}
