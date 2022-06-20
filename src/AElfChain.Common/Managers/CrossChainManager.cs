using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AElf;
using AElf.Client.Dto;
using AElf.Contracts.MultiToken;
using AElf.Standards.ACS0;
using AElf.Types;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using Google.Protobuf;
using log4net;
using Shouldly;
using Volo.Abp.Threading;
using TokenContract = AElfChain.Common.Contracts.TokenContract;

namespace AElfChain.Common.Managers
{
    public class CrossChainManager
    {
        private static readonly ILog Logger = Log4NetHelper.GetLogger();
        private readonly GenesisContract _fromChainGenesis;
        private readonly GenesisContract _toChainGenesis;

        private readonly CrossChainContract _fromChainCrossChain;
        private readonly CrossChainContract _toChainCrossChain;
        private readonly TokenContract _fromChainToken;
        private readonly TokenContract _toChainToken;

        private NodesInfo _info;

        public CrossChainManager(INodeManager fromNoeNodeManager, INodeManager toChainNodeManager, string caller = "")
        {
            FromNoeNodeManager = fromNoeNodeManager;
            ToChainNodeManager = toChainNodeManager;
            _fromChainGenesis = FromNoeNodeManager.GetGenesisContract(caller);
            _fromChainToken = _fromChainGenesis.GetTokenContract(caller);
            _toChainGenesis = ToChainNodeManager.GetGenesisContract(caller);
            _toChainToken = _toChainGenesis.GetTokenContract(caller);
            _fromChainCrossChain = _fromChainGenesis.GetCrossChainContract(caller);
            _toChainCrossChain = _toChainGenesis.GetCrossChainContract(caller);
        }

        public INodeManager FromNoeNodeManager { get; set; }
        public INodeManager ToChainNodeManager { get; set; }

        public MerklePath GetMerklePath(long blockNumber, string txId, out Hash root)
        {
            var index = 0;
            var blockInfoResult =
                AsyncHelper.RunSync(() => FromNoeNodeManager.ApiClient.GetBlockByHeightAsync(blockNumber, true));
            var transactionIds = blockInfoResult.Body.Transactions;
            var transactionStatus = new List<string>();

            foreach (var transactionId in transactionIds)
            {
                var txResult = AsyncHelper.RunSync(() =>
                    FromNoeNodeManager.ApiClient.GetTransactionResultAsync(transactionId));
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

        public MerklePath GetMerklePath(INodeManager nodeManager, string transactionId)
        {
            var result =
                AsyncHelper.RunSync(() => nodeManager.ApiClient.GetMerklePathByTransactionIdAsync(transactionId));

            return new MerklePath
            {
                MerklePathNodes =
                {
                    result.MerklePathNodes.Select(o => new MerklePathNode
                    {
                        Hash = Hash.LoadFromHex(o.Hash),
                        IsLeftChildNode = o.IsLeftChildNode
                    })
                }
            };
        }

        public TransactionResultDto ValidateTokenSymbol(string symbol, out string raw)
        {
            var tokenInfo = FromNoeNodeManager.GetTokenInfo(symbol);
            var validateTransaction = FromNoeNodeManager.GenerateRawTransaction(_fromChainGenesis.CallAddress
                , _fromChainToken.ContractAddress,
                TokenMethod.ValidateTokenInfoExists.ToString(), new ValidateTokenInfoExistsInput
                {
                    Decimals = tokenInfo.Decimals,
                    Issuer = tokenInfo.Issuer,
                    IsBurnable = tokenInfo.IsBurnable,
                    IssueChainId = tokenInfo.IssueChainId,
                    Symbol = tokenInfo.Symbol,
                    TokenName = tokenInfo.TokenName,
                    TotalSupply = tokenInfo.TotalSupply
                });
            raw = validateTransaction;
            var txId = FromNoeNodeManager.SendTransaction(validateTransaction);
            var result = FromNoeNodeManager.CheckTransactionResult(txId);
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            return result;
        }

        public TransactionResultDto CrossChainTransfer(string symbol, long amount, string toAccount, string account, out string raw)
        {
            var chainId = ChainHelper.ConvertBase58ToChainId(FromNoeNodeManager.GetChainId());
            var validationChainId = ChainHelper.ConvertBase58ToChainId(ToChainNodeManager.GetChainId());
            var crossChainTransferInput = new CrossChainTransferInput
            {
                Symbol = symbol,
                IssueChainId = chainId,
                Amount = amount,
                Memo = "cross chain transfer",
                To = toAccount.ConvertAddress(),
                ToChainId = validationChainId
            };
            // execute cross chain transfer
            var rawTx = FromNoeNodeManager.GenerateRawTransaction(account,
                _fromChainToken.ContractAddress, TokenMethod.CrossChainTransfer.ToString(),
                crossChainTransferInput);
            raw = rawTx;
            var txId = FromNoeNodeManager.SendTransaction(rawTx);
            var result = FromNoeNodeManager.CheckTransactionResult(txId);
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            return result;
        }
        
        public CrossChainReceiveTokenInput ReceiveFromMainChainInput(long height, string txId,
            string raw)
        {
            var fromChainId = FromNoeNodeManager.GetChainId();
            var toChainId = ToChainNodeManager.GetChainId();

            var merklePath = GetMerklePath(FromNoeNodeManager, txId);
            if (merklePath == null)
                throw new Exception("Can't get the merkle path.");
            CrossChainReceiveTokenInput crossChainReceiveToken;
                
            if (!fromChainId.Equals("AELF"))
            {
                var crossChainMerkleProofContext =
                    _fromChainCrossChain.GetCrossChainMerkleProofContext(height);
                crossChainReceiveToken = new CrossChainReceiveTokenInput
                {
                    FromChainId = ChainHelper.ConvertBase58ToChainId(fromChainId),
                    ParentChainHeight = crossChainMerkleProofContext.BoundParentChainHeight,
                    TransferTransactionBytes = ByteString.CopyFrom(ByteArrayHelper.HexStringToByteArray(raw)),
                    MerklePath = merklePath
                };
                crossChainReceiveToken.MerklePath.MerklePathNodes.AddRange(crossChainMerkleProofContext.MerklePathFromParentChain
                    .MerklePathNodes);
            }
            else
            {
                crossChainReceiveToken = new CrossChainReceiveTokenInput
                {
                    FromChainId = ChainHelper.ConvertBase58ToChainId(fromChainId),
                    ParentChainHeight = height,
                    TransferTransactionBytes = ByteString.CopyFrom(ByteArrayHelper.HexStringToByteArray(raw)),
                    MerklePath = merklePath
                };
            }
            return crossChainReceiveToken;
        } 

        public RegisterCrossChainTokenContractAddressInput RegisterTokenAddressInput(long height, string txId,
            string raw)
        {
            var fromChainId = FromNoeNodeManager.GetChainId();
            var toChainId = ToChainNodeManager.GetChainId();

            var merklePath = GetMerklePath(FromNoeNodeManager, txId);
            if (merklePath == null)
                throw new Exception("Can't get the merkle path.");

            RegisterCrossChainTokenContractAddressInput registerInput;
            if (!fromChainId.Equals("AELF"))
            {
                var crossChainMerkleProofContext =
                    _fromChainCrossChain.GetCrossChainMerkleProofContext(height);
                registerInput = new RegisterCrossChainTokenContractAddressInput
                {
                    FromChainId = ChainHelper.ConvertBase58ToChainId(fromChainId),
                    ParentChainHeight = crossChainMerkleProofContext.BoundParentChainHeight,
                    TokenContractAddress = _toChainToken.Contract,
                    TransactionBytes = ByteString.CopyFrom(ByteArrayHelper.HexStringToByteArray(raw)),
                    MerklePath = merklePath
                };
                registerInput.MerklePath.MerklePathNodes.AddRange(crossChainMerkleProofContext.MerklePathFromParentChain
                    .MerklePathNodes);
            }
            else
            {
                registerInput = new RegisterCrossChainTokenContractAddressInput
                {
                    FromChainId = ChainHelper.ConvertBase58ToChainId(fromChainId),
                    ParentChainHeight = height,
                    TokenContractAddress = _toChainToken.Contract,
                    TransactionBytes = ByteString.CopyFrom(ByteArrayHelper.HexStringToByteArray(raw)),
                    MerklePath = merklePath
                };
            }

            return registerInput;
        }

        public TransactionResultDto ValidateTokenAddress(string account,out string raw)
        {
            var validateTransaction = FromNoeNodeManager.GenerateRawTransaction(
                account, _fromChainGenesis.ContractAddress,
                GenesisMethod.ValidateSystemContractAddress.ToString(), new ValidateSystemContractAddressInput
                {
                    Address = _fromChainToken.Contract,
                    SystemContractHashName = HashHelper.ComputeFrom("AElf.ContractNames.Token")
                });
            raw = validateTransaction;
            var txId = FromNoeNodeManager.SendTransaction(validateTransaction);
            var result = FromNoeNodeManager.CheckTransactionResult(txId);
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            return result;
        }

        public bool CheckTokenAddress()
        {
            var chainId = ChainHelper.ConvertBase58ToChainId(ToChainNodeManager.GetChainId());
            var validationAddress = _fromChainToken.CallViewMethod<Address>(
                TokenMethod.GetCrossChainTransferTokenContractAddress,
                new GetCrossChainTransferTokenContractAddressInput
                {
                    ChainId = chainId
                });
            return validationAddress.Equals(_toChainToken.Contract);
        }

        public bool CheckPrivilegePreserved()
        {
            var chainId = ChainHelper.ConvertBase58ToChainId(ToChainNodeManager.GetChainId());
            var sideChainData = _fromChainCrossChain.GetChainInitializationData(chainId);
            return sideChainData.ChainCreatorPrivilegePreserved;
        }

        public long CheckMainChainIndexSideChain(long txHeight, CrossChainContract mainChainCross = null)
        {
            Logger.Info($"Wait main chain index side chain target height: {txHeight}");

            if (mainChainCross == null) mainChainCross = _fromChainCrossChain;
            var mainHeight = long.MaxValue;
            var checkResult = false;
            var sideChainId = ChainHelper.ConvertBase58ToChainId(ToChainNodeManager.GetChainId());
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
                    ? AsyncHelper.RunSync(() => FromNoeNodeManager.ApiClient.GetBlockHeightAsync())
                    : mainHeight;
                var indexParentBlock = _toChainCrossChain.GetParentChainHeight();
                checkResult = indexParentBlock > mainHeight;
            }

            return mainHeight;
        }

        public void CheckSideChainIndexMainChain(long txHeight)
        {
            Logger.Info($"Wait side chain index main chain target height: {txHeight}");

            while (txHeight > _toChainCrossChain.GetParentChainHeight())
            {
                Logger.Info("Block is not recorded ");
                AsyncHelper.RunSync(() => Task.Delay(10000));
            }
        }
    }
}