using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AElf.Automation.Common.Extensions;
using AElf.Automation.Common.Helpers;
using AElf.Automation.Common.WebApi;
using AElf.Automation.Common.WebApi.Dto;
using AElf.CSharp.Core.Utils;
using AElf.Kernel;

namespace AElf.Automation.SideChainTests
{    
    public class SideChainTestBase
    {
        public ContractTester Tester;
        public readonly ILogHelper _logger = LogHelper.GetLogHelper();
        
        public static string RpcUrl { get; } = "http://192.168.197.56:8001";       
 
        public IApiHelper CH { get; set; }
        public IApiService IS { get; set; }
        public string InitAccount { get; } = "2RCLmZQ2291xDwSbDEJR6nLhFJcMkyfrVTq1i1YxWC4SdY49a6";
        
        public List<string> BpNodeAddress { get; set; }        
        public List<string> UserList { get; set; }

        protected void Initialize()
        {
            CH = new WebApiHelper(RpcUrl, AccountManager.GetDefaultDataDir());
            IS = new WebApiService(RpcUrl);
            var contractServices = new ContractServices(CH,InitAccount,"Main");
            Tester = new ContractTester(contractServices);
            //Init Logger
            string logName = "CrossChainTest_" + DateTime.Now.ToString("MMddHHmmss") + ".log";
            string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", logName);
            _logger.InitLogHelper(dir);
            //Get BpNode Info
            BpNodeAddress = new List<string>();
//            BpNodeAddress.Add("28qLVdGMokanMAp9GwfEqiWnzzNifh8LS9as6mzJFX1gQBB823"); 
            BpNodeAddress.Add("2RCLmZQ2291xDwSbDEJR6nLhFJcMkyfrVTq1i1YxWC4SdY49a6");
            BpNodeAddress.Add("YF8o6ytMB7n5VF9d1RDioDXqyQ9EQjkFK3AwLPCH2b9LxdTEq");
            BpNodeAddress.Add("h6CRCFAhyozJPwdFRd7i8A5zVAqy171AVty3uMQUQp1MB9AKa");
        }

        protected ContractTester ChangeToSideChain(IApiHelper rpcApiHelper, string SideAChainAccount)
        {
            var contractServices = new ContractServices(rpcApiHelper,SideAChainAccount,"Side");
            var tester = new ContractTester(contractServices);
            return tester;
        }

        protected WebApiHelper ChangeRpc(string url)
        {
            var rpcApiHelper = new WebApiHelper(url, AccountManager.GetDefaultDataDir());
            return rpcApiHelper;
        }

        protected MerklePath GetMerklePath(string blockNumber,int index,IApiService apiService)
        {
            var blockInfoResult = apiService.GetBlockByHeight(long.Parse(blockNumber),true).Result;
            var transactionIds = blockInfoResult.Body.Transactions;
            var transactionStatus = new List<string>();
            
            foreach (var transactionId in transactionIds)
            {
                var txResult = apiService.GetTransactionResult(transactionId).Result;
                var resultStatus = txResult.Status;
                transactionStatus.Add(resultStatus);
            }

            var txIdsWithStatus = new List<Hash>();
            for(int num =0; num<transactionIds.Count;num++)
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
            var merklePath = bmt.GenerateMerklePath(index);

            return merklePath;
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