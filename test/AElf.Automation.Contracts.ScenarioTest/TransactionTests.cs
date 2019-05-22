using System;
using System.Collections.Generic;
using System.Diagnostics;
using AElf.Cryptography;
using AElf.Kernel;
using AElf.Types;
using Google.Protobuf;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AElf.Automation.Contracts.ScenarioTest
{
    [TestClass]
    public class TransactionTests
    {
        [TestMethod]
        [DataRow(1000)]
        public void VerifyTransactionSignatureVerify(int txCount)
        {
            var transactionList = GenerateTransactions(txCount);
            var stopwatch = new Stopwatch();
            
            stopwatch.Start();
            foreach (var tx in transactionList)
            {
                tx.VerifySignature();
            }
            stopwatch.Stop();
            
            var timeSpan = stopwatch.ElapsedMilliseconds;
            Debug.WriteLine($"TimeSpan: {timeSpan}");
        }

        private IEnumerable<Transaction> GenerateTransactions(int count)
        {
            var transactions = new List<Transaction>();
            for (var i = 0; i < count; i++)
            {
                var tx = GenerateTransaction();
                transactions.Add(tx);
            }

            return transactions;
        }

        private Transaction GenerateTransaction()
        {
            var newUserKeyPair = CryptoHelpers.GenerateKeyPair();
            var transaction = new Transaction
            {
                From = Address.FromPublicKey(newUserKeyPair.PublicKey),
                To = Address.Generate(),
                MethodName = $"Method-{Guid.NewGuid()}",
                Params = ByteString.CopyFrom(Hash.Generate().ToByteArray()),
                RefBlockNumber = 10
            };
            
            var signature = CryptoHelpers.SignWithPrivateKey(newUserKeyPair.PrivateKey,
                transaction.GetHash().DumpByteArray());
            transaction.Signature = ByteString.CopyFrom(signature);

            return transaction;
        }
    }
}