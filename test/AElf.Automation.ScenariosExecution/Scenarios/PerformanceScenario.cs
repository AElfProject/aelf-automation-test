using System;
using System.Collections.Generic;
using System.Linq;
using AElfChain.Common.Contracts;
using AElfChain.Common.Helpers;
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
        public new static readonly ILog Logger = Log4NetHelper.GetLogger();

        private static readonly List<int> ByteSizeArray = new List<int> {1000, 2000, 5000, 10000};

        public PerformanceScenario()
        {
            InitializeScenario();

            Performance = PerformanceContract.GetOrDeployPerformanceContract(Services.NodeManager, Services.CallAddress);
            Testers = AllTesters.GetRange(15, 5);
            PrintTesters(nameof(PerformanceScenario), Testers);
        }

        public PerformanceContract Performance { get; set; }
        public List<string> Testers { get; }

        public void RunPerformanceScenarioJob()
        {
            ExecuteStandaloneTask(new Action[]
            {
                WritePerformanceAction,
                ComputePerformanceAction,
                QueryPerformanceAction,
                () => PrepareTesterToken(Testers),
                UpdateEndpointAction
            });
        }

        private void WritePerformanceAction()
        {
            var rd = new Random();
            var list = ByteSizeArray.OrderBy(x => rd.Next()).Take(2);
            foreach (var item in list)
            {
                var method = PerformanceMethod.Write1KContentByte;
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

                var txResult = Performance.ExecuteMethodWithResult(method, new WriteInput
                {
                    Content = ByteString.CopyFrom(CommonHelper.GenerateRandombytes(item))
                });
                txResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            }
        }

        private void ComputePerformanceAction()
        {
            var rd = new Random();
            var list = ByteSizeArray.OrderBy(x => rd.Next()).Take(2);
            foreach (var item in list)
            {
                var method = PerformanceMethod.ComputeLevel1;
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

                var txResult = Performance.ExecuteMethodWithResult(method, new Empty());
                txResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
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
            var randomNumber = GenerateRandomNumber(0, 40);
            var result2 = Performance.CallViewMethod<NumberOutput>(PerformanceMethod.QueryFibonacci, new NumberInput
            {
                Number = randomNumber
            });
            Logger.Info($"Fibonacci query, number: {result2.Number}, result: {result2.Result}");
        }
    }
}