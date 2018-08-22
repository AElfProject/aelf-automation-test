using System;
using AElf.Automation.Common.Extensions;
using ServiceStack.Text;

namespace AElf.Automation.RpcPerformance
{
    public class Program
    {
        static void Main(string[] args)
        {
            RpcAPI performance;
            if(args.Length == 1)
            {
                performance = new RpcAPI(8, 2000, args[0]);
            }
            else if (args.Length==3)
            {
                int threadNo = Int32.Parse(args[0]);
                int execTimes = Int32.Parse(args[1]);
                performance = new RpcAPI(threadNo, execTimes, args[2]);
            }
            else if(args.Length ==4)
            {
                int threadNo = Int32.Parse(args[0]);
                int execTimes = Int32.Parse(args[1]);
                performance = new RpcAPI(threadNo, execTimes, args[2], args[3]);
            }
            else
                performance = new RpcAPI(8, 2000);

            //Execute command
            performance.PrepareEnv();
            performance.InitExecRpcCommand();
            performance.DeployContract();
            performance.InitializeContract();
            performance.LoadAllContractAbi();
            ExecuteRpcTask(performance);
            
            //Result summary
            CategoryInfoSet set = new CategoryInfoSet(performance.CH.CommandList);
            set.GetCategoryBasicInfo();
            set.GetCategorySummaryInfo();
            set.SaveTestResultXml(performance.ThreadCount);

            Console.WriteLine("Complete performance testing.");
            Console.ReadLine();
        }

        private static void ExecuteRpcTask(RpcAPI performance)
        {
            Console.WriteLine("Select execution type:");
            Console.WriteLine("1. Normal mode");
            Console.WriteLine("2. Avage mode");
            Console.WriteLine("3. Batch mode");
            Console.Write("Input selection: ");
            string runType = Console.ReadLine();
            int result = 0;
            bool check = Int32.TryParse(runType, out result);
            if (!check)
            {
                Console.WriteLine("Wrong input, please input again.");
                ExecuteRpcTask(performance);
            }

            switch (result)
            {
                    case 1:
                        performance.ExecuteContracts();
                        break;
                    case 2:
                        performance.ExecuteMultiTask();
                        break;
                    case 3:
                        performance.ExecuteContractsRpc();
                        break;
                    default:
                        Console.WriteLine("Wrong input, please input again.");
                        ExecuteRpcTask(performance);
                        break;
            }
            performance.PrintContractInfo();
        }
    }
}
