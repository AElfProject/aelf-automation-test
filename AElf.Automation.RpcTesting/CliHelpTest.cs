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
            _ci = new CommandInfo("ConnectChain");
            _ch.RpcConnectChain(_ci);
            Assert.IsTrue(_ci.Result);
        }

        [TestMethod]
        public void TestRpcLoadContractAbi()
        {
            _ci = new CommandInfo("LoadContractAbi");
            _ch.RpcLoadContractAbi(_ci);
            Assert.IsTrue(_ci.Result);
        }
        
        [DataTestMethod]
        [Ignore("This api is not implemented on new version code.")]
        [DataRow("1", "2000")]
        public void TestNodeWithoutCli(string minValue, string maxValue)
        {
            _ci = new CommandInfo("SetBlockVolume");
            _ci.Parameter = String.Format("{0} {1}", minValue, maxValue);
            _ch.ExecuteCommand(_ci);
            Assert.IsTrue(_ci.Result);
        }

        [DataTestMethod]
        [DataRow("123")]
        [DataRow("12345")]
        public void TestNewAccount(string password)
        {
            _ci = new CommandInfo("account new");
            _ci.Parameter = password;
            _ch.ExecuteCommand(_ci);
            Assert.IsTrue(_ci.Result);
        }

        [DataTestMethod]
        [DataRow("0x04f92c1ea999922e443a807fd548060cde48", "123")]
        public void TestUnlockAccount(string account, string password)
        {
            _ci = new CommandInfo("account unlock");
            _ci.Parameter = string.Format("{0} {1} {2}", account, password, "notimeout");
            _ch.ExecuteCommand(_ci);
            Assert.IsTrue(_ci.Result);
        }

        [TestMethod]
        public void TestListAccount()
        {
            _ci = new CommandInfo("account list");
            _ch.ExecuteCommand(_ci);
        }
    }
}