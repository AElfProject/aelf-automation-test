﻿using System;
 using System.Text;
using System.Collections.Concurrent;
using System.Threading;
using AElf.Automation.Common.Helpers;
using AElf.Automation.Common.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
 using NServiceKit.Common.Extensions;

namespace AElf.Automation.Common.Contracts
{
    public class BaseContract
    {
        #region Priority
        private CliHelper CH { get; set; }
        private string FileName { get; set; }
        public string Account { get; set; }
        public string ContractAbi { get; set; }

        private ConcurrentQueue<string> TxResultList { get; set; }
        private ILogHelper Logger = LogHelper.GetLogHelper();
        #endregion

        public BaseContract(CliHelper ch, string fileName, string account)
        {
            CH = ch;
            FileName = fileName;
            Account = account;
            TxResultList = new ConcurrentQueue<string>();

            DeployContract();
            LoadContractAbi();
        }

        public BaseContract(CliHelper ch, string contractAbi)
        {
            CH = ch;
            ContractAbi = contractAbi;
            TxResultList = new ConcurrentQueue<string>();

            LoadContractAbi();
        }

        public string ExecuteContractMethod(string method, params string[] paramArray)
        {
            string rawTx = GenerateBroadcastRawTx(method, paramArray);

            var txId = ExecuteContractMethod(rawTx);
            Logger.WriteInfo($"Transaction method: {method}, TxId: {txId}");
            TxResultList.Enqueue(txId);

            return txId;
        }

        public CommandInfo ExecuteContractMethodWithResult(string method, params string[] paramArray)
        {
            string rawTx = GenerateBroadcastRawTx(method, paramArray);

            var txId = ExecuteContractMethod(rawTx);
            Logger.WriteInfo($"Transaction method: {method}, TxId: {txId}");
            
            //Chek result
            return CheckTransactionResult(txId, 10);
        }

        public bool GetTransactionResult(string txId, out CommandInfo ci)
        {
            ci = new CommandInfo("get_tx_result");
            ci.Parameter = txId;
            CH.ExecuteCommand(ci);

            if (ci.Result)
            {
                ci.GetJsonInfo();
                ci.JsonInfo = ci.JsonInfo;
                string txResult = ci.JsonInfo["result"]["result"]["tx_status"].ToString();
                Logger.WriteInfo($"Transaction: {txId}, Status: {txResult}");

                return txResult == "Mined";
            }

            Logger.WriteError(ci.GetErrorMessage());
            return false;
        }

        public CommandInfo CheckTransactionResult(string txId, int maxTimes = 60)
        {
            CommandInfo ci = null;
            int checkTimes = 1;
            while (checkTimes <= maxTimes)
            {
                ci = new CommandInfo("get_tx_result");
                ci.Parameter = txId;
                CH.RpcGetTxResult(ci);
                if (ci.Result)
                {
                    ci.GetJsonInfo();
                    ci.JsonInfo = ci.JsonInfo;
                    string txResult = ci.JsonInfo["result"]["result"]["tx_status"].ToString();
                    if (txResult == "Mined")
                    {
                        Logger.WriteInfo($"Transaction status: {txResult}");
                        return ci;
                    }
                    if (txResult == "Failed")
                    {
                        Logger.WriteInfo($"Transaction status: {txResult}");
                        Logger.WriteError(ci.JsonInfo.ToString());
                        return ci;
                    }
                }

                checkTimes++;
                Thread.Sleep(1000);
            }

            Logger.WriteError(ci.JsonInfo.ToString());
            Assert.IsTrue(false, "Transaction execute status cannot be 'Mined' after one minutes.");

            return ci;
        }

        /// <summary>
        /// 切换测试账号
        /// </summary>
        /// <param name="account"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public bool SetAccount(string account, string password = "123")
        {
            Account = account;

            //Unlock
            var uc = new CommandInfo("account unlock", "account");
            uc.Parameter = String.Format("{0} {1} {2}", account, password, "notimeout");
            uc = CH.ExecuteCommand(uc);

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
                var ci = new CommandInfo("get_tx_result");
                ci.Parameter = txId;
                CH.RpcGetTxResult(ci);
                if (ci.Result)
                {
                    ci.GetJsonInfo();
                    ci.JsonInfo = ci.JsonInfo;
                    string txResult = ci.JsonInfo["result"]["result"]["tx_status"].ToString();

                    if (txResult == "Mined")
                        continue;
                    if (txResult == "Failed")
                    {
                        Logger.WriteInfo($"Transaction status: {txResult}");
                        Logger.WriteError(ci.JsonInfo.ToString());
                        continue;
                    }

                    TxResultList.Enqueue(txId);
                }

                if (queueLength == TxResultList.Count)
                {
                    queueSameTimes++;
                    Thread.Sleep(2000);
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
        /// <param name="paramArray"></param>
        /// <returns></returns>
        public JObject CallContractViewMethod(string method, params string[] paramArray)
        {
            var resp = CH.RpcQueryResult(Account, ContractAbi, method, paramArray);
            if(resp == string.Empty)
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
            if (info["result"]["return"] == null)
                return string.Empty;

            return DataHelper.ConvertHexInfo(info["result"]["return"].ToString(), hexValue);
        }

        #region Private Methods

        private void DeployContract()
        {
            var txId = string.Empty;
            var ci = new CommandInfo("deploy_contract");
            ci.Parameter = $"{FileName} 0 {Account}";
            CH.RpcDeployContract(ci);
            if (ci.Result)
            {
                ci.GetJsonInfo();
                txId = ci.JsonInfo["txId"].ToString();
                Logger.WriteInfo($"Transaction: DeployContract, TxId: {txId}");

                bool result = GetContractAbi(txId, out var contractAbi);
                Assert.IsTrue(result, $"Get contract abi failed.");
            }

            Assert.IsTrue(ci.Result, $"Deploy contract failed. Reason: {ci.GetErrorMessage()}");
        }

        private void LoadContractAbi()
        {
            var ci = new CommandInfo("load_contract_abi");
            ci.Parameter = ContractAbi;
            CH.RpcLoadContractAbi(ci);

            Assert.IsTrue(ci.Result, $"Load contract abi failed. Reason: {ci.GetErrorMessage()}");
        }

        private string GenerateBroadcastRawTx(string method, params string[] paramArray)
        {
            return CH.RpcGenerateTransactionRawTx(Account, ContractAbi, method, paramArray);
        }

        private bool GetContractAbi(string txId, out string contractAbi)
        {
            contractAbi = string.Empty;
            var ci = CheckTransactionResult(txId);

            if (ci.Result)
            {
                ci.GetJsonInfo();
                ci.JsonInfo = ci.JsonInfo;
                string deployResult = ci.JsonInfo["result"]["result"]["tx_status"].ToString();
                Logger.WriteInfo($"Transaction: {txId}, Status: {deployResult}");
                if (deployResult == "Mined")
                {
                    contractAbi = ci.JsonInfo["result"]["result"]["return"].ToString();
                    ContractAbi = contractAbi;
                    Logger.WriteInfo($"Get contract ABI: TxId: {txId}, ABI address: {contractAbi}");
                    return true;
                }
            }

            return false;
        }

        private string ExecuteContractMethod(string rawTx)
        {
            string txId = string.Empty;
            var ci = new CommandInfo("broadcast_tx");
            ci.Parameter = rawTx;
            CH.RpcBroadcastTx(ci);
            if (ci.Result)
            {
                ci.GetJsonInfo();
                txId = ci.JsonInfo["txId"].ToString();
                return txId;
            }
            Assert.IsTrue(ci.Result, $"Execute contract failed. Reason: {ci.GetErrorMessage()}");

            return string.Empty;
        }

        #endregion Methods
    }
}