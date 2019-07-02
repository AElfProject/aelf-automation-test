using System;
using Acs3;
using Acs7;
using AElf.Automation.Common.Contracts;
using AElf.Automation.Common.Helpers;
using AElf.Automation.Common.WebApi.Dto;
using AElf.Contracts.CrossChain;
using AElf.Contracts.MultiToken.Messages;
using AElf.Kernel;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.VisualStudio.TestPlatform.Common.DataCollection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ApproveInput = Acs3.ApproveInput;

namespace AElf.Automation.SideChainTests
{
    public class ContractTester
    {
        public readonly IApiHelper ApiHelper;
        public readonly ContractServices ContractServices;
        public readonly TokenContract TokenService;
        public readonly ConsensusContract ConsensusService;
        public readonly CrossChainContract CrossChainService;
        public readonly ParliamentAuthContract ParliamentService;

        public ContractTester(ContractServices contractServices)
        {
            ApiHelper = contractServices.ApiHelper;
            ContractServices = contractServices;

            TokenService = ContractServices.TokenService;
            ConsensusService = ContractServices.ConsensusService;
            CrossChainService = ContractServices.CrossChainService;
            ParliamentService = ContractServices.ParliamentService;
        }

        #region side chain create method

        public CommandInfo RequestSideChain(string account, long lockToken)
        {
            ByteString code = ByteString.FromBase64("4d5a90000300");

            CrossChainService.SetAccount(account);
            var result = CrossChainService.ExecuteMethodWithResult(CrossChainContractMethod.RequestChainCreation,
                new SideChainCreationRequest
                {
                    LockedTokenAmount = lockToken,
                    IndexingPrice = 1,
                    ContractCode = code,
                });
            return result;
        }


        public Address GetOrganizationAddress(string account)
        {
            ParliamentService.SetAccount(account);
            var address =
                ParliamentService.CallViewMethod<Address>(ParliamentMethod.GetGenesisOwnerAddress, new Empty());

            return address;
        }

        public CommandInfo CreateSideChainProposal(Address organizationAddress,string account,int indexingPrice,long lockedTokenAmount)
        {
            ByteString code = ByteString.FromBase64("4d5a90000300");
            var createProposalInput = new SideChainCreationRequest
            {
                ContractCode = code,
                IndexingPrice = indexingPrice,
                LockedTokenAmount = lockedTokenAmount
            };
            ParliamentService.SetAccount(account);
            var result =
                ParliamentService.ExecuteMethodWithResult(ParliamentMethod.CreateProposal,
                    new CreateProposalInput
                    {
                        ContractMethodName = nameof(CrossChainContractMethod.CreateSideChain),
                        ExpiredTime = TimestampHelper.GetUtcNow().AddDays(1),
                        Params = createProposalInput.ToByteString(),
                        ToAddress = Address.Parse(CrossChainService.ContractAddress),
                        OrganizationAddress = organizationAddress
                    });
            
            return result;
        }
        

        public CommandInfo RequestChainDisposal(string account,int chainId)
        {
            CrossChainService.SetAccount(account);
            var result = CrossChainService.ExecuteMethodWithResult(CrossChainContractMethod.RequestChainDisposal, new SInt32Value
            {
                Value = chainId
            });

            return result;
        }

        public CommandInfo Recharge(string account, int chainId, long amount)
        {
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
                ParliamentService.CallViewMethod<ProposalOutput>(ParliamentMethod.GetProposal, Hash.LoadHex(proposalId));
            return result;
        }

        #endregion

        #region cross chain verify 

        public CommandInfo VerifyTransaction(VerifyTransactionInput input, string account)
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

        public CommandInfo Approve(string account, string proposalId)
        {
            ParliamentService.SetAccount(account);
            var result = ParliamentService.ExecuteMethodWithResult(ParliamentMethod.Approve, new ApproveInput
            {
                ProposalId = Hash.LoadHex(proposalId)
            });

            return result;
        }

        public CommandInfo Release(string account,string proposalId)
        {
            ParliamentService.SetAccount(account);
            var transactionResult = ParliamentService.ExecuteMethodWithResult(ParliamentMethod.Release, Hash.LoadHex(proposalId));
            return transactionResult;
        }
        
        #endregion

        #region Token Method

        //action
        public CommandInfo TransferToken(string owner, string spender, long amount, string symbol)
        {
            TokenService.SetAccount(owner);
            var transfer = TokenService.ExecuteMethodWithResult(TokenMethod.Transfer, new TransferInput
            {
                Symbol = symbol,
                To = Address.Parse(spender),
                Amount = amount,
                Memo = "Transfer Token"
            });
            return transfer;
        }

        public CommandInfo CreateToken(string issuer, string symbol, string tokenName)
        {
            TokenService.SetAccount(issuer);
            var create = TokenService.ExecuteMethodWithResult(TokenMethod.Create, new CreateInput
            {
                Symbol = symbol,
                Decimals = 2,
                IsBurnable = true,
                Issuer = Address.Parse(issuer),
                TokenName = tokenName,
                TotalSupply = 100_0000
            });
            return create;
        }

        public CommandInfo IssueToken(string issuer, string symbol, string toAddress)
        {
            TokenService.SetAccount(issuer);
            var issue = TokenService.ExecuteMethodWithResult(TokenMethod.Issue, new IssueInput
            {
                Symbol = symbol,
                Amount = 100_0000,
                Memo = "Issue",
                To = Address.Parse(toAddress)
            });

            return issue;
        }

        public CommandInfo TokenApprove(string owner, long amount)
        {
            TokenService.SetAccount(owner);

            var result = TokenService.ExecuteMethodWithResult(TokenMethod.Approve,
                new Contracts.MultiToken.Messages.ApproveInput
                {
                    Symbol = "ELF",
                    Spender = Address.Parse(CrossChainService.ContractAddress),
                    Amount = amount,
                });

            return result;
        }

        public CommandInfo CrossChainTransfer(string fromAccount, string toAccount, TokenInfo tokenInfo, int toChainId,
            long amount)
        {
            TokenService.SetAccount(fromAccount);
            var result = TokenService.ExecuteMethodWithResult(TokenMethod.CrossChainTransfer,
                new CrossChainTransferInput
                {
                    Amount = amount,
                    Memo = "transfer to side chain",
                    To = Address.Parse(toAccount),
                    ToChainId = toChainId,
                    TokenInfo = tokenInfo
                });

            return result;
        }

        public CommandInfo CrossChainReceive(string account, CrossChainReceiveTokenInput input)
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
                Owner = Address.Parse(account),
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
        
        public void UnlockAllAccounts(ContractServices contractServices,string account)
        {
                var ci = new CommandInfo(ApiMethods.AccountUnlock)
                {
                    Parameter = $"{account} 123 notimeout"
                };
                ci = contractServices.ApiHelper.ExecuteCommand(ci);
                Assert.IsTrue(ci.Result);
        }
    }
}