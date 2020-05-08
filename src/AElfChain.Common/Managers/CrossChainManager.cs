using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AElf;
using AElf.Contracts.MultiToken;
using AElf.Types;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using log4net;
using Volo.Abp.Threading;

namespace AElfChain.Common.Managers
{
    public class CrossChainManager
    {
        private static readonly ILog Logger = Log4NetHelper.GetLogger();
        private readonly GenesisContract _genesis;
        private readonly GenesisContract _validationGenesis;

        private readonly CrossChainContract _crossChain;
        private readonly CrossChainContract _validateCrossChain;
        private readonly TokenContract _token;

        private NodesInfo _info;

        public CrossChainManager(INodeManager nodeManager, INodeManager validationNodeManager,string caller = "")
        {
            
            NodeManager = nodeManager;
            ValidationNodeManager = validationNodeManager;
            _genesis = NodeManager.GetGenesisContract(caller);
            _token = _genesis.GetTokenContract(caller);
            _validationGenesis = ValidationNodeManager.GetGenesisContract(caller);
            _crossChain = _genesis.GetCrossChainContract(caller);
            _validateCrossChain = _validationGenesis.GetCrossChainContract(caller);
        }
        
        public INodeManager NodeManager { get; set; }
        public INodeManager ValidationNodeManager { get; set; }


        public MerklePath GetMerklePath(long blockNumber, string txId, out Hash root)
        {
            var index = 0;
            var blockInfoResult =
                AsyncHelper.RunSync(() => NodeManager.ApiClient.GetBlockByHeightAsync(blockNumber, true));
            var transactionIds = blockInfoResult.Body.Transactions;
            var transactionStatus = new List<string>();

            foreach (var transactionId in transactionIds)
            {
                var txResult = AsyncHelper.RunSync(() =>
                    NodeManager.ApiClient.GetTransactionResultAsync(transactionId));
                var resultStatus = txResult.Status.ConvertTransactionResultStatus();
                transactionStatus.Add(resultStatus.ToString());
            }

            var txIdsWithStatus = new List<Hash>();
            for (var num = 0; num < transactionIds.Count; num++)
            {
                var transactionId = Hash.LoadFromHex(transactionIds[num]);
                var txRes = transactionStatus[num];
                var rawBytes = transactionId.ToByteArray().Concat(Encoding.UTF8.GetBytes(txRes))
                    .ToArray();
                var txIdWithStatus = HashHelper.ComputeFrom(rawBytes);
                txIdsWithStatus.Add(txIdWithStatus);
                if (!transactionIds[num].Equals(txId)) continue;
                index = num;
                Logger.Info($"The transaction index is {index}");
            }

            var bmt = BinaryMerkleTree.FromLeafNodes(txIdsWithStatus);
            root = bmt.Root;
            var merklePath = new MerklePath();
            merklePath.MerklePathNodes.AddRange(bmt.GenerateMerklePath(index).MerklePathNodes);
            return merklePath;
        }

        public string ValidateTokenSymbol(string symbol)
        {
            var tokenInfo = NodeManager.GetTokenInfo(symbol);
            var validateTransaction = NodeManager.GenerateRawTransaction(_genesis.CallAddress
                , _token.ContractAddress,
                TokenMethod.ValidateTokenInfoExists.ToString(), new ValidateTokenInfoExistsInput
                {
                    Decimals = tokenInfo.Decimals,
                    Issuer = tokenInfo.Issuer,
                    IsBurnable = tokenInfo.IsBurnable,
                    IssueChainId = tokenInfo.IssueChainId,
                    IsProfitable = tokenInfo.IsProfitable,
                    Symbol = tokenInfo.Symbol,
                    TokenName = tokenInfo.TokenName,
                    TotalSupply = tokenInfo.TotalSupply
                });
            return validateTransaction;
        }

        public string CrossChainTransfer(string symbol,long amount,string account)
        {
            var chainId =  ChainHelper.ConvertBase58ToChainId(NodeManager.GetChainId());
            var validationChainId = ChainHelper.ConvertBase58ToChainId(ValidationNodeManager.GetChainId());
            var crossChainTransferInput = new CrossChainTransferInput
            {
                Symbol = symbol,
                IssueChainId = chainId,
                Amount = amount,
                Memo = "cross chain transfer",
                To = account.ConvertAddress(),
                ToChainId = validationChainId
            };
            // execute cross chain transfer
            var rawTx = NodeManager.GenerateRawTransaction(account,
                _token.ContractAddress, TokenMethod.CrossChainTransfer.ToString(),
                crossChainTransferInput);
            return rawTx;
        }

        public bool CheckPrivilegePreserved(int chainId)
        {
           var sideChainData = _crossChain.GetChainInitializationData(chainId);
           return sideChainData.ChainCreatorPrivilegePreserved;
        }

        public long MainChainCheckSideChainBlockIndex(long txHeight,CrossChainContract mainChainCross=null)
        {
            if (mainChainCross == null)  mainChainCross = _crossChain;
            var mainHeight = long.MaxValue;
            var checkResult = false;
            var sideChainId = ChainHelper.ConvertBase58ToChainId(ValidationNodeManager.GetChainId());
            while (!checkResult)
            {
                var indexSideChainBlock = mainChainCross.GetSideChainHeight(sideChainId);

                if (indexSideChainBlock < txHeight)
                {
                    Logger.Info("Block is not recorded ");
                    AsyncHelper.RunSync(() => Task.Delay(10000));
                    continue;
                }

                mainHeight = mainHeight == long.MaxValue
                    ? AsyncHelper.RunSync(()=> NodeManager.ApiClient.GetBlockHeightAsync()) 
                    : mainHeight;
                var indexParentBlock = _validateCrossChain.GetParentChainHeight();
                checkResult = indexParentBlock > mainHeight;
            }

            return mainHeight;
        }
        
        public void CheckSideChainBlockIndex(long txHeight)
        {
            while (txHeight > _validateCrossChain.GetParentChainHeight())
            {
                Logger.Info("Block is not recorded ");
                AsyncHelper.RunSync(() => Task.Delay(10000));
            }
        }

    }
}