using System;
using System.Collections.Generic;
using System.Threading;
using AElf.Automation.Common.Extensions;
using AElf.Automation.Common.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AElf.Automation.RpcTesting
{
    [TestClass]
    public class CliHelpTest
    {
        public string RpcUrl = "http://192.168.199.221:8000";
        public CliHelper CI;
        public CommandInfo ci;
        
        [TestInitialize]
        public void Initialize()
        {
            CI = new CliHelper(RpcUrl);
        }

        [TestCleanup]
        public void Cleanup()
        {
            CI = null;
        }

        [TestMethod]
        public void TestRpcConnect()
        {
            ci = new CommandInfo("connect_chain");
            CI.RpcConnectChain(ci);
            Assert.IsTrue(ci.Result);
        }

        [TestMethod]
        public void TestRpcLoadContractAbi()
        {
            ci = new CommandInfo("load_contract_abi");
            CI.RpcLoadContractAbi(ci);
            Assert.IsTrue(ci.Result);
        }
        
        [DataTestMethod]
        [DataRow("1", "2000")]
        public void TestNodeWithoutCli(string minValue, string maxValue)
        {
            ci = new CommandInfo("set_block_volume");
            ci.Parameter = String.Format("{0} {1}", minValue, maxValue);
            CI.ExecuteCommand(ci);
            Assert.IsTrue(ci.Result);
        }

        [DataTestMethod]
        [DataRow("123")]
        [DataRow("")]
        public void TestNewAccount(string password)
        {
            ci = new CommandInfo("account new");
            ci.Parameter = password;
            CI.ExecuteCommand(ci);
            Assert.IsTrue(ci.Result);
        }

        [DataTestMethod]
        [DataRow("0x04f92c1ea999922e443a807fd548060cde48", "123")]
        public void TestUnlockAccount(string account, string password)
        {
            ci = new CommandInfo("account unlock");
            ci.Parameter = string.Format("{0} {1} {2}", account, password, "notimeout");
            CI.ExecuteCommand(ci);
            Assert.IsTrue(ci.Result);
        }

        [TestMethod]
        public void TestListAccount()
        {
            ci = new CommandInfo("account list");
            CI.ExecuteCommand(ci);
        }
    }
}