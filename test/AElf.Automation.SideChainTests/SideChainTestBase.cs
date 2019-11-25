using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AElfChain.Common.Helpers;
using AElf.CSharp.Core.Utils;
using AElf.Types;
using AElfChain.Common;
using AElfChain.SDK.Models;
using log4net;
using Volo.Abp.Threading;

namespace AElf.Automation.SideChainTests
{
    public class SideChainTestBase
    {
        protected static readonly ILog _logger = Log4NetHelper.GetLogger();
        public ContractTester MainContracts;
        public ContractTester SideContractTester1;
        public ContractTester SideContractTester2;
        public ContractTester SideContractTester11;
        
        public ContractServices sideAServices;
        public ContractServices sideBServices;
        public ContractServices sideSideAServices;

        
        public static string MainChainUrl { get; } = "http://192.168.197.56:8001";
        public static string SideAChainUrl { get; } = "http://192.168.197.56:8011";
//        public static string SideSideAChainUrl { get; } = "http://192.168.197.56:8111";

//        public static string SideBChainUrl { get; } = "http://192.168.197.14:8002";

        public string InitAccount { get; } = "2RCLmZQ2291xDwSbDEJR6nLhFJcMkyfrVTq1i1YxWC4SdY49a6";

        public List<string> BpNodeAddress { get; set; }

        protected void Initialize()
        {
            //Init Logger
            Log4NetHelper.LogInit();
            var mainServices = new ContractServices(MainChainUrl, InitAccount, NodeOption.DefaultPassword, "AELF");
            MainContracts = new ContractTester(mainServices);

             sideAServices = new ContractServices(SideAChainUrl, InitAccount, NodeOption.DefaultPassword, "tDVV");
//             sideSideAServices = new ContractServices(SideSideAChainUrl, InitAccount, NodeOption.DefaultPassword, "AZpC");


             //             sideBServices = new ContractServices(SideBChainUrl, InitAccount, NodeOption.DefaultPassword, "tDVW");
             
             MainContracts = new ContractTester(mainServices);
             SideContractTester1 = new ContractTester(sideAServices);
//             SideContractTester11 = new ContractTester(sideSideAServices);
//             SideContractTester2 = new ContractTester(sideBServices);

            //Get BpNode Info
            BpNodeAddress = new List<string>();
            //线下 - 4bp 
            BpNodeAddress.Add("2RCLmZQ2291xDwSbDEJR6nLhFJcMkyfrVTq1i1YxWC4SdY49a6");
//            BpNodeAddress.Add("28Y8JA1i2cN6oHvdv7EraXJr9a1gY6D1PpJXw9QtRMRwKcBQMK");
//            BpNodeAddress.Add("2oSMWm1tjRqVdfmrdL8dgrRvhWu1FP8wcZidjS6wPbuoVtxhEz");
//            BpNodeAddress.Add("WRy3ADLZ4bEQTn86ENi5GXi5J1YyHp9e99pPso84v2NJkfn5k");
//            BpNodeAddress.Add("2ZYyxEH6j8zAyJjef6Spa99Jx2zf5GbFktyAQEBPWLCvuSAn8D");
//            BpNodeAddress.Add("2frDVeV6VxUozNqcFbgoxruyqCRAuSyXyfCaov6bYWc7Gkxkh2");
//            BpNodeAddress.Add("eFU9Quc8BsztYpEHKzbNtUpu9hGKgwGD2tyL13MqtFkbnAoCZ");
//            BpNodeAddress.Add("2V2UjHQGH8WT4TWnzebxnzo9uVboo67ZFbLjzJNTLrervAxnws");
//            BpNodeAddress.Add("EKRtNn3WGvFSTDewFH81S7TisUzs9wPyP4gCwTww32waYWtLB");
//            BpNodeAddress.Add("2LA8PSHTw4uub71jmS52WjydrMez4fGvDmBriWuDmNpZquwkNx");
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
                AsyncHelper.RunSync(() =>
                    tester.NodeManager.ApiService.GetBlockByHeightAsync(long.Parse(blockNumber), true));
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
                var txId = HashHelper.HexStringToHash(transactionIds[num]);
                var txRes = transactionStatus[num];
                var rawBytes = txId.ToByteArray().Concat(EncodingHelper.GetBytesFromUtf8String(txRes))
                    .ToArray();
                var txIdWithStatus = Hash.FromRawBytes(rawBytes);
                txIdsWithStatus.Add(txIdWithStatus);
                if (transactionIds[num] == TxId) index = num;
            }

            var bmt = BinaryMerkleTree.FromLeafNodes(txIdsWithStatus);
            var root = bmt.Root;
            var merklePath = new MerklePath();
            merklePath.MerklePathNodes.AddRange(bmt.GenerateMerklePath(index).MerklePathNodes);
            return merklePath;
        }
        
        protected Hash GetMerkleRoot(string blockNumber, string TxId, ContractServices tester)
        {
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
            }

            var bmt = BinaryMerkleTree.FromLeafNodes(txIdsWithStatus);
            var root = bmt.Root;
            return root;
        }
    }
}