using AElf.Automation.Common.Contracts;
using AElf.Automation.Common.Extensions;
using AElf.Automation.Common.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Automation.Contracts.ScenarioTest
{
    [TestClass]
    public class TokenContractTest
    {
        private readonly ILogHelper _logger = LogHelper.GetLogHelper();
        public string TokenAbi { get; set; }
        public CliHelper CH { get; set; }
        public List<string> UserList { get; set; }

        public string InitAccount { get; } = "ELF_2GkD1q74HwBrFsHufmnCKHJvaGVBYkmYcdG3uebEsAWSspX";
        public string FeeAccount { get; } = "ELF_1dVay78LmRRzP7ymunFsBJFT8frYK4hLNjUCBi4VWa2KmZ";
        public string NoTokenAccount { get; } = "ELF_1sGf6rf4r8VvmgzH1x2YuVKTJBPGXnuau3xg9X5wU2XXCk";

        private TokenContract tokenService { get; set; }

        private static string RpcUrl { get; } = "http://192.168.197.34:8000/chain";

        [TestInitialize]
        public void Initialize()
        {
            #region Basic Preparation
            //Init Logger
            string logName = "ContractTest_" + DateTime.Now.ToString("MMddHHmmss") + ".log";
            string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", logName);
            _logger.InitLogHelper(dir);

            CH = new CliHelper(RpcUrl, AccountManager.GetDefaultDataDir());

            //Connect Chain
            var ci = new CommandInfo("ConnectChain");
            CH.RpcConnectChain(ci);
            Assert.IsTrue(ci.Result, "Connect chain got exception.");

            //Get AElf.Contracts.Token ABI
            ci.GetJsonInfo();
            TokenAbi = ci.JsonInfo["AElf.Contracts.Token"].ToObject<string>();

            //Load default Contract Abi
            ci = new CommandInfo("LoadContractAbi");
            CH.RpcLoadContractAbi(ci);
            Assert.IsTrue(ci.Result, "Load contract abi got exception.");

            //Init contract service
            tokenService = new TokenContract(CH, InitAccount, TokenAbi);

            #endregion
        }

        [TestMethod]
        public void QueryTokenFeeAddress()
        {
            var addressResult = tokenService.CallReadOnlyMethod(TokenMethod.FeePoolAddress);
            var result = DataHelper.TryGetValueFromJson(out var message, addressResult, "result", "return");
            Assert.IsTrue(result, "Return value is not exist.");
            Assert.IsFalse(message==string.Empty, "Token fee address is not set.");
            _logger.WriteInfo($"Token fee account is {message}");
        }

        [TestMethod]
        public void SetTokenFeeAddress()
        {
            var addressResult = tokenService.CallReadOnlyMethod(TokenMethod.FeePoolAddress);
            var result = DataHelper.TryGetValueFromJson(out var message, addressResult, "result", "return");
            Assert.IsTrue(result, "Return value is not exist.");
            if (message == string.Empty)
            {
                tokenService.CallContractMethod(TokenMethod.SetFeePoolAddress, FeeAccount);
            }

            QueryTokenFeeAddress();
        }


        [TestMethod]
        public void InitToken()
        {
            tokenService.CallContractMethod(TokenMethod.Initialize, "elfToken", "ELF", "500000", "2");
            var balanceResult = tokenService.CallReadOnlyMethod(TokenMethod.BalanceOf, InitAccount);
            _logger.WriteInfo($"IniitAccount balance: {tokenService.ConvertViewResult(balanceResult, true)}");

            tokenService.CallContractMethod(TokenMethod.SetFeePoolAddress, FeeAccount);
        }

        [TestMethod]
        public void TransferTest()
        {
            PrepareAccount(50);

            Random rd = new Random();
            for (int i = 0; i < 50000; i++)
            {
                var numbr = rd.Next(0, 49);
                tokenService.CallContractWithoutResult(TokenMethod.Transfer, UserList[numbr], "100");
            }

    /*
            //Init account balance
            var initResult = tokenService.CallReadOnlyMethod(TokenMethod.BalanceOf, InitAccount);
            _logger.WriteInfo($"IniitAccount balance: {tokenService.ConvertViewResult(initResult, true)}");

            //Fee account balance
            var feeResult = tokenService.CallReadOnlyMethod(TokenMethod.BalanceOf, FeeAccount);
            _logger.WriteInfo($"FeeAccount balance: {tokenService.ConvertViewResult(feeResult, true)}");

            //User account balance
            foreach (var acc in UserList)
            {
                var userResult = tokenService.CallReadOnlyMethod(TokenMethod.BalanceOf, acc);
                _logger.WriteInfo($"user balance: {tokenService.ConvertViewResult(userResult, true)}");
            }
    */
        }

        [TestMethod]
        public void ExecuteTransactionWithoutFee()
        {
            //Fee account balance
            var fee1Result = tokenService.CallReadOnlyMethod(TokenMethod.BalanceOf, FeeAccount);
            _logger.WriteInfo($"FeeAccount before balance: {tokenService.ConvertViewResult(fee1Result, true)}");

            tokenService.SetAccount(NoTokenAccount);
            tokenService.CallContractMethod(TokenMethod.BalanceOf, FeeAccount);

            tokenService.SetAccount(InitAccount);
            tokenService.CallContractMethod(TokenMethod.BalanceOf, InitAccount);
            tokenService.CallContractMethod(TokenMethod.BalanceOf, FeeAccount);

            //Fee account balance
            var fee2Result = tokenService.CallReadOnlyMethod(TokenMethod.BalanceOf, FeeAccount);
            _logger.WriteInfo($"FeeAccount after balance: {tokenService.ConvertViewResult(fee2Result, true)}");
        }

        [TestMethod]
        public void TransferFrom()
        {
            var tokenContract1 = new TokenContract(CH, UserList[0]);
            tokenContract1.CallContractMethod(TokenMethod.Initialize, "elfToken", "ELF", "200000", "2");
            tokenContract1.CallContractMethod(TokenMethod.Transfer, UserList[1], "2000");
            var abResult = tokenContract1.CallReadOnlyMethod(TokenMethod.BalanceOf, UserList[0]);
            Console.WriteLine("A balance: {0}", tokenContract1.ConvertViewResult(abResult, true));

            var bbResult = tokenContract1.CallReadOnlyMethod(TokenMethod.BalanceOf, UserList[1]);
            Console.WriteLine("B balance: {0}", tokenContract1.ConvertViewResult(bbResult, true));

            tokenContract1.CallContractMethod(TokenMethod.Approve, UserList[2], "10000");
            tokenContract1.Account = UserList[2];
            var allowResult = tokenContract1.CallReadOnlyMethod(TokenMethod.Allowance, UserList[0], UserList[1]);
            Console.WriteLine(allowResult.ToString());
            Console.WriteLine("B allowance from A: {0}", tokenContract1.ConvertViewResult(allowResult, true));

            tokenContract1.CallContractMethod(TokenMethod.TransferFrom, UserList[0], UserList[2], "5000");
            var bbResult1 = tokenContract1.CallReadOnlyMethod(TokenMethod.BalanceOf, UserList[0]);
            Console.WriteLine("B balance: {0}", tokenContract1.ConvertViewResult(bbResult1, true));

            var bbResult2 = tokenContract1.CallReadOnlyMethod(TokenMethod.BalanceOf, UserList[2]);
            Console.WriteLine("B balance: {0}", tokenContract1.ConvertViewResult(bbResult2, true));

            var allowResult1 = tokenContract1.CallReadOnlyMethod(TokenMethod.Allowance, UserList[0], UserList[1]);
            Console.WriteLine("B allowance from A: {0}", tokenContract1.ConvertViewResult(allowResult1, true));
        }

        private void PrepareAccount(int userCoouont)
        {
            //Account preparation
            UserList = new List<string>();
            var ci = new CommandInfo("AccountNew", "account");
            for (int i = 0; i < userCoouont; i++)
            {
                ci.Parameter = "123";
                ci = CH.NewAccount(ci);
                if (ci.Result)
                    UserList.Add(ci.InfoMsg?[0].Replace("Account address:", "").Trim());

                //unlock
                var uc = new CommandInfo("AccountUnlock", "account");
                uc.Parameter = String.Format("{0} {1} {2}", UserList[i], "123", "notimeout");
                uc = CH.UnlockAccount(uc);
            }
        }
    }
}
