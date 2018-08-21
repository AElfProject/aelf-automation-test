using AElf.Cryptography;
using System;
using AElf.Kernel;
using Newtonsoft.Json.Linq;
using AElf.Common.ByteArrayHelpers;
using AElf.Cryptography.ECDSA;
using System.IO;
using ProtoBuf;

using Transaction = AElf.Automation.Common.Protobuf.Transaction;
using TransactionType = AElf.Automation.Common.Protobuf.TransactionType;
using System.Security.Cryptography;

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
                Console.WriteLine("The following account is locked:" + addr);

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
}
