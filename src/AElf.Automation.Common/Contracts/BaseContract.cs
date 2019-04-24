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
        public string CallAddress { get; set; }
        public Address CallAccount {get; set;}
        public string ContractAddress { get; set; }
        private ConcurrentQueue<string> TxResultList { get; set; }
        private readonly ILogHelper _logger = LogHelper.GetLogHelper();

        #endregion

        public BaseContract(CliHelper ch, string fileName, string callAddress)
        {
            Ch = ch;
            FileName = fileName;
            CallAddress = callAddress;
            CallAccount = Address.Parse(callAddress);
            TxResultList = new ConcurrentQueue<string>();

            UnlockAccount(callAddress);
            DeployContract();
        }

        public BaseContract(CliHelper ch, string contractAddress)
        {
            Ch = ch;
            ContractAddress = contractAddress;
            TxResultList = new ConcurrentQueue<string>();
        }

        public string ExecuteMethodWithTxId(string method, IMessage inputParameter)
        {
            string rawTx = GenerateBroadcastRawTx(method, inputParameter);

            var txId = ExecuteMethodWithTxId(rawTx);
            _logger.WriteInfo($"Transaction method: {method}, TxId: {txId}");
            TxResultList.Enqueue(txId);

            return txId;
        }

        public CommandInfo ExecuteMethodWithResult(string method, IMessage inputParameter)
        {
            string rawTx = GenerateBroadcastRawTx(method, inputParameter);

            var txId = ExecuteMethodWithTxId(rawTx);
            _logger.WriteInfo($"Transaction method: {method}, TxId: {txId}");

            //Chek result
            return CheckTransactionResult(txId, 30);
        }

        public bool GetTransactionResult(string txId, out CommandInfo ci)
        {
            ci = new CommandInfo(ApiMethods.GetTransactionResult);
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
                ci = new CommandInfo(ApiMethods.GetTransactionResult);
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
            CallAddress = account;
            CallAccount = Address.Parse(account);

            //Unlock
            var uc = new CommandInfo(ApiMethods.AccountUnlock);
            uc.Parameter = $"{account} {password} notimeout";
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
                var ci = new CommandInfo(ApiMethods.GetTransactionResult);
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
        public JObject CallViewMethod(string method, IMessage input)
        {
            return Ch.RpcQueryView(CallAddress, ContractAddress, method, input);
        }
        
        public T CallViewMethod<T>(string method, IMessage input) where T : IMessage<T>, new()
        {
            return Ch.RpcQueryView<T>(CallAddress, ContractAddress, method, input);
        }

        public void UnlockAccount(string account, string password = "123")
        {
            var uc = new CommandInfo(ApiMethods.AccountUnlock);
            uc.Parameter = $"{account} {password} notimeout";
            Ch.UnlockAccount(uc);
        }

        #region Private Methods

        private void DeployContract()
        {
            var ci = new CommandInfo(ApiMethods.DeploySmartContract);
            ci.Parameter = $"{FileName} {CallAddress}";
            Ch.RpcDeployContract(ci);
            if (ci.Result)
            {
                ci.GetJsonInfo();
                var txId = ci.JsonInfo["TransactionId"].ToString();
                _logger.WriteInfo($"Transaction: DeploySmartContract, TxId: {txId}");

                bool result = GetContractAddress(txId, out _);
                Assert.IsTrue(result, $"Get contract abi failed.");
            }

            Assert.IsTrue(ci.Result, $"Deploy contract failed. Reason: {ci.GetErrorMessage()}");
        }

        private string GenerateBroadcastRawTx(string method, IMessage inputParameter)
        {
            return Ch.RpcGenerateTransactionRawTx(CallAddress, ContractAddress, method, inputParameter);
        }

        private bool GetContractAddress(string txId, out string contractAddress)
        {
            contractAddress = string.Empty;
            var ci = CheckTransactionResult(txId);

            if (ci.Result)
            {
                ci.GetJsonInfo();
                ci.JsonInfo = ci.JsonInfo;
                string deployResult = ci.JsonInfo["result"]["Status"].ToString();
                _logger.WriteInfo($"Transaction: {txId}, Status: {deployResult}");
                if (deployResult == "Mined")
                {
                    contractAddress = ci.JsonInfo["result"]["ReadableReturnValue"].ToString().Replace("\"","");
                    ContractAddress = contractAddress;
                    _logger.WriteInfo($"Get contract address: TxId: {txId}, Address: {contractAddress}");
                    return true;
                }
            }

            return false;
        }

        private string ExecuteMethodWithTxId(string rawTx)
        {
            var ci = new CommandInfo(ApiMethods.BroadcastTransactions);
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