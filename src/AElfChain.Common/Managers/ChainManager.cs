using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AElf;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.CrossChain;
using AElf.Contracts.Genesis;
using AElf.Contracts.MultiToken;
using AElf.Contracts.ParliamentAuth;
using AElf.CSharp.Core.Utils;
using AElf.Types;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using log4net;

namespace AElfChain.Common.Managers
{
    public class ChainManager
    {
        public ILog Logger = Log4NetHelper.GetLogger();
        public int ChainId { get; set; }
        public string ChainIdName { get; set; }
        public INodeManager NodeManager { get; set; }
        public string CallAddress { get; set; }
        public Address CallAccount { get; set; }
        
        public AuthorityManager Authority { get; set; }

        public ChainManager(INodeManager nodeManager, string callAddress)
        {
            NodeManager = nodeManager;
            CallAddress = callAddress;
            CallAccount = callAddress.ConvertAddress();
            ChainIdName = NodeManager.GetChainId();
            ChainId = ChainHelper.ConvertBase58ToChainId(ChainIdName);
            GenesisService = GenesisContract.GetGenesisContract(NodeManager, CallAddress);
            GenesisStub = GenesisService.GetGensisStub(CallAddress);
        }

        public ChainManager(string endpoint, string callAddress)
        {
            NodeManager = new NodeManager(endpoint);
            CallAddress = callAddress;
            CallAccount = callAddress.ConvertAddress();
            ChainIdName = NodeManager.GetChainId();
            ChainId = ChainHelper.ConvertBase58ToChainId(ChainIdName);
            GenesisService = GenesisContract.GetGenesisContract(NodeManager, CallAddress);
            GenesisStub = GenesisService.GetGensisStub(CallAddress);
        }
        
        #region Contracts services and stub
        public GenesisContract GenesisService { get; set; }
        public BasicContractZeroContainer.BasicContractZeroStub GenesisStub { get; set; }
        public TokenContract TokenService => GenesisService.GetTokenContract();
        public TokenContractContainer.TokenContractStub TokenStub => GenesisService.GetTokenStub();
        public ConsensusContract ConsensusService => GenesisService.GetConsensusContract();

        public AEDPoSContractContainer.AEDPoSContractStub ConsensusStub => GenesisService.GetConsensusStub();
        public CrossChainContract CrossChainService => GenesisService.GetCrossChainContract();

        public CrossChainContractContainer.CrossChainContractStub CrossChainStub => GenesisService.GetCrossChainStub();
        public ParliamentAuthContract ParliamentService => GenesisService.GetParliamentAuthContract();

        public ParliamentAuthContractContainer.ParliamentAuthContractStub ParliamentStub =>
            GenesisService.GetParliamentAuthStub();
        #endregion
        
        public async Task<long> CheckSideChainBlockIndex(long txHeight, ChainManager sideChain)
        {
            var stopwatch = Stopwatch.StartNew();
            while (true)
            {
                var indexSideHeight = CrossChainService.GetSideChainHeight(sideChain.ChainId);
                if (indexSideHeight < txHeight)
                {
                    System.Console.Write($"\r[Main->Side]Current index height: {indexSideHeight}, target index height: {txHeight}. Time using: {CommonHelper.ConvertMileSeconds(stopwatch.ElapsedMilliseconds)}"); 
                    await Task.Delay(2000);
                    continue;
                }
                System.Console.Write($"\r[Main->Side]Current index height: {indexSideHeight}, target index height: {txHeight}. Time using: {CommonHelper.ConvertMileSeconds(stopwatch.ElapsedMilliseconds)}"); 
                System.Console.WriteLine();
                stopwatch.Stop();
                var mainHeight = await NodeManager.ApiClient.GetBlockHeightAsync();
                return mainHeight;
            }
        }
        
        public async Task CheckParentChainBlockIndex(long blockHeight)
        {
            var stopwatch = Stopwatch.StartNew();
            while (true)
            {
                var indexHeight = CrossChainService.GetParentChainHeight();
                if (blockHeight > indexHeight)
                {
                    System.Console.Write($"\r[Side->Main]Current index height: {indexHeight}, target index height: {blockHeight}. Time using: {CommonHelper.ConvertMileSeconds(stopwatch.ElapsedMilliseconds)}");
                    await Task.Delay(2000);
                    continue;
                }
                System.Console.Write($"\r[Side->Main]Current index height: {indexHeight}, target index height: {blockHeight}. Time using: {CommonHelper.ConvertMileSeconds(stopwatch.ElapsedMilliseconds)}");
                System.Console.WriteLine();
                stopwatch.Stop();
                break;
            }
        }
        
        public async Task<MerklePath> GetMerklePath(long blockNumber, string txId)
        {
            var index = 0;
            var blockInfoResult =
                await NodeManager.ApiClient.GetBlockByHeightAsync(blockNumber, true);
            var transactionIds = blockInfoResult.Body.Transactions;
            var transactionStatus = new List<string>();

            foreach (var transactionId in transactionIds)
            {
                var txResult = await NodeManager.ApiClient.GetTransactionResultAsync(transactionId);
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
            }

            var bmt = BinaryMerkleTree.FromLeafNodes(txIdsWithStatus);
            var merklePath = new MerklePath();
            merklePath.MerklePathNodes.AddRange(bmt.GenerateMerklePath(index).MerklePathNodes);
            
            return merklePath;
        }

        public void AuthorityManager(string configFile)
        {
            NodeInfoHelper.SetConfig(configFile);
            Authority = new AuthorityManager(NodeManager, CallAddress);
        }
    }
}