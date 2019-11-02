using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using AElf.Automation.Common;
using AElf.Automation.Common.Helpers;
using AElf.Automation.Common.Managers;
using AElf.CSharp.Core.Utils;
using AElf.Types;
using AElfChain.SDK.Models;
using log4net;
using Volo.Abp.Threading;

namespace AElf.Automation.SideChainTests
{
    public class SideChainTestBase
    {
        private static int Timeout { get; set; }
        public ContractTester MainContracts;
        public ContractServices sideAServices;
        public ContractServices sideBServices;

        protected static readonly ILog _logger = Log4NetHelper.GetLogger();

        public static string MainChainUrl { get; } = "http://35.183.35.159:8000";
        public static string SideAChainUrl { get; } = "http://54.154.233.225:8000";
        public static string SideBChainUrl { get; } = "http://54.92.109.42:8000";

        public string InitAccount { get; } = "2ZYyxEH6j8zAyJjef6Spa99Jx2zf5GbFktyAQEBPWLCvuSAn8D";

        public List<string> BpNodeAddress { get; set; }

        protected void Initialize()
        {
            //Init Logger
            Log4NetHelper.LogInit();
            var chainId = ChainHelper.ConvertBase58ToChainId("TELF");
            var mainServices = new ContractServices(MainChainUrl, InitAccount, NodeOption.DefaultPassword, "TELF");
            MainContracts = new ContractTester(mainServices);

             sideAServices = new ContractServices(SideAChainUrl, InitAccount, NodeOption.DefaultPassword, "2112");
            
             sideBServices = new ContractServices(SideBChainUrl, InitAccount, NodeOption.DefaultPassword, "2112");

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
            var contractServices = new ContractServices(url, initAccount, NodeOption.DefaultPassword, chainId);
            var tester = new ContractTester(contractServices);
            return tester;
        }

        protected MerklePath GetMerklePath(string blockNumber, string TxId, ContractServices tester)
        {
            var index = 0;
            var blockInfoResult =
                AsyncHelper.RunSync(() => tester.NodeManager.ApiService.GetBlockByHeightAsync(long.Parse(blockNumber), true));
            var transactionIds = blockInfoResult.Body.Transactions;
            var transactionStatus = new List<string>();

            foreach (var transactionId in transactionIds)
            {
                var txResult = AsyncHelper.RunSync(() =>
                    tester.NodeManager.ApiService.GetTransactionResultAsync(transactionId));
                var resultStatus = txResult.Status.ConvertTransactionResultStatus();
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

        protected TransactionResultDto CheckTransactionResult(ContractServices services, string txId, int maxTimes = -1)
        {
            if (maxTimes == -1)
            {
                maxTimes = Timeout == 0 ? 600 : Timeout;
            }

            TransactionResultDto transactionResult = null;
            var checkTimes = 1;
            while (checkTimes <= maxTimes)
            {
                transactionResult = AsyncHelper.RunSync(()=> services.NodeManager.ApiService.GetTransactionResultAsync(txId));
                var status = transactionResult.Status.ConvertTransactionResultStatus();
                switch (status)
                {
                    case TransactionResultStatus.Mined:
                        _logger.Info($"Transaction {txId} status: {transactionResult.Status}");
                        return transactionResult;
                    case TransactionResultStatus.NotExisted:
                        _logger.Error($"Transaction {txId} status: {transactionResult.Status}");
                        return transactionResult;
                    case TransactionResultStatus.Failed:
                    {
                        var message = $"Transaction {txId} status: {transactionResult.Status}";
                        message +=
                            $"\r\nMethodName: {transactionResult.Transaction.MethodName}, Parameter: {transactionResult.Transaction.Params}";
                        message += $"\r\nError Message: {transactionResult.Error}";
                        _logger.Error(message);
                        return transactionResult;
                    }
                }

                checkTimes++;
                Thread.Sleep(500);
            }

            _logger.Error("Transaction execute status cannot be 'Mined' after one minutes.");
            return transactionResult;
        }
    }
}