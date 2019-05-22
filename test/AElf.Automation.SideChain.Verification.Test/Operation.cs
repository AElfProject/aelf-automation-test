using AElf.Automation.Common.Contracts;
using AElf.Automation.Common.Helpers;
using AElf.Automation.Common.WebApi.Dto;
using AElf.Automation.SideChain.VerificationTest;
using AElf.Contracts.CrossChain;
using AElf.Contracts.MultiToken.Messages;
using AElf.Types;
using Google.Protobuf;
using CrossChainMerkleProofContext = AElf.Contracts.CrossChain.CrossChainMerkleProofContext;

namespace AElf.Automation.SideChain.Verification.Test
{
    public class Operation
    {
        public readonly IApiHelper ApiHelper;
        public readonly string chainId;
        public readonly ContractServices ContractServices;
     
        public readonly TokenContract TokenService;
        public readonly ConsensusContract ConsensusService;
        public readonly CrossChainContract CrossChainService;
        public readonly ParliamentAuthContract ParliamentService;

        public Operation(ContractServices contractServices,string type)
        {
            ApiHelper = contractServices.ApiHelper;
            var ci = new CommandInfo(ApiMethods.GetChainInformation);
            var result = ApiHelper.ExecuteCommand(ci);
            var returnResult = result.InfoMsg as ChainStatusDto;
            chainId = returnResult.ChainId;
            ContractServices = contractServices;

            TokenService = ContractServices.TokenService;
            CrossChainService = ContractServices.CrossChainService;
            if (type.Equals("Main"))
            {
                ParliamentService = ContractServices.ParliamentService;
            }
        }

        #region Token Method

        public GetBalanceOutput GetBalance(string account,string symbol)
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

        #endregion

        #region Cross Chain Method

        public CommandInfo CrossChainTransfer(string fromAccount,CrossChainTransferInput input)
        {
            TokenService.SetAccount(fromAccount);
            var result = TokenService.ExecuteMethodWithResult(TokenMethod.CrossChainTransfer, input);

            return result;
        }
       

        public CommandInfo CrossChainReceive(string account,CrossChainReceiveTokenInput input)
        {
            TokenService.SetAccount(account);
            var result = TokenService.ExecuteMethodWithResult(TokenMethod.CrossChainReceiveToken, input);
            return result;
        }

        public CommandInfo VerifyTransaction(VerifyTransactionInput input,string account)
        {
            CrossChainService.SetAccount(account);
            var result = CrossChainService.ExecuteMethodWithResult(CrossChainContractMethod.VerifyTransaction, input);
            return result;
        }

        public CrossChainMerkleProofContext GetBoundParentChainHeightAndMerklePathByHeight(string account,long blockNumber)
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
        
        public string GenerateBroadcastRawTx(string method, IMessage inputParameter,string CallAddress,string ContractAddress)
        {
            return ApiHelper.GenerateTransactionRawTx(CallAddress,ContractAddress, method, inputParameter);
        }

    }
}