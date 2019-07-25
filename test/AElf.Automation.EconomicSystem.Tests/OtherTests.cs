using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Automation.Common.Helpers;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Kernel;
using AElf.Types;
using Google.Protobuf;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Shouldly;

namespace AElf.Automation.EconomicSystem.Tests
{
    [TestClass]
    public class OtherTests
    {
        private readonly ILogHelper _logger = LogHelper.GetLogger();

        [TestInitialize]
        public void InitTest()
        {
            _logger.InitLogHelper(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "test.log"));
        }

        [TestMethod]
        public void DictionaryTest()
        {
            var dic = new Dictionary<string, long>();

            dic.ContainsKey("test").ShouldBeFalse();

            dic.Add("test", 123);
            dic.ContainsKey("test").ShouldBeTrue();
        }

        [TestMethod]
        public void GetCurrentRoundInformation()
        {
            var stringMsg =
                "08c40312a4030a820130343530343766366562623032336334333766313233386630313232636561386632313834343735376132313164386535336336633433326563346333633563393930643733373533346334306632373336656536613034626631336336336131666136623465396237386535383463326164363534353162383337663439306234129c020802100122220a205ea9a20a983ffa37978bdbef5a0457d6aa7a42f1c69a2a43796f0a785bad8e492a220a2062f1aaaac52d77e574729f80a936a3528268f9e6ba7da3ff450c171749c77648320c08a7f9aee705109ccae6f101382e4a820130343530343766366562623032336334333766313233386630313232636561386632313834343735376132313164386535336336633433326563346333633563393930643733373533346334306632373336656536613034626631336336336131666136623465396237386535383463326164363534353162383337663439306234500262220a20d6ef93b5124f05fcf68039db742bf1c6f857b8cb6637f45644855d4889b1de3f680270027a0c08a9f9aee70510f0fab3f40190011a2088343a8201303435303437663665626230323363343337663132333866303132326365613866323138343437353761323131643865353363366334333265633463336335633939306437333735333463343066323733366565366130346266313363363361316661366234653962373865353834633261643635343531623833376634393062344018";
            var byteArray = ByteArrayHelper.FromHexString(stringMsg);
            var round = Round.Parser.ParseFrom(byteArray);
            _logger.Info(JsonConvert.SerializeObject(round));
        }

        [TestMethod]
        public async Task SelectTransactionTest1()
        {
            var tasks = new List<Task>
            {
                Task.Run(UpdateTransactionHub),
                Task.Run(GetTransactionFromHub)
            };

            Task.WaitAll(tasks.ToArray<Task>());
        }

        [TestMethod]
        public async Task SelectTransactionTest2()
        {
            TestCount = 200;
            var tasks = new List<Task>
            {
                Task.Run(UpdateTransactionHub),
                Task.Run(GetSelectedTransactionFromHub)
            };

            Task.WaitAll(tasks.ToArray());
        }

        private async Task GetTransactionFromHub()
        {
            var count = 0;
            while (count < 1000_000)
            {
                Thread.Sleep(50);
                var sw = new Stopwatch();
                sw.Start();
                var txs = TransactionHub.Values.Select(x => x.Transaction);
                sw.Stop();
                _logger.Info($"TestCost: {sw.ElapsedMilliseconds} milliseconds");
                count += txs.Count();
            }
        }

        private async Task GetSelectedTransactionFromHub()
        {
            var count = 0;
            while (count < 1000_000)
            {
                Thread.Sleep(50);
                var sw = new Stopwatch();
                sw.Start();
                var txs = TransactionHub.Values.Take(TestCount).Select(x => x.Transaction);
                sw.Stop();
                _logger.Info($"TestCost: {sw.ElapsedMilliseconds} milliseconds");
                count += txs.Count();
            }
        }

        private async Task UpdateTransactionHub()
        {
            TransactionHub = new ConcurrentDictionary<Hash, TransactionReceipt>();

            for (var i = 0; i < 1000_000; i++)
            {
                if (TransactionHub.Count > 10000) return;
                TransactionHub.TryAdd(Hash.FromString(Guid.NewGuid().ToString()), new TransactionReceipt
                {
                    TransactionId = Hash.FromString(Guid.NewGuid().ToString()),
                    Transaction = new Transaction
                    {
                        From = Address.Generate(),
                        To = Address.Generate(),
                        MethodName = $"Test-{Guid.NewGuid()}",
                        Params = ByteString.CopyFromUtf8("test"),
                        RefBlockNumber = 24,
                        RefBlockPrefix = ByteString.CopyFromUtf8("prefix"),
                        Signature = ByteString.CopyFromUtf8("sig")
                    }
                });
                if (i % 10 == 0)
                    Thread.Sleep(1);
            }
        }

        public ConcurrentDictionary<Hash, TransactionReceipt> TransactionHub { get; set; }
        public int TestCount { get; set; } = 1000;
    }
}