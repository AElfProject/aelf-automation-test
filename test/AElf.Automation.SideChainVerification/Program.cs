using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using AElf.Automation.Common.Helpers;

namespace AElf.Automation.SideChainVerification
{
    class Program
    {
        public static readonly ILogHelper Logger = LogHelper.GetLogHelper();

        static void Main(string[] args)
        {
            //Init Logger
            string logName = "SideChain" + DateTime.Now.ToString("yyyyMMdd_HHmm") + ".log";
            string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", logName);
            Logger.InitLogHelper(dir);

            string mainUrl = "http://192.168.197.70:8001";
            string side1Url = "http://192.168.197.70:8004";
            string side2Url = "http://192.168.197.70:8007";

            SideChain mSc = new SideChain(mainUrl, "MainNode");
            SideChain s1Sc = new SideChain(side1Url, "Side1Node");
            SideChain s2Sc = new SideChain(side2Url, "Side2Node");

            s1Sc.StartCheckVerifyResultTasks(3);
            s2Sc.StartCheckVerifyResultTasks(3);

            int currentHeight = 1;
            while (true)
            {
                try
                {
                    int mHeight = mSc.GetCurrentHeight();
                    if (mHeight == currentHeight)
                        break;
                    for (int i = currentHeight; i < mHeight; i++)
                    {
                        Logger.WriteInfo("Block check height: {0}", i);
                        var indexs = mSc.GetIndexBlockInfo(i);
                        if (indexs.Count == 0) continue;
                        string indexInfo = $"MainNode BlockHeight: {i}";
                        List<Task> tasks = new List<Task>();
                        foreach (var index in indexs)
                        {
                            indexInfo += $", ChainId: {index.ChainId}, IndexHeight: {index.Height}";
                            tasks.Add(Task.Run(() =>
                            {
                                s1Sc.PostVeriyTransaction(index);
                                s2Sc.PostVeriyTransaction(index);
                            }));
                        }

                        Logger.WriteInfo(indexInfo);
                        Task.WaitAll(tasks.ToArray());
                    }

                    //Continue another round of testing.
                    currentHeight = mHeight;
                }
                catch (Exception e)
                {
                    Logger.WriteInfo("StackTrace: {0}", e.StackTrace);
                    Logger.WriteInfo("Message: {0}", e.Message);
                    break;
                }
                finally
                {
                    s1Sc.StopCheckVerifyResultTasks();
                    Console.WriteLine();
                    s2Sc.StopCheckVerifyResultTasks();
                }
            }

            Logger.WriteInfo("Completed SideChain verification.");
        }
    }
}