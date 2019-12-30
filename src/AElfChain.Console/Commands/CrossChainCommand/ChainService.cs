using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using AElf;
using AElf.Contracts.Genesis;
using AElf.Contracts.MultiToken;
using AElf.CSharp.Core.Utils;
using AElf.Types;
using AElfChain.Common;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;

namespace AElfChain.Console.Commands
{
    public class ChainService
    {
        public readonly int ChainId;
        public readonly INodeManager NodeManager;
        
        public ChainService(Node node)
        {
            NodeManager = new NodeManager(node.Endpoint);
            ChainId = ChainHelper.ConvertBase58ToChainId(NodeManager.GetChainId());
            CallAddress = node.Account;
            CallAccount = CallAddress.ConvertAddress();

            NodeManager.UnlockAccount(CallAddress, node.Password);
            Authority = new AuthorityManager(NodeManager);
            GetContractServices();
        }
        public GenesisContract GenesisService { get; set; }
        public BasicContractZeroContainer.BasicContractZeroStub GenesisStub { get; set; }
        public TokenContract TokenService { get; set; }
        public ConsensusContract ConsensusService { get; set; }
        public CrossChainContract CrossChainService { get; set; }
        public ParliamentAuthContract ParliamentService { get; set; }
        public TokenContractContainer.TokenContractStub TokenStub { get; set; }
        public AuthorityManager Authority { get; set; }
        public string CallAddress { get; set; }
        public Address CallAccount { get; set; }
        public async Task<long> CheckSideChainBlockIndex(long txHeight, ChainService sideChain)
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
        private void GetContractServices()
        {
            GenesisService = GenesisContract.GetGenesisContract(NodeManager, CallAddress);
            GenesisStub = GenesisService.GetGensisStub(CallAddress);

            //TokenService contract
            var tokenAddress = GenesisService.GetContractAddressByName(NameProvider.Token);
            TokenService = new TokenContract(NodeManager, CallAddress, tokenAddress.GetFormatted());
            TokenStub = GenesisService.GetTokenStub();
            
            //Consensus contract
            var consensusAddress = GenesisService.GetContractAddressByName(NameProvider.Consensus);
            ConsensusService = new ConsensusContract(NodeManager, CallAddress, consensusAddress.GetFormatted());

            //CrossChain contract
            var crossChainAddress = GenesisService.GetContractAddressByName(NameProvider.CrossChain);
            CrossChainService = new CrossChainContract(NodeManager, CallAddress, crossChainAddress.GetFormatted());

            //ParliamentAuth contract
            var parliamentAuthAddress = GenesisService.GetContractAddressByName(NameProvider.ParliamentAuth);
            ParliamentService =
                new ParliamentAuthContract(NodeManager, CallAddress, parliamentAuthAddress.GetFormatted());
        }
    }
}