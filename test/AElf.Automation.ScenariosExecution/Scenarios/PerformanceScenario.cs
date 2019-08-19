using System;
using System.Collections.Generic;
using System.Linq;
using AElf.Automation.Common.Contracts;
using AElf.Automation.Common.Helpers;
using AElf.Contracts.TestContract.Performance;
using AElf.Types;
using AElfChain.SDK.Models;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using log4net;
using Shouldly;

namespace AElf.Automation.ScenariosExecution.Scenarios
{
    public class PerformanceScenario : BaseScenario
    {
        public PerformanceContract Performance { get; set; }
        public List<string> Testers { get; }
        public new static readonly ILog Logger = Log4NetHelper.GetLogger();

        public PerformanceScenario()
        {
            InitializeScenario();

            Performance = Services.PerformanceService;
            Testers = AllTesters.GetRange(0, 50);
        }

        public void RunPerformanceScenarioJob()
        {
            ExecuteStandaloneTask(new Action[]
            {
                WritePerformanceAction,
                ComputePerformanceAction,
                QueryPerformanceAction
            });
        }

        private void WritePerformanceAction()
        {
            var rd = new Random();
            var list = ByteSizeArray.OrderBy(x => rd.Next()).Take(2);
            foreach (var item in list)
            {
                PerformanceMethod method = PerformanceMethod.Write1KContentByte;
                switch (item)
                {
                    case 1000:
                        method = PerformanceMethod.Write1KContentByte;
                        break;
                    case 2000:
                        method = PerformanceMethod.Write2KContentByte;
                        break;
                    case 5000:
                        method = PerformanceMethod.Write5KContentByte;
                        break;
                    case 10000:
                        method = PerformanceMethod.Write10KContentByte;
                        break;
                }

                var commandResult = Performance.ExecuteMethodWithResult(method, new WriteInput
                {
                    Content = GenerateRandomByteString(item)
                });
                var txResult = commandResult.InfoMsg as TransactionResultDto;
                txResult?.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            }
        }

        private void ComputePerformanceAction()
        {
            var rd = new Random();
            var list = ByteSizeArray.OrderBy(x => rd.Next()).Take(2);
            foreach (var item in list)
            {
                PerformanceMethod method = PerformanceMethod.ComputeLevel1;
                switch (item)
                {
                    case 1000:
                        method = PerformanceMethod.ComputeLevel1;
                        break;
                    case 2000:
                        method = PerformanceMethod.ComputeLevel2;
                        break;
                    case 5000:
                        method = PerformanceMethod.ComputeLevel3;
                        break;
                    case 10000:
                        method = PerformanceMethod.ComputeLevel4;
                        break;
                }

                var commandResult = Performance.ExecuteMethodWithResult(method, new Empty());
                var txResult = commandResult.InfoMsg as TransactionResultDto;
                txResult?.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            }
        }

        private void QueryPerformanceAction()
        {
            //query content
            var randomAddress = Testers[GenerateRandomNumber(0, Testers.Count - 1)];
            var result1 = Performance.CallViewMethod<ReadOutput>(PerformanceMethod.QueryReadInfo,
                AddressHelper.Base58StringToAddress(randomAddress));
            if (string.IsNullOrEmpty(result1.Content))
                result1.Content.WriteSuccessLine();

            //query fibonacci 
            var randomNumber = GenerateRandomNumber(0, 50);
            var result2 = Performance.CallViewMethod<NumberOutput>(PerformanceMethod.QueryFibonacci, new NumberInput
            {
                Number = randomNumber
            });
            Logger.Info($"Fibonacci query, number: {result2.Number}, result: {result2.Result}");
        }

        private static ByteString GenerateRandomByteString(long length)
        {
            var bytes = new byte[length];
            var rand = new Random(Guid.NewGuid().GetHashCode());
            rand.NextBytes(bytes);

            return ByteString.CopyFrom(bytes);
        }

        private static readonly List<int> ByteSizeArray = new List<int> {1000, 2000, 5000, 10000};
    }
}