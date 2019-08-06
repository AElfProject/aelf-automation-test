using AElf.Automation.Common.Contracts;
using AElf.Automation.Common.OptionManagers;
using AElf.Automation.Common.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken.Messages;
using AElf.Cryptography;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Shouldly;

namespace AElf.Automation.Contracts.ScenarioTest
{
    [TestClass]
    public class TokenContractTest
    {
        private readonly ILogHelper _logger = LogHelper.GetLogHelper();
        public string TokenAbi { get; set; }
        public WebApiHelper CH { get; set; }
        public List<string> UserList { get; set; }

        public string InitAccount { get; } = "2876Vk2deM5ZnaXr1Ns9eySMSjpuvd53XatHTc37JXeW6HjiPs";
        public string TestAccount { get; } = "2cHBbC8CNriMQBJiNAm3bfiuoBEb8uS39avkLNdZa2hhBsARdr";
        private static string RpcUrl { get; } = "http://192.168.197.13:8100";

        [TestInitialize]
        public void Initialize()
        {
            #region Basic Preparation

            //Init Logger
            var logName = "ContractTest_" + DateTime.Now.ToString("MMddHHmmss") + ".log";
            var dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", logName);
            _logger.InitLogHelper(dir);

            #endregion
        }

        [TestMethod]
        public async Task NewStubTest_Call()
        {
            var tokenContractAddress = AddressHelper.Base58StringToAddress("WnV9Gv3gioSh3Vgaw8SSB96nV8fWUNxuVozCf6Y14e7RXyGaM");
            var keyPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "aelf");
            var tester = new ContractTesterFactory(RpcUrl, keyPath);
            var tokenStub = tester.Create<TokenContractContainer.TokenContractStub>(tokenContractAddress, InitAccount);
            var tokenInfo = await tokenStub.GetTokenInfo.CallAsync(new GetTokenInfoInput
            {
                Symbol = "ELF"
            });
            tokenInfo.ShouldNotBeNull();
        }

        [TestMethod]
        public async Task NewStubTest_Execution()
        {
            var tokenContractAddress = AddressHelper.Base58StringToAddress("WnV9Gv3gioSh3Vgaw8SSB96nV8fWUNxuVozCf6Y14e7RXyGaM");
            var keyPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "aelf");
            var tester = new ContractTesterFactory(RpcUrl, keyPath);
            var tokenStub = tester.Create<TokenContractContainer.TokenContractStub>(tokenContractAddress, InitAccount);
            var transactionResult = await tokenStub.Transfer.SendAsync(new TransferInput
            {
                Amount = 100,
                Symbol = "ELF",
                To = AddressHelper.Base58StringToAddress(TestAccount),
                Memo = "Test transfer with new sdk"
            });
            transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            //query balance
            var result = await tokenStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = AddressHelper.Base58StringToAddress(TestAccount),
                Symbol = "ELF"
            });
            result.Balance.ShouldBeGreaterThanOrEqualTo(100);
        }

        /*
        [TestMethod]
        public void QueryTokenFeeAddress()
        {
            var addressResult = tokenService.CallReadOnlyMethod(TokenMethod.FeePoolAddress);
            var result = DataHelper.TryGetValueFromJson(out var message, addressResult, "result", "return");
            Assert.IsTrue(result, "Return value is not exist.");
            Assert.IsFalse(message==string.Empty, "Token fee address is not set.");
            _logger.Info($"Token fee account is {message}");
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
            var balanceResult = tokenService.CallReadOnlyMethod(TokenMethod.GetBalance, InitAccount);
            _logger.Info($"IniitAccount balance: {tokenService.ConvertViewResult(balanceResult, true)}");

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

            //Init account balance
            var initResult = tokenService.CallReadOnlyMethod(TokenMethod.BalanceOf, InitAccount);
            _logger.Info($"IniitAccount balance: {tokenService.ConvertViewResult(initResult, true)}");

            //Fee account balance
            var feeResult = tokenService.CallReadOnlyMethod(TokenMethod.BalanceOf, FeeAccount);
            _logger.Info($"FeeAccount balance: {tokenService.ConvertViewResult(feeResult, true)}");

            //User account balance
            foreach (var acc in UserList)
            {
                var userResult = tokenService.CallReadOnlyMethod(TokenMethod.BalanceOf, acc);
                _logger.Info($"user balance: {tokenService.ConvertViewResult(userResult, true)}");
            }
        }

        [TestMethod]
        public void ExecuteTransactionWithoutFee()
        {
            //Fee account balance
            var fee1Result = tokenService.CallReadOnlyMethod(TokenMethod.GetBalance, FeeAccount);
            _logger.Info($"FeeAccount before balance: {tokenService.ConvertViewResult(fee1Result, true)}");

            tokenService.SetAccount(NoTokenAccount);
            tokenService.CallContractMethod(TokenMethod.GetBalance, FeeAccount);

            tokenService.SetAccount(InitAccount);
            tokenService.CallContractMethod(TokenMethod.GetBalance, InitAccount);
            tokenService.CallContractMethod(TokenMethod.GetBalance, FeeAccount);

            //Fee account balance
            var fee2Result = tokenService.CallReadOnlyMethod(TokenMethod.GetBalance, FeeAccount);
            _logger.Info($"FeeAccount after balance: {tokenService.ConvertViewResult(fee2Result, true)}");
        }

        [TestMethod]
        public void TransferFrom()
        {
            var tokenContract1 = new TokenContract(ApiHelper, UserList[0]);
            tokenContract1.CallContractMethod(TokenMethod.Initialize, "elfToken", "ELF", "200000", "2");
            tokenContract1.CallContractMethod(TokenMethod.Transfer, UserList[1], "2000");
            var abResult = tokenContract1.CallReadOnlyMethod(TokenMethod.GetBalance, UserList[0]);
            Console.WriteLine("A balance: {0}", tokenContract1.ConvertViewResult(abResult, true));

            var bbResult = tokenContract1.CallReadOnlyMethod(TokenMethod.GetBalance, UserList[1]);
            Console.WriteLine("B balance: {0}", tokenContract1.ConvertViewResult(bbResult, true));

            tokenContract1.CallContractMethod(TokenMethod.Approve, UserList[2], "10000");
            tokenContract1.Account = UserList[2];
            var allowResult = tokenContract1.CallReadOnlyMethod(TokenMethod.Allowance, UserList[0], UserList[1]);
            Console.WriteLine(allowResult.ToString());
            Console.WriteLine("B allowance from A: {0}", tokenContract1.ConvertViewResult(allowResult, true));

            tokenContract1.CallContractMethod(TokenMethod.TransferFrom, UserList[0], UserList[2], "5000");
            var bbResult1 = tokenContract1.CallReadOnlyMethod(TokenMethod.GetBalance, UserList[0]);
            Console.WriteLine("B balance: {0}", tokenContract1.ConvertViewResult(bbResult1, true));

            var bbResult2 = tokenContract1.CallReadOnlyMethod(TokenMethod.GetBalance, UserList[2]);
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
                ci = ApiHelper.NewAccount(ci);
                if (ci.Result)
                    UserList.Add(ci.InfoMsg?[0].Replace("Account address:", "").Trim());

                //unlock
                var uc = new CommandInfo("AccountUnlock", "account");
                uc.Parameter = String.Format("{0} {1} {2}", UserList[i], "123", "notimeout");
                uc = ApiHelper.UnlockAccount(uc);
            }
        }
        */
    }
}