using AElf.Automation.Common.Contracts;
using AElf.Automation.Common.Extensions;
using AElf.Automation.Common.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;

namespace AElf.Automation.Contracts.ScenarioTest
{
    [TestClass]
    public class TokenContractTest
    {
        public static ILogHelper Logger = LogHelper.GetLogHelper();
        public static string TokenAbi { get; set; }
        public static CliHelper CH { get; set; }
        public static List<string> AccList { get; set; }

        [TestInitialize]
        public void Initialize()
        {
            #region Basic Preparation
            //Init Logger
            string logName = "ContractTest_" + DateTime.Now.ToString("MMddHHmmss") + ".log";
            string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", logName);
            Logger.InitLogHelper(dir);

            string url = "http://192.168.197.53:8001/chain";
            CH = new CliHelper(url, AccountManager.GetDefaultDataDir());

            //Connect Chain
            var ci = new CommandInfo("connect_chain");
            CH.RpcConnectChain(ci);
            Assert.IsTrue(ci.Result, "Connect chain got exception.");

            //Get AElf.Contracts.Token ABI
            ci.GetJsonInfo();
            TokenAbi = ci.JsonInfo["AElf.Contracts.Token"].ToObject<string>();

            //Load default Contract Abi
            ci = new CommandInfo("load_contract_abi");
            CH.RpcLoadContractAbi(ci);
            Assert.IsTrue(ci.Result, "Load contract abi got exception.");

            //Account preparation
            AccList = new List<string>();
            ci = new CommandInfo("account new", "account");
            for (int i = 0; i < 5; i++)
            {
                ci.Parameter = "123";
                ci = CH.ExecuteCommand(ci);
                if (ci.Result)
                    AccList.Add(ci.InfoMsg?[0].Replace("Account address:", "").Trim());

                //unlock
                var uc = new CommandInfo("account unlock", "account");
                uc.Parameter = String.Format("{0} {1} {2}", AccList[i], "123", "notimeout");
                uc = CH.ExecuteCommand(uc);
            }
            #endregion
        }

        [TestMethod]
        public void TransferFrom()
        {
            var tokenContract1 = new TokenContract(CH, AccList[0]);
            tokenContract1.CallContractMethod(TokenMethod.Initialize, "elfToken", "ELF", "200000", "2");
            tokenContract1.CallContractMethod(TokenMethod.Transfer, AccList[1], "2000");
            var abResult = tokenContract1.CallReadOnlyMethod(TokenMethod.BalanceOf, AccList[0]);
            Console.WriteLine("A balance: {0}", tokenContract1.ConvertViewResult(abResult, true));

            var bbResult = tokenContract1.CallReadOnlyMethod(TokenMethod.BalanceOf, AccList[1]);
            Console.WriteLine("B balance: {0}", tokenContract1.ConvertViewResult(bbResult, true));

            tokenContract1.CallContractMethod(TokenMethod.Approve, AccList[2], "10000");
            tokenContract1.Account = AccList[2];
            var allowResult = tokenContract1.CallReadOnlyMethod(TokenMethod.Allowance, AccList[0], AccList[1]);
            Console.WriteLine(allowResult.ToString());
            Console.WriteLine("B allowance from A: {0}", tokenContract1.ConvertViewResult(allowResult, true));

            tokenContract1.CallContractMethod(TokenMethod.TransferFrom, AccList[0], AccList[2], "5000");
            var bbResult1 = tokenContract1.CallReadOnlyMethod(TokenMethod.BalanceOf, AccList[0]);
            Console.WriteLine("B balance: {0}", tokenContract1.ConvertViewResult(bbResult1, true));

            var bbResult2 = tokenContract1.CallReadOnlyMethod(TokenMethod.BalanceOf, AccList[2]);
            Console.WriteLine("B balance: {0}", tokenContract1.ConvertViewResult(bbResult2, true));

            var allowResult1 = tokenContract1.CallReadOnlyMethod(TokenMethod.Allowance, AccList[0], AccList[1]);
            Console.WriteLine("B allowance from A: {0}", tokenContract1.ConvertViewResult(allowResult1, true));
        }
    }
}
