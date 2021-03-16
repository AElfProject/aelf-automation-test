using System;
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

            if (mode)
            {
                check.GetOneBlockInfoTimes();
            }
            else
            {
                check.GetBlockInfo();
            }
        }
        public readonly ILog Logger = Log4NetHelper.GetLogger();

    }
}