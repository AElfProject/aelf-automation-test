using System;
using System.Collections.Generic;
using AElf.Cryptography;
using AElf.Types;
using AElfChain.Common.DtoExtension;
using Google.Protobuf;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AElf.Automation.Contracts.ScenarioTest
{
    [TestClass]
    public class TransactionTests
    {
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
                From = AddressExtension.Generate(),
                To = AddressExtension.Generate(),
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