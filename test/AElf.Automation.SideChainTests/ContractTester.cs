using AElf.Automation.Common.Contracts;
using AElf.Automation.Common.Helpers;
using AElf.Contracts.CrossChain;
using AElf.Contracts.MultiToken.Messages;
using AElf.CrossChain;
using AElf.Kernel;
using Google.Protobuf;
using ApproveInput = Acs3.ApproveInput;

namespace AElf.Automation.SideChainTests
{
    public class ContractTester
    {
        public readonly RpcApiHelper ApiHelper;
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

        public CommandInfo RequestSideChain(string account)
        {
            ByteString code = ByteString.FromBase64("4d5a90000300");

            var resourceBalance = new ResourceTypeBalancePair
            {
                Amount = 1,
                Type = Kernel.ResourceType.Ram
            };
            
            CrossChainService.SetAccount(account);
            var result =CrossChainService.ExecuteMethodWithResult(CrossChainContractMethod.RequestChainCreation,
                new SideChainCreationRequest
                {
                    LockedTokenAmount = 100_000,
                    IndexingPrice = 1,
                    ContractCode = code,
                    ResourceBalances = { resourceBalance}
                });
            return result;
        }
        

        public CommandInfo Recharge(int chainId, long amount)
        {
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
                CrossChainService.CallViewMethod<SInt32Value>(CrossChainContractMethod.GetChainStatus, new SInt32Value{Value = chainId});
            return result;
        }
        
        #endregion

        #region cross chain verify 
        
        public CommandInfo VerifyTransaction(VerifyTransactionInput input,string account)
        {
            CrossChainService.SetAccount(account);
            var result = CrossChainService.ExecuteMethodWithResult(CrossChainContractMethod.VerifyTransaction, input);
            return result;
        }

        public CommandInfo GetBoundParentChainHeightAndMerklePathByHeight(string account,long blockNumber)
        {
            CrossChainService.SetAccount(account);
            var result = CrossChainService.ExecuteMethodWithResult(
                CrossChainContractMethod.GetBoundParentChainHeightAndMerklePathByHeight, new SInt64Value
                {
                    Value = blockNumber
                });
            return result;
        }


        #endregion
        
        
        #region Parliament Method

        public CommandInfo Approve(string account,string proposalId)
        {
            ParliamentService.SetAccount(account);
            var result = ParliamentService.ExecuteMethodWithResult(ParliamentMethod.Approve, new ApproveInput
            {
                 ProposalId= Hash.LoadHex(proposalId)
            });

            return result;
        }

        #endregion

        #region Token Method
        
        //action
        public CommandInfo TransferToken(string owner,string spender,long amount,string symbol)
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

        public CommandInfo CreateToken(string issuer,string symbol,string tokenName)
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

        //view
        public GetBalanceOutput GetBalance(string account,string symbol)
        {
            var balance = TokenService.CallViewMethod<GetBalanceOutput>(TokenMethod.GetBalance, new GetBalanceInput
            {
                Owner = Address.Parse(account),
                Symbol = symbol
            });
            return balance;
        }
        

        #endregion
    }
}