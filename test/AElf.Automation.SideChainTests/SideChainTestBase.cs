using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Acs7;
using AElf.Automation.Common.OptionManagers;
using AElf.Automation.Common.Helpers;
using AElf.Automation.Common.WebApi;
using AElf.Automation.Common.WebApi.Dto;
using AElf.CSharp.Core.Utils;
using AElf.Kernel;
using AElf.Types;

namespace AElf.Automation.SideChainTests
{
    public class SideChainTestBase
    {
        public ContractTester Tester;
        public readonly ILogHelper _logger = LogHelper.GetLogHelper();
        
//        public static string RpcUrl { get; } = "http://127.0.0.1:9000";    
        public static string RpcUrl { get; } = "http://192.168.197.14:8001";
//        public static string RpcUrl { get; } = "http://192.168.197.56:8001";
 
        public IApiHelper CH { get; set; }
//        public string InitAccount { get; } = "2RCLmZQ2291xDwSbDEJR6nLhFJcMkyfrVTq1i1YxWC4SdY49a6";
        public string InitAccount { get; } = "7BSmhiLtVqHSUVGuYdYbsfaZUGpkL2ingvCmVPx66UR5L5Lbs";
        
        public List<string> BpNodeAddress { get; set; }        
        public List<string> UserList { get; set; }

        protected void Initialize()
        {
            //Init Logger
            string logName = "CrossChainTest_" + DateTime.Now.ToString("MMddHHmmss") + ".log";
            string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", logName);
            _logger.InitLogHelper(dir);
            
            CH = new WebApiHelper(RpcUrl, CommonHelper.GetCurrentDataDir());
            var contractServices = new ContractServices(CH, InitAccount, "123");
            Tester = new ContractTester(contractServices);

            //Get BpNode Info
            BpNodeAddress = new List<string>();
            //线下 - 4bp 
            BpNodeAddress.Add("7BSmhiLtVqHSUVGuYdYbsfaZUGpkL2ingvCmVPx66UR5L5Lbs");
            BpNodeAddress.Add("28Y8JA1i2cN6oHvdv7EraXJr9a1gY6D1PpJXw9QtRMRwKcBQMK");
            BpNodeAddress.Add("2oSMWm1tjRqVdfmrdL8dgrRvhWu1FP8wcZidjS6wPbuoVtxhEz");
            
//            BpNodeAddress.Add("28qLVdGMokanMAp9GwfEqiWnzzNifh8LS9as6mzJFX1gQBB823"); 
//            BpNodeAddress.Add("2RCLmZQ2291xDwSbDEJR6nLhFJcMkyfrVTq1i1YxWC4SdY49a6");
//            BpNodeAddress.Add("YF8o6ytMB7n5VF9d1RDioDXqyQ9EQjkFK3AwLPCH2b9LxdTEq");
//            BpNodeAddress.Add("XSYSQ2kf4MCcSu1uWnZ9mTtgM9pq6yu85HUtV2j743mk8b4WF");
//            BpNodeAddress.Add("h6CRCFAhyozJPwdFRd7i8A5zVAqy171AVty3uMQUQp1MB9AKa");
        }

        protected ContractTester ChangeToSideChain(IApiHelper rpcApiHelper, string SideAChainAccount)
        {
            var contractServices = new ContractServices(rpcApiHelper, SideAChainAccount, "123");
            var tester = new ContractTester(contractServices);
            return tester;
        }

        protected WebApiHelper ChangeRpc(string url)
        {
            var rpcApiHelper = new WebApiHelper(url, CommonHelper.GetCurrentDataDir());
            return rpcApiHelper;
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
                var txId = Hash.LoadHex(transactionIds[num]);
                var txRes = transactionStatus[num];
                var rawBytes = txId.DumpByteArray().Concat(EncodingHelper.GetBytesFromUtf8String(txRes))
                    .ToArray();
                var txIdWithStatus = Hash.FromRawBytes(rawBytes);
                txIdsWithStatus.Add(txIdWithStatus);
                if (transactionIds[num] == TxId)
                {
                    index = num;
                }
            }

            var bmt = new BinaryMerkleTree();
            bmt.AddNodes(txIdsWithStatus);
            var root = bmt.ComputeRootHash();
            var merklePath = new MerklePath();
            merklePath.Path.AddRange(bmt.GenerateMerklePath(index));
            return merklePath;
        }

        protected void TestCleanUp()
        {
            if (UserList.Count == 0) return;
            _logger.WriteInfo("Delete all account files created.");
            foreach (var item in UserList)
            {
                var file = Path.Combine(CommonHelper.GetCurrentDataDir(), $"{item}.ak");
                File.Delete(file);
            }
        }
    }
}