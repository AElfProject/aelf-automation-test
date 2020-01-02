using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Acs0;
using Acs3;
using Acs7;
using AElf.Client.Dto;
using AElf.Contracts.AssociationAuth;
using AElf.Contracts.CrossChain;
using AElf.Contracts.MultiToken;
using AElfChain.Common.Helpers;
using AElf.CSharp.Core.Utils;
using AElf.Types;
using AElfChain.Common;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Managers;
using Google.Protobuf;
using log4net;
using Volo.Abp.Threading;
using ApproveInput = Acs3.ApproveInput;

namespace AElf.Automation.SideChainTests
{
    public class SideChainTestBase
    {
        protected static readonly ILog _logger = Log4NetHelper.GetLogger();
        public ContractServices MainServices;
        public ContractServices SideAServices;
        public ContractServices SideBServices;

        public TokenContractContainer.TokenContractStub TokenContractStub;
        public TokenContractContainer.TokenContractStub side1TokenContractStub;
        public TokenContractContainer.TokenContractStub side2TokenContractStub;
        public string InitAccount;

        public List<string> Miners;

        protected void Initialize()
        {
            //Init Logger
            Log4NetHelper.LogInit();

            InitAccount = ConfigInfoHelper.Config.MainChainInfos.Account;
            var mainUrl = ConfigInfoHelper.Config.MainChainInfos.MainChainUrl;
            var password = ConfigInfoHelper.Config.MainChainInfos.Password;
            var sideUrls = ConfigInfoHelper.Config.SideChainInfos.Select(l => l.SideChainUrl).ToList();

            MainServices = new ContractServices(mainUrl, InitAccount, password);
            SideAServices = new ContractServices(sideUrls[0], InitAccount, password);
            SideBServices = new ContractServices(sideUrls[1], InitAccount, NodeOption.DefaultPassword);

            TokenContractStub = MainServices.TokenContractStub;
            side1TokenContractStub = SideAServices.TokenContractStub;
            side2TokenContractStub = SideBServices.TokenContractStub;
            Miners = new List<string>();
            Miners = (new AuthorityManager(SideBServices.NodeManager, InitAccount).GetCurrentMiners());
        }

        #region cross chain transfer

        protected MerklePath GetMerklePath(long blockNumber, string txId, ContractServices services, out Hash root)
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

        protected string ValidateTokenAddress(ContractServices services)
        {
            var validateTransaction = services.NodeManager.GenerateRawTransaction(
                services.CallAddress, services.GenesisService.ContractAddress,
                GenesisMethod.ValidateSystemContractAddress.ToString(), new ValidateSystemContractAddressInput
                {
                    Address = AddressHelper.Base58StringToAddress(services.TokenService.ContractAddress),
                    SystemContractHashName = Hash.FromString("AElf.ContractNames.Token")
                });
            return validateTransaction;
        }

        public async Task<string> ValidateTokenSymbol(ContractServices services, string symbol)
        {
            var tokenInfo = await TokenContractStub.GetTokenInfo.CallAsync(new GetTokenInfoInput {Symbol = symbol});
            var validateTransaction = services.NodeManager.GenerateRawTransaction(
                services.CallAddress, services.TokenService.ContractAddress,
                TokenMethod.ValidateTokenInfoExists.ToString(), new ValidateTokenInfoExistsInput
                {
                    IsBurnable = tokenInfo.IsBurnable,
                    Issuer = tokenInfo.Issuer,
                    IssueChainId = tokenInfo.IssueChainId,
                    Decimals = tokenInfo.Decimals,
                    Symbol = tokenInfo.Symbol,
                    TokenName = tokenInfo.TokenName,
                    TotalSupply = tokenInfo.TotalSupply
                });
            return validateTransaction;
        }

        #endregion

        #region Other Method

        protected string ExecuteMethodWithTxId(ContractServices services, string rawTx)
        {
            var transactionId =
                services.NodeManager.SendTransaction(rawTx);

            return transactionId;
        }

        #endregion

        #region side chain create method

        protected Hash RequestSideChainCreation(ContractServices services, string creator, string password,
            long indexingPrice, long lockedTokenAmount, bool isPrivilegePreserved,
            SideChainTokenInfo tokenInfo)
        {
            services.CrossChainService.SetAccount(creator, password);
            var result =
                services.CrossChainService.ExecuteMethodWithResult(CrossChainContractMethod.RequestSideChainCreation,
                    new SideChainCreationRequest
                    {
                        IndexingPrice = indexingPrice,
                        LockedTokenAmount = lockedTokenAmount,
                        IsPrivilegePreserved = isPrivilegePreserved,
                        SideChainTokenDecimals = tokenInfo.Decimals,
                        SideChainTokenName = tokenInfo.TokenName,
                        SideChainTokenSymbol = tokenInfo.Symbol,
                        SideChainTokenTotalSupply = tokenInfo.TotalSupply,
                        IsSideChainTokenBurnable = tokenInfo.IsBurnable
                    });
            var byteString = result.Logs.First(l => l.Name.Contains(nameof(ProposalCreated))).NonIndexed;
            var proposalId = ProposalCreated.Parser
                .ParseFrom(ByteString.FromBase64(byteString))
                .ProposalId;
            ;
            return proposalId;
        }

        protected TransactionResultDto RequestChainDisposal(ContractServices services, string account, int chainId)
        {
            services.CrossChainService.SetAccount(account);
            var result = services.CrossChainService.ExecuteMethodWithResult(
                CrossChainContractMethod.RequestChainDisposal,
                new SInt32Value
                {
                    Value = chainId
                });

            return result;
        }

        protected TransactionResultDto Recharge(ContractServices services, string account, int chainId, long amount)
        {
//            var approve = TokenService.ExecuteMethodWithResult(TokenMethod.Approve, new AElf.Contracts.MultiToken.ApproveInput
//            {
//                Spender = CrossChainService.Contract,
//                Symbol = "ELF",
//                Amount = amount
//            });
//            approve.Status.ShouldBe("MINED");

            services.CrossChainService.SetAccount(account);
            var result =
                services.CrossChainService.ExecuteMethodWithResult(CrossChainContractMethod.Recharge, new RechargeInput
                {
                    ChainId = chainId,
                    Amount = amount
                });
            return result;
        }

        public SInt32Value GetChainStatus(ContractServices services, int chainId)
        {
            var result =
                services.CrossChainService.CallViewMethod<SInt32Value>(CrossChainContractMethod.GetChainStatus,
                    new SInt32Value {Value = chainId});
            return result;
        }

        public ProposalOutput GetProposal(ContractServices services, string proposalId)
        {
            var result =
                services.ParliamentService.CallViewMethod<ProposalOutput>(ParliamentMethod.GetProposal,
                    HashHelper.HexStringToHash(proposalId));
            return result;
        }

        #endregion

        #region cross chain verify 

        protected CrossChainMerkleProofContext GetBoundParentChainHeightAndMerklePathByHeight(ContractServices services,
            string account,
            long blockNumber)
        {
            services.CrossChainService.SetAccount(account);
            var result = services.CrossChainService.CallViewMethod<CrossChainMerkleProofContext>(
                CrossChainContractMethod.GetBoundParentChainHeightAndMerklePathByHeight, new SInt64Value
                {
                    Value = blockNumber
                });
            return result;
        }

        #endregion

        #region Parliament Method

        protected TransactionResultDto Approve(ContractServices services, string account, string proposalId)
        {
            services.ParliamentService.SetAccount(account);
            var result = services.ParliamentService.ExecuteMethodWithResult(ParliamentMethod.Approve, new ApproveInput
            {
                ProposalId = HashHelper.HexStringToHash(proposalId)
            });

            return result;
        }

        protected TransactionResultDto ReleaseSideChainCreation(ContractServices services, string account,
            string proposalId)
        {
            services.CrossChainService.SetAccount(account);
            var transactionResult =
                services.CrossChainService.ExecuteMethodWithResult(CrossChainContractMethod.ReleaseSideChainCreation,
                    new ReleaseSideChainCreationInput {ProposalId = HashHelper.Base64ToHash(proposalId)});
            return transactionResult;
        }

        #endregion

        #region Token Method

        //action
        protected TransactionResultDto TransferToken(ContractServices services, string owner, string spender,
            long amount, string symbol)
        {
            var transfer = services.TokenService.ExecuteMethodWithResult(TokenMethod.Transfer, new TransferInput
            {
                Symbol = symbol,
                To = AddressHelper.Base58StringToAddress(spender),
                Amount = amount,
                Memo = "Transfer Token"
            });
            return transfer;
        }

        public TransactionResultDto IssueToken(ContractServices services, string issuer, string symbol,
            string toAddress)
        {
            services.TokenService.SetAccount(issuer);
            var issue = services.TokenService.ExecuteMethodWithResult(TokenMethod.Issue, new IssueInput
            {
                Symbol = symbol,
                Amount = 100_0000,
                Memo = "Issue",
                To = AddressHelper.Base58StringToAddress(toAddress)
            });

            return issue;
        }

        protected TransactionResultDto TokenApprove(ContractServices services, string owner, long amount)
        {
            services.TokenService.SetAccount(owner);

            var result = services.TokenService.ExecuteMethodWithResult(TokenMethod.Approve,
                new Contracts.MultiToken.ApproveInput
                {
                    Symbol = NodeOption.NativeTokenSymbol,
                    Spender = AddressHelper.Base58StringToAddress(services.CrossChainService.ContractAddress),
                    Amount = amount
                });

            return result;
        }

        protected TransactionResultDto CrossChainReceive(ContractServices services, string account,
            CrossChainReceiveTokenInput input)
        {
            services.TokenService.SetAccount(account);
            var result = services.TokenService.ExecuteMethodWithResult(TokenMethod.CrossChainReceiveToken, input);
            return result;
        }

        //view
        protected GetBalanceOutput GetBalance(ContractServices services, string account, string symbol)
        {
            var balance = services.TokenService.CallViewMethod<GetBalanceOutput>(TokenMethod.GetBalance,
                new GetBalanceInput
                {
                    Owner = AddressHelper.Base58StringToAddress(account),
                    Symbol = symbol
                });
            return balance;
        }

        protected string GetPrimaryTokenSymbol(ContractServices services)
        {
            var symbol = services.TokenService.GetPrimaryTokenSymbol();
            return symbol;
        }

        #endregion
    }
}