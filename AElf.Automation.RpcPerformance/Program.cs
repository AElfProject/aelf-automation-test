using System;
using System.IO;
using AElf.Automation.Common.Extensions;
using AElf.Automation.Common.Helpers;

namespace AElf.Automation.RpcPerformance
{
    public class Program
    {
        public static ILogHelper Logger = LogHelper.GetLogHelper();

        static void Main(string[] args)
        {
            RpcAPI performance;
            if (args.Length == 1)
            {
                performance = new RpcAPI(4, 50, args[0]);
            }
            else if (args.Length == 3)
            {
                int threadNo = Int32.Parse(args[0]);
                int execTimes = Int32.Parse(args[1]);
                performance = new RpcAPI(threadNo, execTimes, args[2]);
            }
            else if (args.Length == 4)
            {
                int threadNo = Int32.Parse(args[0]);
                int execTimes = Int32.Parse(args[1]);
                performance = new RpcAPI(threadNo, execTimes, args[2], args[3]);
            }
            else
                performance = new RpcAPI(8, 2000);

            //Init Logger
            string logName = "RpcTh_" + performance.ThreadCount + "_Tx_" + performance.ExeTimes +"_"+ DateTime.Now.ToString("MMddHHmmss") + ".log";
            string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", logName);
            Logger.InitLogHelper(dir);

            //Execute transaction command
            try
            {
                performance.InitExecRpcCommand();
                performance.DeployContract();
                performance.InitializeContract();
                performance.LoadAllContractAbi();

                bool autoTest = args?.Length == 1;
                ExecuteRpcTask(performance, autoTest);
            }
            catch (Exception e)
            {
                Logger.WriteError("Message: " + e.Message);
                Logger.WriteError("Source: " + e.Source);
            }
            finally
            {
                //Delete accounts
                performance.DeleteAccounts();
            }

            //Result summary
            CategoryInfoSet set = new CategoryInfoSet(performance.CH.CommandList);
            set.GetCategoryBasicInfo();
            set.GetCategorySummaryInfo();
            string xmlFile = set.SaveTestResultXml(performance.ThreadCount, performance.ExeTimes);
            Logger.WriteInfo("Rpc xml summary information saved into: {0}", xmlFile);
            Logger.WriteInfo("Complete performance testing.");
        }

        private static void ExecuteRpcTask(RpcAPI performance, bool autoTest = false)
        {
            Logger.WriteInfo("Select execution type:");
            Console.WriteLine("1. Normal mode");
            Console.WriteLine("2. Continus Tx mode");
            Console.WriteLine("3. Batch mode");
            Console.WriteLine("4. Continus Txs mode");
            Console.Write("Input selection: ");

            int result = 0;
            if (autoTest)
                result = 3;
            else
            {
                string runType = Console.ReadLine();
                bool check = Int32.TryParse(runType, out result);
                if (!check)
                {
                    Logger.WriteInfo("Wrong input, please input again.");
                    ExecuteRpcTask(performance);
                }
            }

            switch (result)
            {
                case 1:
                    performance.ExecuteContracts();
                    break;
                case 2:
                    performance.ExecuteMultiRpcTask();
                    break;
                case 3:
                    performance.ExecuteContractsRpc();
                    break;
                case 4:
                    performance.ExecuteMultiRpcTask(useTxs:true);
                    break;
                default:
                    Logger.WriteInfo("Wrong input, please input again.");
                    ExecuteRpcTask(performance);
                    break;
            }

            performance.PrintContractInfo();
        }
    }
}