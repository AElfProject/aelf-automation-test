using System.Collections.Generic;
using System.Linq;
using AElf.Contracts.MultiToken;
using AElfChain.Common.Helpers;
using AElf.CSharp.Core.Utils;
using AElf.Types;
using AElfChain.Common;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Managers;
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
        public ContractTester SideContractTester3;
        public ContractTester SideContractTester4;
        public ContractTester SideContractTester5;
        public ContractTester SideContractTester11;
        
        public ContractServices sideAServices;
        public ContractServices sideBServices;
        public ContractServices sideCServices;
        public ContractServices sideDServices;
        public ContractServices sideEServices;
        
        public ContractServices sideSideAServices;
        public TokenContractContainer.TokenContractStub TokenContractStub;
        public TokenContractContainer.TokenContractStub side1TokenContractStub;
        public TokenContractContainer.TokenContractStub side2TokenContractStub;
        
        public List<string> Miners;
        
        public static string MainChainUrl { get; } = "http://52.90.147.175:8000";
        public static string SideAChainUrl { get; } = "http://54.84.43.173:8000";
//        public static string SideSideAChainUrl { get; } = "http://192.168.197.56:8111";
        public static string SideBChainUrl { get; } = "http://54.234.110.152:8000";
//        public static string SideCChainUrl { get; } = "http://54.146.209.204:8000";
//        public static string SideDChainUrl { get; } = "http://52.201.34.95:8000";
//        public static string SideEChainUrl { get; } = "http://3.95.195.143:8000";

        public string InitAccount { get; } = "2RCLmZQ2291xDwSbDEJR6nLhFJcMkyfrVTq1i1YxWC4SdY49a6";

        protected void Initialize()
        {
            //Init Logger
            Log4NetHelper.LogInit();
            var mainServices = new ContractServices(MainChainUrl, InitAccount, NodeOption.DefaultPassword, "AELF");
            MainContracts = new ContractTester(mainServices);

             sideAServices = new ContractServices(SideAChainUrl, InitAccount, NodeOption.DefaultPassword, "tDVV");
//             sideSideAServices = new ContractServices(SideSideAChainUrl, InitAccount, NodeOption.DefaultPassword, "AZpC");
            sideBServices = new ContractServices(SideBChainUrl, InitAccount, NodeOption.DefaultPassword, "tDVW");
//            sideCServices = new ContractServices(SideCChainUrl, InitAccount, NodeOption.DefaultPassword, "tDVX");
//            sideDServices = new ContractServices(SideDChainUrl, InitAccount, NodeOption.DefaultPassword, "tDVY");
//            sideEServices = new ContractServices(SideEChainUrl, InitAccount, NodeOption.DefaultPassword, "tDVZ");
             
             MainContracts = new ContractTester(mainServices);
             SideContractTester1 = new ContractTester(sideAServices);
//             SideContractTester11 = new ContractTester(sideSideAServices);
             SideContractTester2 = new ContractTester(sideBServices);
//             SideContractTester3 = new ContractTester(sideCServices);
//             SideContractTester4 = new ContractTester(sideDServices);
//             SideContractTester5 = new ContractTester(sideEServices);

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

        protected MerklePath GetMerklePath(long blockNumber, string txId, ContractServices services,out Hash root)
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
            root = bmt.Root;
            var merklePath = new MerklePath();
            merklePath.MerklePathNodes.AddRange(bmt.GenerateMerklePath(index).MerklePathNodes);
            return merklePath;
        }
    }
}