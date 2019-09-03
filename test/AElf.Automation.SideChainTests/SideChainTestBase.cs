using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using AElf.Automation.Common.Helpers;
using AElf.Automation.Common.OptionManagers;
using AElf.CSharp.Core.Utils;
using AElf.Types;
using AElfChain.SDK.Models;
using log4net;

namespace AElf.Automation.SideChainTests
{
    public class SideChainTestBase
    {
        private static int Timeout { get; set; }
        public ContractTester Tester;
        protected static readonly ILog _logger = Log4NetHelper.GetLogger();

//        public static string MainChainUrl { get; } = "http://127.0.0.1:9000";    
        public static string MainChainUrl { get; } = "http://192.168.197.44:8000";
//        public static string RpcUrl { get; } = "http://192.168.197.56:8001";

//        public string InitAccount { get; } = "2RCLmZQ2291xDwSbDEJR6nLhFJcMkyfrVTq1i1YxWC4SdY49a6";
        public string InitAccount { get; } = "28Y8JA1i2cN6oHvdv7EraXJr9a1gY6D1PpJXw9QtRMRwKcBQMK";

        public List<string> BpNodeAddress { get; set; }
        public List<string> UserList { get; set; }

        protected void Initialize()
        {
            //Init Logger
            Log4NetHelper.LogInit();
            var keyStore = CommonHelper.GetCurrentDataDir();
            var chainId = ChainHelper.ConvertBase58ToChainId("AELF");
            var contractServices = new ContractServices(MainChainUrl, InitAccount, Account.DefaultPassword, chainId);
            Tester = new ContractTester(contractServices);

            //Get BpNode Info
            BpNodeAddress = new List<string>();
            //线下 - 4bp 
//            BpNodeAddress.Add("7BSmhiLtVqHSUVGuYdYbsfaZUGpkL2ingvCmVPx66UR5L5Lbs");
            BpNodeAddress.Add("28Y8JA1i2cN6oHvdv7EraXJr9a1gY6D1PpJXw9QtRMRwKcBQMK");
            BpNodeAddress.Add("2oSMWm1tjRqVdfmrdL8dgrRvhWu1FP8wcZidjS6wPbuoVtxhEz");

//            BpNodeAddress.Add("28qLVdGMokanMAp9GwfEqiWnzzNifh8LS9as6mzJFX1gQBB823"); 
            BpNodeAddress.Add("2RCLmZQ2291xDwSbDEJR6nLhFJcMkyfrVTq1i1YxWC4SdY49a6");
            BpNodeAddress.Add("YF8o6ytMB7n5VF9d1RDioDXqyQ9EQjkFK3AwLPCH2b9LxdTEq");
            BpNodeAddress.Add("XSYSQ2kf4MCcSu1uWnZ9mTtgM9pq6yu85HUtV2j743mk8b4WF");
            BpNodeAddress.Add("h6CRCFAhyozJPwdFRd7i8A5zVAqy171AVty3uMQUQp1MB9AKa");
        }

        protected ContractTester GetSideChain(string url, string initAccount, string chainId)
        {
            var keyStore = CommonHelper.GetCurrentDataDir();
            var chain = ChainHelper.ConvertBase58ToChainId(chainId);
            var contractServices = new ContractServices(url, initAccount, Account.DefaultPassword, chain);
            var tester = new ContractTester(contractServices);
            return tester;
        }

        protected MerklePath GetMerklePath(string blockNumber, string TxId, ContractTester tester)
        {
            var index = 0;
            var ci = new CommandInfo(ApiMethods.GetBlockByHeight) {Parameter = $"{blockNumber} true"};
            ci = tester.ApiHelper.ExecuteCommand(ci);
            var blockInfoResult = ci.InfoMsg as BlockDto;
            var transactionIds = blockInfoResult.Body.Transactions;
            var transactionStatus = new List<string>();

            foreach (var transactionId in transactionIds)
            {
                var CI = new CommandInfo(ApiMethods.GetTransactionResult) {Parameter = transactionId};
                var result = tester.ApiHelper.ExecuteCommand(CI);
                var txResult = result.InfoMsg as TransactionResultDto;

                var resultStatus =
                    (TransactionResultStatus) Enum.Parse(typeof(TransactionResultStatus),
                        txResult.Status, true);
                transactionStatus.Add(resultStatus.ToString());
            }

            var txIdsWithStatus = new List<Hash>();
            for (var num = 0; num < transactionIds.Count; num++)
            {
                var txId = HashHelper.HexStringToHash(transactionIds[num].ToString());
                string txRes = transactionStatus[num];
                var rawBytes = txId.ToByteArray().Concat(EncodingHelper.GetBytesFromUtf8String(txRes))
                    .ToArray();
                var txIdWithStatus = Hash.FromRawBytes(rawBytes);
                txIdsWithStatus.Add(txIdWithStatus);
                if (transactionIds[num] == TxId)
                {
                    index = num;
                }
            }

            var bmt = BinaryMerkleTree.FromLeafNodes(txIdsWithStatus);
            var root = bmt.Root;
            var merklePath = new MerklePath();
            merklePath.MerklePathNodes.AddRange(bmt.GenerateMerklePath(index).MerklePathNodes);
            return merklePath;
        }

        protected void TestCleanUp()
        {
            if (UserList.Count == 0) return;
            _logger.Info("Delete all account files created.");
            foreach (var item in UserList)
            {
                var file = Path.Combine(CommonHelper.GetCurrentDataDir(), $"{item}.json");
                File.Delete(file);
            }
        }

        protected CommandInfo CheckTransactionResult(ContractServices services, string txId, int maxTimes = -1)
        {
            if (maxTimes == -1)
            {
                maxTimes = Timeout == 0 ? 600 : Timeout;
            }

            CommandInfo ci = null;
            var checkTimes = 1;
            while (checkTimes <= maxTimes)
            {
                ci = new CommandInfo(ApiMethods.GetTransactionResult) {Parameter = txId};
                services.ApiHelper.GetTransactionResult(ci);
                if (ci.Result)
                {
                    if (ci.InfoMsg is TransactionResultDto transactionResult)
                    {
                        var status = transactionResult.Status.ConvertTransactionResultStatus();
                        switch (status)
                        {
                            case TransactionResultStatus.Mined:
                                _logger.Info($"Transaction {txId} status: {transactionResult.Status}");
                                return ci;
                            case TransactionResultStatus.NotExisted:
                                _logger.Error($"Transaction {txId} status: {transactionResult.Status}");
                                return ci;
                            case TransactionResultStatus.Failed:
                            {
                                var message = $"Transaction {txId} status: {transactionResult.Status}";
                                message +=
                                    $"\r\nMethodName: {transactionResult.Transaction.MethodName}, Parameter: {transactionResult.Transaction.Params}";
                                message += $"\r\nError Message: {transactionResult.Error}";
                                _logger.Error(message);
                                return ci;
                            }
                        }
                    }
                }

                checkTimes++;
                Thread.Sleep(500);
            }

            if (ci != null)
            {
                var result = ci.InfoMsg as TransactionResultDto;
                _logger.Error(result?.Error);
            }

            _logger.Error("Transaction execute status cannot be 'Mined' after one minutes.");
            return ci;
        }
    }
}