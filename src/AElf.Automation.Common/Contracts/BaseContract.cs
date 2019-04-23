using System;
using System.Collections.Concurrent;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using AElf.Automation.Common.Helpers;
using Google.Protobuf;

namespace AElf.Automation.Common.Contracts
{
    public class BaseContract
    {
        #region Priority

        private CliHelper Ch { get; set; }
        private string FileName { get; set; }
        public string Address { get; set; }
        
        public Address Account {get; set;}
        public string ContractAbi { get; set; }

        private ConcurrentQueue<string> TxResultList { get; set; }
        private readonly ILogHelper _logger = LogHelper.GetLogHelper();

        #endregion

        public BaseContract(CliHelper ch, string fileName, string address)
        {
            Ch = ch;
            FileName = fileName;
            Address = address;
            TxResultList = new ConcurrentQueue<string>();

            UnlockAccount(address);
            DeployContract();
        }

        public BaseContract(CliHelper ch, string contractAbi)
        {
            Ch = ch;
            ContractAbi = contractAbi;
            TxResultList = new ConcurrentQueue<string>();
        }

        public string ExecuteContractMethod(string method, IMessage inputParameter)
        {
            string rawTx = GenerateBroadcastRawTx(method, inputParameter);

            var txId = ExecuteContractMethod(rawTx);
            _logger.WriteInfo($"Transaction method: {method}, TxId: {txId}");
            TxResultList.Enqueue(txId);

            return txId;
        }

        public CommandInfo ExecuteContractMethodWithResult(string method, IMessage inputParameter)
        {
            string rawTx = GenerateBroadcastRawTx(method, inputParameter);

            var txId = ExecuteContractMethod(rawTx);
            _logger.WriteInfo($"Transaction method: {method}, TxId: {txId}");

            //Chek result
            return CheckTransactionResult(txId, 30);
        }

        public bool GetTransactionResult(string txId, out CommandInfo ci)
        {
            ci = new CommandInfo("GetTransactionResult");
            ci.Parameter = txId;
            Ch.ExecuteCommand(ci);

            if (ci.Result)
            {
                ci.GetJsonInfo();
                ci.JsonInfo = ci.JsonInfo;
                string txResult = ci.JsonInfo["result"]["Status"].ToString();
                _logger.WriteInfo($"Transaction: {txId}, Status: {txResult}");

                return txResult == "Mined";
            }

            _logger.WriteError(ci.GetErrorMessage());
            return false;
        }

        public CommandInfo CheckTransactionResult(string txId, int maxTimes = 60)
        {
            CommandInfo ci = null;
            int checkTimes = 1;
            while (checkTimes <= maxTimes)
            {
                ci = new CommandInfo("GetTransactionResult");
                ci.Parameter = txId;
                Ch.RpcGetTxResult(ci);
                if (ci.Result)
                {
                    ci.GetJsonInfo();
                    ci.JsonInfo = ci.JsonInfo;
                    string txResult = ci.JsonInfo["result"]["Status"].ToString();
                    if (txResult == "Mined")
                    {
                        _logger.WriteInfo($"Transaction status: {txResult}");
                        return ci;
                    }

                    if (txResult == "Failed")
                    {
                        _logger.WriteInfo($"Transaction status: {txResult}");
                        _logger.WriteError(ci.JsonInfo.ToString());
                        return ci;
                    }
                }

                checkTimes++;
                Thread.Sleep(1000);
            }

            _logger.WriteError(ci?.JsonInfo.ToString());
            Assert.IsTrue(false, "Transaction execute status cannot be 'Mined' after one minutes.");

            return ci;
        }

        /// <summary>
        /// 切换账号
        /// </summary>
        /// <param name="account"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public bool SetAccount(string account, string password = "123")
        {
            Address = account;

            //Unlock
            var uc = new CommandInfo("AccountUnlock", "account");
            uc.Parameter = String.Format("{0} {1} {2}", account, password, "notimeout");
            uc = Ch.UnlockAccount(uc);

            return uc.Result;
        }

        /// <summary>
        /// 检查所有执行合约结果
        /// </summary>
        public void CheckTransactionResultList()
        {
            int queueLength = 0;
            int queueSameTimes = 0;

            while (true)
            {
                bool result = TxResultList.TryDequeue(out var txId);
                if (!result)
                    break;
                var ci = new CommandInfo("GetTransactionResult");
                ci.Parameter = txId;
                Ch.RpcGetTxResult(ci);
                if (ci.Result)
                {
                    ci.GetJsonInfo();
                    ci.JsonInfo = ci.JsonInfo;
                    string txResult = ci.JsonInfo["result"]["Status"].ToString();

                    if (txResult == "Mined")
                        continue;
                    if (txResult == "Failed" || txResult == "NotExisted")
                    {
                        _logger.WriteInfo($"Transaction status: {txResult}");
                        _logger.WriteError(ci.JsonInfo.ToString());
                        continue;
                    }

                    TxResultList.Enqueue(txId);
                }

                if (queueLength == TxResultList.Count)
                {
                    queueSameTimes++;
                    Thread.Sleep(3000);
                }
                else
                    queueSameTimes = 0;

                queueLength = TxResultList.Count;
                if (queueSameTimes == 10)
                    Assert.IsTrue(false, "Transaction result check failed due to pending results.");
            }
        }

        /// <summary>
        /// 调用合约View方法
        /// </summary>
        /// <param name="method"></param>
        /// <param name="input"></param>
        /// <returns></returns>
        public JObject CallContractViewMethod(string method, IMessage input)
        {
            var resp = Ch.RpcQueryResult(Address, ContractAbi, method, input);
            if (resp == string.Empty)
                return new JObject();

            return JObject.Parse(resp);
        }

        /// <summary>
        /// 转化Hex View结果信息
        /// </summary>
        /// <param name="info"></param>
        /// <param name="hexValue">是否是数值类型</param>
        /// <returns></returns>
        public string ConvertViewResult(JObject info, bool hexValue = false)
        {
            if (info["result"] == null)
                return string.Empty;

            return DataHelper.ConvertHexInfo(info["result"].ToString(), hexValue);
        }

        public void UnlockAccount(string account, string password = "123")
        {
            var uc = new CommandInfo("AccountUnlock", "account");
            uc.Parameter = String.Format("{0} {1} {2}", account, password, "notimeout");
            Ch.UnlockAccount(uc);
        }

        #region Private Methods

        private void DeployContract()
        {
            var ci = new CommandInfo("DeploySmartContract");
            ci.Parameter = $"{FileName} 0 {Address}";
            Ch.RpcDeployContract(ci);
            if (ci.Result)
            {
                ci.GetJsonInfo();
                var txId = ci.JsonInfo["TransactionId"].ToString();
                _logger.WriteInfo($"Transaction: DeploySmartContract, TxId: {txId}");

                bool result = GetContractAbi(txId, out _);
                Assert.IsTrue(result, $"Get contract abi failed.");
            }

            Assert.IsTrue(ci.Result, $"Deploy contract failed. Reason: {ci.GetErrorMessage()}");
        }

        private string GenerateBroadcastRawTx(string method, IMessage inputParameter)
        {
            return Ch.RpcGenerateTransactionRawTx(Address, ContractAbi, method, inputParameter);
        }

        private bool GetContractAbi(string txId, out string contractAbi)
        {
            contractAbi = string.Empty;
            var ci = CheckTransactionResult(txId);

            if (ci.Result)
            {
                ci.GetJsonInfo();
                ci.JsonInfo = ci.JsonInfo;
                string deployResult = ci.JsonInfo["result"]["Status"].ToString();
                _logger.WriteInfo($"Transaction: {txId}, Status: {deployResult}");
                if (deployResult == "Mined")
                {
                    contractAbi = ci.JsonInfo["result"]["ReadableReturnValue"].ToString().Replace("\"","");
                    ContractAbi = contractAbi;
                    _logger.WriteInfo($"Get contract ABI: TxId: {txId}, ABI address: {contractAbi}");
                    return true;
                }
            }

            return false;
        }

        private string ExecuteContractMethod(string rawTx)
        {
            var ci = new CommandInfo("BroadcastTransaction");
            ci.Parameter = rawTx;
            Ch.RpcBroadcastTx(ci);
            if (ci.Result)
            {
                ci.GetJsonInfo();
                var txId = ci.JsonInfo["TransactionId"].ToString();
                return txId;
            }

            Assert.IsTrue(ci.Result, $"Execute contract failed. Reason: {ci.GetErrorMessage()}");

            return string.Empty;
        }

        #endregion Methods
    }
}