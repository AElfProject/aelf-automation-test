using System.IO;
using System.Threading;
using AElf.Automation.Common.Helpers;
using AElf.Automation.Common.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AElf.Automation.ContractsTesting.Contracts
{
    public class ContractBase
    {
        public CliHelper CH { get; set; }
        public string FileName { get; set; }
        public string Account { get; set; }
        public string ContractAbi { get; set; }
        public ILogHelper Logger = LogHelper.GetLogHelper();

        public ContractBase(CliHelper ch, string fileName, string account)
        {
            CH = ch;
            FileName = fileName;
            Account = account;
        }

        public ContractBase(CliHelper ch, string contractAbi)
        {
            CH = ch;
            ContractAbi = contractAbi;
        }

        public void DeployContract(out string txId)
        {
            txId = string.Empty;
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

        public void ExecuteContractMethod(out string txId, string method, params string[] paramArray)
        {
            string rawTx = GenerateBroadcastRawTx(method, paramArray);

            ExecuteContractMethod(rawTx, out txId);
            Logger.WriteInfo($"Transaction method: {method}, TxId: {txId}");
        }

        public void ExecuteContractMethod(string rawTx, out string txId)
        {
            txId = string.Empty;
            var ci = new CommandInfo("broadcast_tx");
            ci.Parameter = rawTx;
            CH.RpcBroadcastTx(ci);
            if (ci.Result)
            {
                ci.GetJsonInfo();
                txId = ci.JsonInfo["txId"].ToString();
            }
            Assert.IsTrue(ci.Result, $"Execute contract failed. Reason: {ci.GetErrorMessage()}");
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

        public void CheckTransactionResult(out CommandInfo ci, string txId, int checkTimes = 15)
        {
            ci = new CommandInfo("get_tx_result");
            ci.Parameter = txId;
            while (checkTimes > 0)
            {
                CH.RpcGetTxResult(ci);
                if (ci.Result)
                {
                    ci.GetJsonInfo();
                    ci.JsonInfo = ci.JsonInfo;
                    string txResult = ci.JsonInfo["result"]["result"]["tx_status"].ToString();
                    Logger.WriteInfo($"Transaction: {txId}, Status: {txResult}");

                    if (txResult == "Mined")
                        return;
                }

                checkTimes--;
                Thread.Sleep(2000);
            }

            Logger.WriteError(ci.JsonInfo.ToString());
            Assert.IsTrue(false, "Transaction execute status cannot mined.");
        }

        private bool GetContractAbi(string txId, out string contractAbi)
        {
            contractAbi = string.Empty;
            int checkTimes = 10;

            while (checkTimes > 0)
            {
                var ci = new CommandInfo("get_tx_result");
                ci.Parameter = txId;
                CH.RpcGetTxResult(ci);

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


                    checkTimes--;
                    Thread.Sleep(2000);
                }
            }

            return false;
        }

    }
}