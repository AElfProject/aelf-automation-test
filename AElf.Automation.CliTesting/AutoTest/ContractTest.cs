using System;
using System.IO;
using System.Threading;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AElf.Automation.CliTesting.AutoTest
{
    [TestClass]
    public class ContractTest : BaseTest
    {
        public static string ReadAccount()
        {
            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AccountInfo.json");
            string fileContent = File.ReadAllText(filePath);
            var accountInfo= JsonConvert.DeserializeObject<JObject>(fileContent);
            return accountInfo["account"].ToString();
        }

        [TestMethod]
        public void ConnectChain()
        {
            CommandRequest cmdReq = new CommandRequest("connect_chain");
            cmdReq.Result = Instance.ExecuteCommand(cmdReq.Command, out cmdReq.InfoMessage, out cmdReq.ErrorMessage);
            Assert.IsTrue(cmdReq.Result);

            cmdReq.GetJsonInfo();
            Assert.IsTrue(cmdReq.JsonInfo["genesis_contract"].ToString() != string.Empty);
            Assert.IsTrue(cmdReq.JsonInfo["chain_id"].ToString() != string.Empty);
        }

        [TestMethod]
        public void GetCommand()
        {
            CommandRequest cmdReq = new CommandRequest("get_commands");
            cmdReq.Result = Instance.ExecuteCommand(cmdReq.Command, out cmdReq.InfoMessage, out cmdReq.ErrorMessage);

            Assert.IsTrue(cmdReq.Result);
            Assert.IsTrue(cmdReq.InfoMessage.Contains("broadcast_tx"));
            Assert.IsTrue(cmdReq.InfoMessage.Contains("get_increment"));
            Assert.IsTrue(cmdReq.InfoMessage.Contains("get_contract_abi"));
        }


        [TestMethod]
        public void GetBlockHeight()
        {
            CommandRequest cmdReq = new CommandRequest("get_block_height");
            cmdReq.Result = Instance.ExecuteCommand(cmdReq.Command, out cmdReq.InfoMessage, out cmdReq.ErrorMessage);

            Assert.IsTrue(cmdReq.Result);
            cmdReq.GetJsonInfo();
            Assert.IsTrue(Convert.ToInt32(cmdReq.JsonInfo["block_height"].ToString()) > 0);
        }

        [TestMethod]
        public void LoadContractAbi()
        {
            //Connect First
            CommandRequest connectReq = new CommandRequest("connect_chain");
            connectReq.Result = Instance.ExecuteCommand(connectReq.Command, out connectReq.InfoMessage, out connectReq.ErrorMessage);
            Assert.IsTrue(connectReq.Result);

            CommandRequest cmdReq = new CommandRequest("load_contract_abi");
            cmdReq.Result = Instance.ExecuteCommand(cmdReq.Command, out cmdReq.InfoMessage, out cmdReq.ErrorMessage);
            Assert.IsTrue(cmdReq.Result);
            Assert.IsTrue(cmdReq.InfoMessage.Contains("AElf.Contracts.Genesis.ContractZeroWithDPoS"));
        }

        [TestMethod]
        public void LoadContractWithTxHash()
        {
            //Connect First
            CommandRequest connectReq = new CommandRequest("connect_chain");
            connectReq.Result = Instance.ExecuteCommand(connectReq.Command, out connectReq.InfoMessage, out connectReq.ErrorMessage);
            Assert.IsTrue(connectReq.Result);
            connectReq.GetJsonInfo();
            string genesisContract = connectReq.JsonInfo["genesis_contract"].ToString();

            CommandRequest cmdReq = new CommandRequest($"load_contract_abi {genesisContract}");
            cmdReq.Result = Instance.ExecuteCommand(cmdReq.Command, out cmdReq.InfoMessage, out cmdReq.ErrorMessage);
            Assert.IsTrue(cmdReq.Result);
            Assert.IsTrue(cmdReq.InfoMessage.Contains("AElf.Contracts.Genesis.ContractZeroWithDPoS"));
        }

        [TestMethod]
        public void GetIncrementWithInvalidAddress()
        {
            CommandRequest cmdReq = new CommandRequest("get_increment 044b8922878dde79054");
            cmdReq.Result = Instance.ExecuteCommand(cmdReq.Command, out cmdReq.InfoMessage, out cmdReq.ErrorMessage);
            Assert.IsFalse(cmdReq.Result);
            Assert.AreEqual("Invalid Address Format", cmdReq.ErrorMessage);
        }

        [TestMethod]
        public void GetIncrementWithCorrectAddress()
        {
            CommandRequest cmdReq = new CommandRequest($"get_increment {ACCOUNT}");
            cmdReq.Result = Instance.ExecuteCommand(cmdReq.Command, out cmdReq.InfoMessage, out cmdReq.ErrorMessage);
            Assert.IsTrue(cmdReq.Result);
            Assert.IsTrue(cmdReq.InfoMessage != string.Empty);
        }

        [TestMethod]
        public void DeployContractWithInvalidParameter()
        {
            //Connect First
            CommandRequest connectReq = new CommandRequest("connect_chain");
            connectReq.Result = Instance.ExecuteCommand(connectReq.Command, out connectReq.InfoMessage, out connectReq.ErrorMessage);
            Assert.IsTrue(connectReq.Result);

            CommandRequest cmdReq = new CommandRequest($"deploy_contract AElf.Contracts.InvalidName 0 {ACCOUNT}");
            cmdReq.Result = Instance.ExecuteCommand(cmdReq.Command, out cmdReq.InfoMessage, out cmdReq.ErrorMessage);
            Assert.IsFalse(cmdReq.Result);
            Assert.AreEqual("Invalid transaction data.", cmdReq.ErrorMessage);
        }

        [TestMethod]
        public void DeployContractWithoutAbiLoad()
        {
            //Connect First
            CommandRequest connectReq = new CommandRequest("connect_chain");
            connectReq.Result = Instance.ExecuteCommand(connectReq.Command, out connectReq.InfoMessage, out connectReq.ErrorMessage);
            Assert.IsTrue(connectReq.Result);

            CommandRequest cmdReq = new CommandRequest($"deploy_contract AElf.Contracts.Token 0 {ACCOUNT}");
            cmdReq.Result = Instance.ExecuteCommand(cmdReq.Command, out cmdReq.InfoMessage, out cmdReq.ErrorMessage);
            Assert.IsFalse(cmdReq.Result);

            Assert.AreEqual("ABI not loaded.", cmdReq.ErrorMessage);
        }

        [TestMethod]

        public void DeployContractWithCorrectParameter()
        {
            //Connect First
            CommandRequest connectReq = new CommandRequest("connect_chain");
            connectReq.Result = Instance.ExecuteCommand(connectReq.Command, out connectReq.InfoMessage, out connectReq.ErrorMessage);
            Assert.IsTrue(connectReq.Result);
            connectReq.GetJsonInfo();
            string genesisContract = connectReq.JsonInfo["genesis_contract"].ToString();

            //Load ABI 
            CommandRequest abiReq = new CommandRequest($"load_contract_abi {genesisContract}");
            abiReq.Result = Instance.ExecuteCommand(abiReq.Command, out abiReq.InfoMessage, out abiReq.ErrorMessage);
            Assert.IsTrue(abiReq.Result);

            //Unlock account
            CommandRequest accountReq = new CommandRequest($"account unlock {ACCOUNT} 123");
            accountReq.Result = Instance.ExecuteCommand(accountReq.Command, out accountReq.InfoMessage, out accountReq.ErrorMessage);

            Assert.IsTrue(accountReq.Result);

            //Deploy contract
            CommandRequest contractReq = new CommandRequest($"deploy_contract AElf.Contracts.Token 0 {ACCOUNT}");
            contractReq.Result = Instance.ExecuteCommand(contractReq.Command, out contractReq.InfoMessage, out contractReq.ErrorMessage);
            Assert.IsTrue(contractReq.Result);

            contractReq.GetJsonInfo();
            genesisContract = contractReq.JsonInfo["txId"].ToString();
            Assert.AreNotEqual(string.Empty, genesisContract);

            //Get Tx result
            string deployResult = string.Empty;
            for (int i = 0; i < 20; i++)
            {
                Thread.Sleep(10000);
                Console.WriteLine($"Check times: {(i + 1).ToString()} at {DateTime.Now.ToString()}");
                CommandRequest txReq = new CommandRequest($"get_tx_result {genesisContract}");
                txReq.Result = Instance.ExecuteCommand(txReq.Command, out txReq.InfoMessage, out txReq.ErrorMessage);
                Assert.IsTrue(txReq.Result);
                txReq.GetJsonInfo();
                deployResult = txReq.JsonInfo["tx_status"].ToString();
                if (deployResult == "Pending")
                    continue;
                break;
            }
            Assert.AreEqual("Mined", deployResult);
        }

        [TestMethod]
        public void DeployContractWithCorrectParameterAgain()
        {
            //Connect First
            CommandRequest connectReq = new CommandRequest("connect_chain");
            connectReq.Result = Instance.ExecuteCommand(connectReq.Command, out connectReq.InfoMessage, out connectReq.ErrorMessage);
            Assert.IsTrue(connectReq.Result);
            connectReq.GetJsonInfo();
            string genesisContract = connectReq.JsonInfo["genesis_contract"].ToString();

            //Load ABI 
            CommandRequest abiReq = new CommandRequest($"load_contract_abi {genesisContract}");
            abiReq.Result = Instance.ExecuteCommand(abiReq.Command, out abiReq.InfoMessage, out abiReq.ErrorMessage);
            Assert.IsTrue(abiReq.Result);

            //Unlock account
            CommandRequest accountReq = new CommandRequest($"account unlock {ACCOUNT} 123");
            accountReq.Result = Instance.ExecuteCommand(accountReq.Command, out accountReq.InfoMessage, out accountReq.ErrorMessage);

            Assert.IsTrue(accountReq.Result);

            //Deploy contract
            CommandRequest cmdReq = new CommandRequest($"deploy_contract AElf.Contracts.Token 0 {ACCOUNT}");
            cmdReq.Result = Instance.ExecuteCommand(cmdReq.Command, out cmdReq.InfoMessage, out cmdReq.ErrorMessage);
            Assert.IsTrue(cmdReq.Result);

            cmdReq.GetJsonInfo();

            Assert.AreEqual("AlreadyExecuted", cmdReq.JsonInfo["error"].ToString());
        }

        [TestMethod]
        public void ExecuteContractMethod()
        {
            //Connect First
            CommandRequest connectReq = new CommandRequest("connect_chain");
            connectReq.Result = Instance.ExecuteCommand(connectReq.Command, out connectReq.InfoMessage, out connectReq.ErrorMessage);
            Assert.IsTrue(connectReq.Result);

            //Unlock account
            CommandRequest accountReq = new CommandRequest($"account unlock {ACCOUNT} 123");
            accountReq.Result = Instance.ExecuteCommand(accountReq.Command, out accountReq.InfoMessage, out accountReq.ErrorMessage);

            Assert.IsTrue(accountReq.Result);

            //Get User increment
            accountReq = new CommandRequest($"get_increment {ACCOUNT}");
            accountReq.Result = Instance.ExecuteCommand(accountReq.Command, out accountReq.InfoMessage, out accountReq.ErrorMessage);
            Assert.IsTrue(accountReq.Result);
            string increNo = accountReq.InfoMessage;

            //Load Contract abi
            CommandRequest loadReq = new CommandRequest($"load_contract_abi");
            loadReq.Result = Instance.ExecuteCommand(loadReq.Command, out loadReq.InfoMessage, out loadReq.ErrorMessage);
            Assert.IsTrue(loadReq.Result);
            Assert.IsTrue(loadReq.InfoMessage.Contains("AElf.Contracts.Genesis.ContractZeroWithDPoS"));

            //Deploy contract
            CommandRequest contractReq = new CommandRequest($"deploy_contract AElf.Kernel.Tests.TestContract {increNo} {ACCOUNT}");
            contractReq.Result = Instance.ExecuteCommand(contractReq.Command, out contractReq.InfoMessage, out contractReq.ErrorMessage);
            Assert.IsTrue(contractReq.Result);

            contractReq.GetJsonInfo();
            string genesisContract = contractReq.JsonInfo["txId"].ToString();
            Assert.AreNotEqual(string.Empty, genesisContract);

            //Get Tx result
            CommandRequest txReq = new CommandRequest("");
            string deployResult = string.Empty;
            for (int i = 0; i < 20; i++)
            {
                Thread.Sleep(10000);
                Console.WriteLine($"Check times: {(i + 1).ToString()} at {DateTime.Now.ToString()}");
                txReq = new CommandRequest($"get_tx_result {genesisContract}");
                txReq.Result = Instance.ExecuteCommand(txReq.Command, out txReq.InfoMessage, out txReq.ErrorMessage);
                Assert.IsTrue(txReq.Result);
                txReq.GetJsonInfo();
                deployResult = txReq.JsonInfo["tx_status"].ToString();
                if (deployResult == "Pending")
                    continue;
                break;
            }
            Assert.AreEqual("Mined", deployResult);
            string abiAddress = txReq.JsonInfo["return"].ToString();

            //Load deployed contract abi
            CommandRequest abiReq = new CommandRequest($"load_contract_abi {abiAddress}");
            abiReq.Result = Instance.ExecuteCommand(abiReq.Command, out abiReq.InfoMessage, out abiReq.ErrorMessage);
            Assert.IsTrue(abiReq.Result);
            Assert.IsTrue(abiReq.InfoMessage.Contains("AElf.Benchmark.TestContract.TestContract"));

            //Get User increment again
            accountReq = new CommandRequest($"get_increment {ACCOUNT}");
            accountReq.Result = Instance.ExecuteCommand(accountReq.Command, out accountReq.InfoMessage, out accountReq.ErrorMessage);
            Assert.IsTrue(accountReq.Result);
            increNo = accountReq.InfoMessage;

            //Execute contract method
            string parameterinfo = "{\"from\":\"" + ACCOUNT +
                                   "\",\"to\":\"" + abiAddress +
                                   "\",\"method\":\"Initialize\",\"incr\":\"" +
                                   increNo + "\",\"params\":[" +
                                   "\"" + ACCOUNT + "\"," + "\"2000\"]}";
            CommandRequest exeReq = new CommandRequest($"broadcast_tx {parameterinfo}");
            exeReq.Result = Instance.ExecuteCommand(exeReq.Command, out exeReq.InfoMessage, out exeReq.ErrorMessage);
            Assert.IsTrue(exeReq.Result);
            exeReq.GetJsonInfo();
            string txResult = exeReq.JsonInfo["txId"].ToString();

            //Get Tx Result
            for (int i = 0; i < 20; i++)
            {
                Thread.Sleep(10000);
                Console.WriteLine($"Check times: {(i + 1).ToString()} at {DateTime.Now.ToString()}");
                txReq = new CommandRequest($"get_tx_result {txResult}");
                txReq.Result = Instance.ExecuteCommand(txReq.Command, out txReq.InfoMessage, out txReq.ErrorMessage);
                Assert.IsTrue(txReq.Result);
                txReq.GetJsonInfo();
                if (txReq.JsonInfo["tx_status"].ToString() == "Pending")
                    continue;
                break;
            }

            Assert.AreEqual("Mined", txReq.JsonInfo["tx_status"].ToString());
            Assert.AreEqual("0x01", txReq.JsonInfo["return"].ToString());
        }
    }
}