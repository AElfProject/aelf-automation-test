using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Acs7;
using AElf.Automation.Common.OptionManagers;
using AElf.Automation.Common.Helpers;
using AElf.Automation.Common.WebApi;
using AElf.CSharp.Core.Utils;
using AElf.Kernel;
using AElf.Types;

namespace AElf.Automation.SideChainTests
{
    public class SideChainTestBase
    {
        public ContractTester Tester;
        public readonly ILogHelper _logger = LogHelper.GetLogHelper();
        
//        public static string RpcUrl { get; } = "http://192.168.197.56:8001";    
        public static string RpcUrl { get; } = "http://192.168.197.12:8001";
 
        public IApiHelper CH { get; set; }
        public IApiService IS { get; set; }
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
            
            CH = new WebApiHelper(RpcUrl, AccountManager.GetDefaultDataDir());
            IS = new WebApiService(RpcUrl);
            var contractServices = new ContractServices(CH, InitAccount, "Main");
            Tester = new ContractTester(contractServices);

            //Get BpNode Info
            BpNodeAddress = new List<string>();
//            BpNodeAddress.Add("28qLVdGMokanMAp9GwfEqiWnzzNifh8LS9as6mzJFX1gQBB823"); 
//            BpNodeAddress.Add("2RCLmZQ2291xDwSbDEJR6nLhFJcMkyfrVTq1i1YxWC4SdY49a6");
//            BpNodeAddress.Add("YF8o6ytMB7n5VF9d1RDioDXqyQ9EQjkFK3AwLPCH2b9LxdTEq");
//            BpNodeAddress.Add("QmsbxArByariFrB2TwpPA7F51xyQySsjurtSR57Dibpohk8qH");
//            BpNodeAddress.Add("7ZYaVinJ5mi7k64YTQZxaYtbKFb5GzcgeX37cJ5b3bNa6W3G8");
//            BpNodeAddress.Add("KJoGS8hwpgqYaNX6EMA38tqjytuT5RCjaX4gMwfjYXihMRThi");
//            BpNodeAddress.Add("XSYSQ2kf4MCcSu1uWnZ9mTtgM9pq6yu85HUtV2j743mk8b4WF");
//            BpNodeAddress.Add("h6CRCFAhyozJPwdFRd7i8A5zVAqy171AVty3uMQUQp1MB9AKa");
        }

        protected ContractTester ChangeToSideChain(IApiHelper rpcApiHelper, string SideAChainAccount)
        {
            var contractServices = new ContractServices(rpcApiHelper, SideAChainAccount, "Side");
            var tester = new ContractTester(contractServices);
            return tester;
        }

        protected WebApiHelper ChangeRpc(string url)
        {
            var rpcApiHelper = new WebApiHelper(url, AccountManager.GetDefaultDataDir());
            return rpcApiHelper;
        }

        protected MerklePath GetMerklePath(string blockNumber, int index, IApiService apiService)
        {
            var blockInfoResult = apiService.GetBlockByHeight(long.Parse(blockNumber), true).Result;
            var transactionIds = blockInfoResult.Body.Transactions;
            var transactionStatus = new List<string>();

            foreach (var transactionId in transactionIds)
            {
                var txResult = apiService.GetTransactionResult(transactionId).Result;
                var resultStatus = txResult.Status;
                transactionStatus.Add(resultStatus);
            }

            var txIdsWithStatus = new List<Hash>();
            for (int num = 0; num < transactionIds.Count; num++)
            {
                var txId = Hash.LoadHex(transactionIds[num].ToString());
                string txRes = transactionStatus[num];
                var rawBytes = txId.DumpByteArray().Concat(EncodingHelper.GetBytesFromUtf8String(txRes))
                    .ToArray();
                var txIdWithStatus = Hash.FromRawBytes(rawBytes);
                txIdsWithStatus.Add(txIdWithStatus);
            }

            var bmt = new BinaryMerkleTree();
            bmt.AddNodes(txIdsWithStatus);
            var root = bmt.ComputeRootHash();
            var merklePath = new MerklePath();
            merklePath.Path.AddRange(bmt.GenerateMerklePath(index));

            //return merklePath;
            return null;
        }

        protected void TestCleanUp()
        {
            if (UserList.Count == 0) return;
            _logger.WriteInfo("Delete all account files created.");
            foreach (var item in UserList)
            {
                var file = Path.Combine(AccountManager.GetDefaultDataDir(), $"{item}.ak");
                File.Delete(file);
            }
        }
    }
}