using System;
using System.Collections.Generic;
using System.Diagnostics;
using AElfChain.Common.Utils;
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
            foreach (var tx in transactionList) tx.VerifySignature();

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
            var newUserKeyPair = CryptoHelper.GenerateKeyPair();
            var transaction = new Transaction
            {
                From = AddressUtils.Generate(),
                To = AddressUtils.Generate(),
                MethodName = $"Method-{Guid.NewGuid()}",
                Params = ByteString.CopyFrom(Hash.FromString(Guid.NewGuid().ToString()).ToByteArray()),
                RefBlockNumber = 10
            };

            var signature = CryptoHelper.SignWithPrivateKey(newUserKeyPair.PrivateKey,
                transaction.GetHash().ToByteArray());
            transaction.Signature = ByteString.CopyFrom(signature);

            return transaction;
        }
    }
}