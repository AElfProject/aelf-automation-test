using System.Collections.Generic;
using System.Linq;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.MultiToken;
using AElfChain.Common.Helpers;
using AElf.CSharp.Core.Utils;
using AElf.Types;
using AElfChain.Common;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Managers;
using Google.Protobuf.WellKnownTypes;
using log4net;
using ProtoBuf.WellKnownTypes;
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
        public TokenContractContainer.TokenContractStub TokenContractStub;
        public TokenContractContainer.TokenContractStub side1TokenContractStub;
        public TokenContractContainer.TokenContractStub side2TokenContractStub;


        public List<string> Miners;

        
        public static string MainChainUrl { get; } = "http://192.168.197.14:8000";
        public static string SideAChainUrl { get; } = "http://192.168.197.14:8001";
//        public static string SideSideAChainUrl { get; } = "http://192.168.197.56:8111";

        public static string SideBChainUrl { get; } = "http://192.168.197.14:8002";

        public string InitAccount { get; } = "28Y8JA1i2cN6oHvdv7EraXJr9a1gY6D1PpJXw9QtRMRwKcBQMK";
        protected void Initialize()
        {
            //Init Logger
            Log4NetHelper.LogInit();
            var mainServices = new ContractServices(MainChainUrl, InitAccount, NodeOption.DefaultPassword, "AELF");
            MainContracts = new ContractTester(mainServices);

             sideAServices = new ContractServices(SideAChainUrl, InitAccount, NodeOption.DefaultPassword, "tDVV");
//             sideSideAServices = new ContractServices(SideSideAChainUrl, InitAccount, NodeOption.DefaultPassword, "AZpC");
             sideBServices = new ContractServices(SideBChainUrl, InitAccount, NodeOption.DefaultPassword, "tDVW");
             
             MainContracts = new ContractTester(mainServices);
             SideContractTester1 = new ContractTester(sideAServices);
//             SideContractTester11 = new ContractTester(sideSideAServices);
             SideContractTester2 = new ContractTester(sideBServices);
            TokenContractStub = MainContracts.TokenContractStub;
            side1TokenContractStub = SideContractTester1.TokenContractStub;
            side2TokenContractStub = SideContractTester2.TokenContractStub;
            Miners = new List<string>();
            Miners = (new AuthorityManager(MainContracts.NodeManager, InitAccount).GetCurrentMiners());
        }

        protected ContractTester GetSideChain(string url, string initAccount, string chainId)
        {
            var keyStore = CommonHelper.GetCurrentDataDir();
            var contractServices = new ContractServices(url, initAccount, NodeOption.DefaultPassword, chainId);
            var tester = new ContractTester(contractServices);
            return tester;
        }

        protected MerklePath GetMerklePath(long blockNumber, string txId,ContractServices services)
        {
            var index = 0;
            var blockInfoResult =
                AsyncHelper.RunSync(() => services.NodeManager.ApiClient.GetBlockByHeightAsync(blockNumber, true));
            var transactionIds = blockInfoResult.Body.Transactions;
            var transactionStatus = new List<string>();

            foreach (var transactionId in transactionIds)
            {
                var txResult = AsyncHelper.RunSync(() =>
                    services.NodeManager.ApiClient.GetTransactionResultAsync(transactionId));
                var resultStatus = txResult.Status.ConvertTransactionResultStatus();
                transactionStatus.Add(resultStatus.ToString());
            }

            var txIdsWithStatus = new List<Hash>();
            for (var num = 0; num < transactionIds.Count; num++)
            {
                var transactionId = HashHelper.HexStringToHash(transactionIds[num]);
                var txRes = transactionStatus[num];
                var rawBytes = transactionId.ToByteArray().Concat(EncodingHelper.GetBytesFromUtf8String(txRes))
                    .ToArray();
                var txIdWithStatus = Hash.FromRawBytes(rawBytes);
                txIdsWithStatus.Add(txIdWithStatus);
                if (!transactionIds[num].Equals(txId)) continue;
                index = num;
                _logger.Info($"The transaction index is {index}");
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
                AsyncHelper.RunSync(() => tester.NodeManager.ApiClient.GetBlockByHeightAsync(long.Parse(blockNumber), true));
            var transactionIds = blockInfoResult.Body.Transactions;
            var transactionStatus = new List<string>();

            foreach (var transactionId in transactionIds)
            {
                var txResult = AsyncHelper.RunSync(() =>
                    tester.NodeManager.ApiClient.GetTransactionResultAsync(transactionId));
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