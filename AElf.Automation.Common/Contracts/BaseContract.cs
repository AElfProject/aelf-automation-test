﻿using System;
 using System.Text;
using System.Collections.Concurrent;
using System.Threading;
using AElf.Automation.Common.Helpers;
using AElf.Automation.Common.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;

namespace AElf.Automation.Common.Contracts
{
    public class BaseContract
    {
        public CliHelper CH { get; set; }
        public string FileName { get; set; }
        public string Account { get; set; }
        public string ContractAbi { get; set; }

        public ConcurrentQueue<string> TxResultList { get; set; }

        public ILogHelper Logger = LogHelper.GetLogHelper();

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

        public void DeployContract()
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

        public void LoadContractAbi()
        {
            var ci = new CommandInfo("load_contract_abi");
            ci.Parameter = ContractAbi;
            CH.RpcLoadContractAbi(ci);

            Assert.IsTrue(ci.Result, $"Load contract abi failed. Reason: {ci.GetErrorMessage()}");
        }

        public string GenerateBroadcastRawTx(string method, params string[] paramArray)
        {
            return CH.RpcGenerateTransactionRawTx(Account, ContractAbi, method, paramArray);
        }

        public string ExecuteContractMethod(string method, params string[] paramArray)
        {
            string rawTx = GenerateBroadcastRawTx(method, paramArray);

            var txId = ExecuteContractMethod(rawTx);
            Logger.WriteInfo($"Transaction method: {method}, TxId: {txId}");
            TxResultList.Enqueue(txId);

            return txId;
        }

        public string ExecuteContractMethod(string rawTx)
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
            Logger.WriteInfo($"Check result of transaction Id： {txId}");
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
                    if(checkTimes%3 == 0 && txResult == "Pending")
                        Logger.WriteInfo($"Check times: {checkTimes/3}, Status: {txResult}");

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

        public int GetValueFromHex(JObject jsonInfo)
        {
            string result = string.Empty;
            string message = string.Empty;
            try
            {
                result = jsonInfo["result"]["result"]["tx_status"].ToString();
                message = jsonInfo["result"]["result"]["return"].ToString();
                if (result == "Mined")
                    return Convert.ToInt32(message, 16);
                else
                    Logger.WriteError("Transaction result ：{0}， return message: {1}", result, message);
            }
            catch (Exception)
            {
                Logger.WriteError("Convert from hex todDecimal got exception. return message: {0}", message);
            }

            return 0;
        }

        public JObject QueryReadOnlyInfo(string method, params string[] paramArray)
        {
            var resp = CH.RpcQueryResult(Account, ContractAbi, method, paramArray);
            if(resp == string.Empty)
                return new JObject();

            return JObject.Parse(resp);
        }

        public string ConvertQueryResult(JObject info, bool convertHex = false)
        {
            if (info["result"]["return"] == null)
                return string.Empty;
            if (convertHex)
                return ConvertHexToString(info["result"]["return"].ToString());

            return info["result"]["return"].ToString();
        }

        public static string ConvertHexToString(string HexValue)
        {
            string StrValue = "";
            while (HexValue.Length > 0)
            {
                StrValue += System.Convert.ToChar(System.Convert.ToUInt32(HexValue.Substring(0, 2), 16)).ToString();
                HexValue = HexValue.Substring(2, HexValue.Length - 2);
            }
            return StrValue;
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
    }
}