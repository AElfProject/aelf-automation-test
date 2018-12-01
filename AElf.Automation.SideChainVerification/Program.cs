using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using AElf.Automation.Common.Helpers;

namespace AElf.Automation.SideChainVerification
{
    class Program
    {
        public static ILogHelper Logger = LogHelper.GetLogHelper();

        static void Main(string[] args)
        {
            //Init Logger
            string logName = "SideChain" + DateTime.Now.ToString("yyyyMMdd_HHmm") + ".log";
            string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", logName);
            Logger.InitLogHelper(dir);

            string mainUrl = "http://192.168.197.43:8000";
            string side1Url = "http://192.168.197.36:8000";
            string side2Url = "http://192.168.197.41:8000";

            SideChain mSC = new SideChain(mainUrl, "MainNode");
            SideChain s1SC = new SideChain(side1Url, "Side1Node");
            SideChain s2SC = new SideChain(side2Url, "Side2Node");

            s1SC.StartCheckVerifyResultTasks(3);
            s2SC.StartCheckVerifyResultTasks(3);

            int currentHeight = 1;
            while (true)
            {
                try
                {
                    int mHeight = mSC.GetCurrentHeight();
                    if (mHeight == currentHeight)
                        break;
                    for (int i = currentHeight; i < mHeight; i++)
                    {
                        Logger.WriteInfo("Block check height: {0}", i);
                        var indexs = mSC.GetIndexBlockInfo(i);
                        if (indexs.Count == 0)
                            continue;
                        else
                        {
                            List<Task> tasks = new List<Task>();
                            foreach (var index in indexs)
                            {
                                tasks.Add(Task.Run(() =>
                                {
                                    s1SC.PostVeriyTransaction(index);
                                    s2SC.PostVeriyTransaction(index);
                                }));
                            }

                            Task.WaitAll(tasks.ToArray());
                        }
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
                    s1SC.StopCheckVerifyResultTasks();
                    Console.WriteLine();
                    s2SC.StopCheckVerifyResultTasks();
                }
            }
            Logger.WriteInfo("Completed SideChain verification.");
        }
    }
}