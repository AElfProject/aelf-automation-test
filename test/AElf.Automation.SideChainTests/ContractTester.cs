using System.Linq;
using System.Threading.Tasks;
using Acs0;
using Acs3;
using Acs7;
using AElf.Client.Dto;
using AElfChain.Common;
using AElfChain.Common.Contracts;
using AElfChain.Common.Managers;
using AElf.Contracts.CrossChain;
using AElf.Contracts.MultiToken;
using AElf.Contracts.ParliamentAuth;
using AElf.Kernel;
using AElf.Sdk.CSharp;
using AElf.Types;
using AElfChain.Common.DtoExtension;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Volo.Abp.Threading;
using ApproveInput = Acs3.ApproveInput;

namespace AElf.Automation.SideChainTests
{
    public class ContractTester
    {
        public readonly ConsensusContract ConsensusService;
        public readonly ContractServices ContractServices;
        public readonly CrossChainContract CrossChainService;
        public readonly INodeManager NodeManager;
        public readonly ParliamentAuthContract ParliamentService;
        public readonly TokenContract TokenService;
        public TokenContractContainer.TokenContractStub TokenContractStub;


        public ContractTester(ContractServices contractServices)
        {
            NodeManager = contractServices.NodeManager;
            ContractServices = contractServices;

            TokenService = ContractServices.TokenService;
            ConsensusService = ContractServices.ConsensusService;
            CrossChainService = ContractServices.CrossChainService;
            ParliamentService = ContractServices.ParliamentService; 
            var tester = new ContractTesterFactory(NodeManager);
            TokenContractStub =
                tester.Create<TokenContractContainer.TokenContractStub>(TokenService.Contract,
                    ConsensusService.CallAddress);
            

        }

        #region cross chain transfer

        public string ValidateTokenAddress()
        {
            var validateTransaction = NodeManager.GenerateRawTransaction(
                ContractServices.CallAddress, ContractServices.GenesisService.ContractAddress,
                GenesisMethod.ValidateSystemContractAddress.ToString(), new ValidateSystemContractAddressInput
                {
                    Address = AddressHelper.Base58StringToAddress(TokenService.ContractAddress),
                    SystemContractHashName = Hash.FromString("AElf.ContractNames.Token")
                });
            return validateTransaction;
        }
        
        public async Task<string> ValidateTokenSymbol(string symbol)
        {
            var tokenInfo = await TokenContractStub.GetTokenInfo.CallAsync(new GetTokenInfoInput{Symbol = symbol});
            var validateTransaction = NodeManager.GenerateRawTransaction(
                ContractServices.CallAddress, ContractServices.TokenService.ContractAddress,
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

        public string ExecuteMethodWithTxId(string rawTx)
        {
            var transactionId =
                NodeManager.SendTransaction(rawTx);

            return transactionId;
        }

        #endregion

        #region side chain create method

        public Address GetOrganizationAddress(string account)
        {
            ParliamentService.SetAccount(account);
            var address =
                ParliamentService.CallViewMethod<Address>(ParliamentMethod.GetDefaultOrganizationAddress, new Empty());

            return address;
        }

        public Hash RequestSideChainCreation(string creator, string password, long indexingPrice, long lockedTokenAmount, bool isPrivilegePreserved,
            SideChainTokenInfo tokenInfo)
        {
            CrossChainService.SetAccount(creator,password);
            var result =
                CrossChainService.ExecuteMethodWithResult(CrossChainContractMethod.RequestSideChainCreation,
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
                .ProposalId;;
            return proposalId;
        }


        public TransactionResultDto RequestChainDisposal(string account, int chainId)
        {
            CrossChainService.SetAccount(account);
            var result = CrossChainService.ExecuteMethodWithResult(CrossChainContractMethod.RequestChainDisposal,
                new SInt32Value
                {
                    Value = chainId
                });

            return result;
        }

        public TransactionResultDto Recharge(string account, int chainId, long amount)
        {
//            var approve = TokenService.ExecuteMethodWithResult(TokenMethod.Approve, new AElf.Contracts.MultiToken.ApproveInput
//            {
//                Spender = CrossChainService.Contract,
//                Symbol = "ELF",
//                Amount = amount
//            });
//            approve.Status.ShouldBe("MINED");
            
            CrossChainService.SetAccount(account);
            var result =
                CrossChainService.ExecuteMethodWithResult(CrossChainContractMethod.Recharge, new RechargeInput
                {
                    ChainId = chainId,
                    Amount = amount
                });
            return result;
        }

        public SInt32Value GetChainStatus(int chainId)
        {
            var result =
                CrossChainService.CallViewMethod<SInt32Value>(CrossChainContractMethod.GetChainStatus,
                    new SInt32Value {Value = chainId});
            return result;
        }

        public ProposalOutput GetProposal(string proposalId)
        {
            var result =
                ParliamentService.CallViewMethod<ProposalOutput>(ParliamentMethod.GetProposal,
                    HashHelper.HexStringToHash(proposalId));
            return result;
        }

        #endregion

        #region cross chain verify 

        public TransactionResultDto VerifyTransaction(VerifyTransactionInput input, string account)
        {
            CrossChainService.SetAccount(account);
            var result = CrossChainService.ExecuteMethodWithResult(CrossChainContractMethod.VerifyTransaction, input);
            return result;
        }

        public CrossChainMerkleProofContext GetBoundParentChainHeightAndMerklePathByHeight(string account,
            long blockNumber)
        {
            CrossChainService.SetAccount(account);
            var result = CrossChainService.CallViewMethod<CrossChainMerkleProofContext>(
                CrossChainContractMethod.GetBoundParentChainHeightAndMerklePathByHeight, new SInt64Value
                {
                    Value = blockNumber
                });
            return result;
        }

        #endregion

        #region Parliament Method

        public TransactionResultDto Approve(string account, string proposalId)
        {
            ParliamentService.SetAccount(account);
            var result = ParliamentService.ExecuteMethodWithResult(ParliamentMethod.Approve, new ApproveInput
            {
                ProposalId = HashHelper.HexStringToHash(proposalId)
            });

            return result;
        }

        public TransactionResultDto ReleaseSideChainCreation(string account, string proposalId)
        {
            CrossChainService.SetAccount(account);
            var transactionResult =
                CrossChainService.ExecuteMethodWithResult(CrossChainContractMethod.ReleaseSideChainCreation,
                    new ReleaseSideChainCreationInput{ProposalId = HashHelper.Base64ToHash(proposalId)});
            return transactionResult;
        }

        #endregion

        #region Token Method

        //action
        public TransactionResultDto TransferToken(string owner, string spender, long amount, string symbol)
        {
            TokenService.SetAccount(owner);
            var transfer = TokenService.ExecuteMethodWithResult(TokenMethod.Transfer, new TransferInput
            {
                Symbol = symbol,
                To = AddressHelper.Base58StringToAddress(spender),
                Amount = amount,
                Memo = "Transfer Token"
            });
            return transfer;
        }

        public TransactionResultDto CreateToken(string issuer, string symbol, string tokenName)
        {
            TokenService.SetAccount(issuer);
            var create = TokenService.ExecuteMethodWithResult(TokenMethod.Create, new CreateInput
            {
                Symbol = symbol,
                Decimals = 2,
                IsBurnable = true,
                Issuer = AddressHelper.Base58StringToAddress(issuer),
                TokenName = tokenName,
                TotalSupply = 100_0000
            });
            return create;
        }

        public TransactionResultDto IssueToken(string issuer, string symbol, string toAddress)
        {
            TokenService.SetAccount(issuer);
            var issue = TokenService.ExecuteMethodWithResult(TokenMethod.Issue, new IssueInput
            {
                Symbol = symbol,
                Amount = 100_0000,
                Memo = "Issue",
                To = AddressHelper.Base58StringToAddress(toAddress)
            });

            return issue;
        }

        public TransactionResultDto TokenApprove(string owner, long amount)
        {
            TokenService.SetAccount(owner);

            var result = TokenService.ExecuteMethodWithResult(TokenMethod.Approve,
                new Contracts.MultiToken.ApproveInput
                {
                    Symbol = NodeOption.NativeTokenSymbol,
                    Spender = AddressHelper.Base58StringToAddress(CrossChainService.ContractAddress),
                    Amount = amount
                });

            return result;
        }

        public TransactionResultDto CrossChainTransfer(string fromAccount, string toAccount, int toChainId,
            long amount)
        {
            TokenService.SetAccount(fromAccount);
            var result = TokenService.ExecuteMethodWithResult(TokenMethod.CrossChainTransfer,
                new CrossChainTransferInput
                {
                    Amount = amount,
                    Memo = "transfer to side chain",
                    To = AddressHelper.Base58StringToAddress(toAccount),
                    ToChainId = toChainId
                });

            return result;
        }

        public TransactionResultDto CrossChainReceive(string account, CrossChainReceiveTokenInput input)
        {
            TokenService.SetAccount(account);
            var result = TokenService.ExecuteMethodWithResult(TokenMethod.CrossChainReceiveToken, input);
            return result;
        }

        //view
        public GetBalanceOutput GetBalance(string account, string symbol)
        {
            var balance = TokenService.CallViewMethod<GetBalanceOutput>(TokenMethod.GetBalance, new GetBalanceInput
            {
                Owner = AddressHelper.Base58StringToAddress(account),
                Symbol = symbol
            });
            return balance;
        }

        public TokenInfo GetTokenInfo(string symbol)
        {
            var tokenInfo = TokenService.CallViewMethod<TokenInfo>(TokenMethod.GetTokenInfo, new GetTokenInfoInput
            {
                Symbol = symbol
            });

            return tokenInfo;
        }

        #endregion
    }
}