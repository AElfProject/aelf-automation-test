using System;
using System.Threading;
using System.Threading.Tasks;
using AElfChain.Common.Helpers;
using log4net;

namespace AElf.Automation.BlockCheck
{
    class Program
    {
        public static void Main()
        {
            //Init Logger
            Log4NetHelper.LogInit("CheckBlock");
            var check = new BlockCheck();
            var mode = ConfigInfo.ReadInformation.VerifyOne;
            var times = ConfigInfo.ReadInformation.VerifyTimes;
            var verifyCount = ConfigInfo.ReadInformation.VerifyBlockCount;
            if (mode)
            {
                for (var i = 0; i < 10; i++)
                { 
                    var duration = check.GetOneBlockInfoTimes();
                    Thread.Sleep(1000);
                }
                
                long all = 0;
                var vt = times;
                while (vt > 0)
                {
                    Logger.Info($"{vt}");
                    var duration = check.GetOneBlockInfoTimes();
                    all += duration;
                    vt--;
                    Thread.Sleep(1000);
                }
                var req = (double) verifyCount * times / all * 1000;
                var req1 = (double) all / verifyCount * times;
                Logger.Info($"all time: {all}ms, {req}/s, {req1}ms");
            }
            else
            {
                for (var i = 0; i < 10; i++)
                { 
                    var duration = check.GetOneBlockInfoTimes();
                    Thread.Sleep(1000);
                }
                
                long all = 0;
                var vt = times;
                while (vt > 0)
                {
                    Logger.Info($"{vt}");
                    var duration =  check.GetBlockInfo();
                    all += duration;
                    vt--;
                    Thread.Sleep(1000);
                }

                var req = (double) verifyCount * times / all * 1000;
                var req1 = (double) all / (verifyCount * times);
                Logger.Info($"all time: {all}ms, {req}/s, {req1}ms");
            }
        }
        private static readonly ILog Logger = Log4NetHelper.GetLogger();

    }
}