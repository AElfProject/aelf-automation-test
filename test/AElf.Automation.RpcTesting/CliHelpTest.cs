using System;
using AElf.Automation.Common.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AElf.Automation.RpcTesting
{
    [TestClass]
    public class CliHelpTest
    {
        private const string RpcUrl = "http://192.168.199.221:8000";
        private CliHelper _ch;
        private CommandInfo _ci;
        
        [TestInitialize]
        public void Initialize()
        {
            _ch = new CliHelper(RpcUrl);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _ch = null;
        }

        [TestMethod]
        public void TestRpcConnect()
        {
            _ci = new CommandInfo(ApiMethods.GetChainInformation);
            _ch.RpcGetChainInformation(_ci);
            Assert.IsTrue(_ci.Result);
        }
        
        [DataTestMethod]
        [DataRow("123")]
        [DataRow("12345")]
        public void TestNewAccount(string password)
        {
            _ci = new CommandInfo(ApiMethods.AccountNew);
            _ci.Parameter = password;
            _ch.ExecuteCommand(_ci);
            Assert.IsTrue(_ci.Result);
        }

        [DataTestMethod]
        [DataRow("0x04f92c1ea999922e443a807fd548060cde48", "123")]
        public void TestUnlockAccount(string account, string password)
        {
            _ci = new CommandInfo(ApiMethods.AccountUnlock);
            _ci.Parameter = string.Format("{0} {1} {2}", account, password, "notimeout");
            _ch.ExecuteCommand(_ci);
            Assert.IsTrue(_ci.Result);
        }

        [TestMethod]
        public void TestListAccount()
        {
            _ci = new CommandInfo(ApiMethods.AccountList);
            _ch.ExecuteCommand(_ci);
        }
    }
}