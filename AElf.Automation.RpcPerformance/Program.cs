using System;
using AElf.Automation.Common.Extensions;

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
            Console.WriteLine("Whether run batch mode(yes/no):");
            string runType = Console.ReadLine();
            if (runType.Trim().ToLower() == "yes")
                performance.ExecuteContractsRpc();
            else if (runType.Trim().ToLower() == "avage")
                performance.ExecuteMultiTask();
            else
                performance.ExecuteContracts();

            //Result summary
            CategoryInfoSet set = new CategoryInfoSet(performance.CH.CommandList);
            set.GetCategoryBasicInfo();
            set.GetCategorySummaryInfo();
            set.SaveTestResultXml(performance.ThreadCount);

            Console.WriteLine("Complete performance testing.");
            Console.ReadLine();
        }
    }
}
