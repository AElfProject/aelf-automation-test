using System;
using System.Collections.Generic;
using System.Threading;
using AElf.Contracts.MultiToken;
using AElf.Types;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using Google.Protobuf;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace AElf.Automation.SideChain.Verification.CrossChainTransfer
{
    public class CrossChainCreateToken : CrossChainBase
    {
        public CrossChainCreateToken()
        {
            MainChainService = InitMainChainServices();
            SideChainServices = InitSideChainServices();
            TokenSymbols = new List<string>();
            ChainValidateTxInfo = new Dictionary<int, CrossChainTransactionInfo>();
            ChainCreateTxInfo = new Dictionary<string, CrossChainTransactionInfo>();
        }

        private Dictionary<string, CrossChainTransactionInfo> ChainCreateTxInfo { get; }
        private Dictionary<int, CrossChainTransactionInfo> ChainValidateTxInfo { get; }


        public void DoCrossChainCreateToken()
        {
            Logger.Info("Validate token symbol exists:");
            ValidateTokenInfoExists();
            Logger.Info("Waiting for indexing");
            Thread.Sleep(60000);
            Logger.Info("Cross chain Create token:");
            ValidateTokenSymbol();

            if (CreateTokenNumber <= 0) return;
            Logger.Info("Create token on main chain:");
            MainChainCreateToken();
            IssueToken();
            Logger.Info("Waiting for indexing");
            Thread.Sleep(60000);
            SideChainCrossCreateToken();
        }

        private void ValidateTokenInfoExists()
        {
            foreach (var sideChainService in SideChainServices)
            {
                var symbol = sideChainService.PrimaryTokenSymbol;
                var tokenInfo = sideChainService.TokenService.GetTokenInfo(symbol);
                var validateTransaction = sideChainService.NodeManager.GenerateRawTransaction(
                    sideChainService.CallAddress, sideChainService.TokenService.ContractAddress,
                    TokenMethod.ValidateTokenInfoExists.ToString(), new ValidateTokenInfoExistsInput
                    {
                        IsBurnable = tokenInfo.IsBurnable,
                        Issuer = tokenInfo.Issuer,
                        IssueChainId = tokenInfo.IssueChainId,
                        Decimals = tokenInfo.Decimals,
                        Symbol = tokenInfo.Symbol,
                        TokenName = tokenInfo.TokenName,
                        TotalSupply = tokenInfo.TotalSupply,
                        IsProfitable = tokenInfo.IsProfitable
                    });

                var txId = ExecuteMethodWithTxId(sideChainService, validateTransaction);
                var txResult = sideChainService.NodeManager.CheckTransactionResult(txId);
                if (txResult.Status.ConvertTransactionResultStatus() != TransactionResultStatus.Mined)
                    Assert.IsTrue(false,
                        $"Validate chain {sideChainService.ChainId} token symbol failed");
                var validateTokenTx = new CrossChainTransactionInfo(txResult.BlockNumber, txId, validateTransaction);
                ChainValidateTxInfo.Add(sideChainService.ChainId, validateTokenTx);
                Logger.Info($"Validate {sideChainService.ChainId} chain {symbol} token");
            }
        }

        private void ValidateTokenSymbol()
        {
            foreach (var sideChainService in SideChainServices)
            {
                var validate = CheckTokenExisted(sideChainService);
                if (validate.Count == 0) continue;
                var symbol = sideChainService.PrimaryTokenSymbol;
                //verify side chain token address
                var chainTxInfo = ChainValidateTxInfo[sideChainService.ChainId];

                var mainHeight = MainChainCheckSideChainBlockIndex(sideChainService, chainTxInfo.BlockHeight);
                var crossChainMerkleProofContext =
                    GetCrossChainMerkleProofContext(sideChainService, chainTxInfo.BlockHeight);
                var sideChainMerklePath =
                    GetMerklePath(sideChainService, chainTxInfo.BlockHeight, chainTxInfo.TxId);
                if (sideChainMerklePath == null)
                    throw new Exception("Can't get the merkle path.");
                var createInput = new CrossChainCreateTokenInput
                {
                    FromChainId = sideChainService.ChainId,
                    MerklePath = sideChainMerklePath,
                    TransactionBytes = ByteString.CopyFrom(ByteArrayHelper.HexStringToByteArray(chainTxInfo.RawTx))
                };

                createInput.MerklePath.MerklePathNodes.AddRange(crossChainMerkleProofContext
                    .MerklePathFromParentChain.MerklePathNodes);
                createInput.ParentChainHeight = crossChainMerkleProofContext.BoundParentChainHeight;

                foreach (var validateSideChainService in validate)
                {
                    while (mainHeight > GetIndexParentHeight(validateSideChainService))
                    {
                        Console.WriteLine("Block is not recorded ");
                        Thread.Sleep(10000);
                    }

                    var createResult =
                        validateSideChainService.TokenService.ExecuteMethodWithResult(TokenMethod.CrossChainCreateToken,
                            createInput);
                    createResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
                    var sideTokenInfo = validateSideChainService.TokenService.CallViewMethod<TokenInfo>(
                        TokenMethod.GetTokenInfo,
                        new GetTokenInfoInput {Symbol = symbol});
                    sideTokenInfo.Symbol.ShouldBe(symbol);
                }
            }
        }

        //cross create 
        private void MainChainCreateToken()
        {
            for (var i = 0; i < CreateTokenNumber; i++)
            {
                var symbol = $"TEST{CommonHelper.RandomString(4, false)}";
                var createResult = MainChainService.TokenService.ExecuteMethodWithResult(TokenMethod.Create,
                    new CreateInput
                    {
                        Symbol = symbol,
                        Decimals = 2,
                        IsBurnable = true,
                        Issuer = MainChainService.CallAccount,
                        TokenName = "Token of test",
                        TotalSupply = 10_0000_0000_00000000,
                        IsProfitable = true
                    });
                createResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
                var tokenInfo = MainChainService.TokenService.GetTokenInfo(symbol);
                var validateTransaction = MainChainService.NodeManager.GenerateRawTransaction(
                    MainChainService.CallAddress, MainChainService.TokenService.ContractAddress,
                    TokenMethod.ValidateTokenInfoExists.ToString(), new ValidateTokenInfoExistsInput
                    {
                        IsBurnable = tokenInfo.IsBurnable,
                        Issuer = tokenInfo.Issuer,
                        IssueChainId = tokenInfo.IssueChainId,
                        Decimals = tokenInfo.Decimals,
                        Symbol = tokenInfo.Symbol,
                        TokenName = tokenInfo.TokenName,
                        TotalSupply = tokenInfo.TotalSupply,
                        IsProfitable = tokenInfo.IsProfitable
                    });
                var txId = ExecuteMethodWithTxId(MainChainService, validateTransaction);
                var txResult = MainChainService.NodeManager.CheckTransactionResult(txId);
                if (txResult.Status.ConvertTransactionResultStatus() != TransactionResultStatus.Mined)
                    Assert.IsTrue(false,
                        $"Validate chain {MainChainService.ChainId} token symbol failed");
                var mainChainTx = new CrossChainTransactionInfo(txResult.BlockNumber, txId, validateTransaction);
                ChainCreateTxInfo.Add(symbol, mainChainTx);
                Logger.Info($"Create token {symbol} success");
                TokenSymbols.Add(symbol);
            }
        }

        private void IssueToken()
        {
            foreach (var symbol in TokenSymbols)
            {
                var issueToken = MainChainService.TokenService.ExecuteMethodWithResult(TokenMethod.Issue, new IssueInput
                {
                    Symbol = symbol,
                    Amount = 10_0000_0000_00000000,
                    Memo = "Issue token",
                    To = MainChainService.CallAccount
                });
                if (issueToken.Status.ConvertTransactionResultStatus() == TransactionResultStatus.Failed)
                    throw new Exception($"Issue token {symbol} failed");

                var balance =
                    MainChainService.TokenService.CallViewMethod<GetBalanceOutput>(TokenMethod.GetBalance,
                        new GetBalanceInput
                        {
                            Symbol = symbol,
                            Owner = MainChainService.CallAccount
                        }).Balance;
                Logger.Info($" user {MainChainService.CallAddress} token {symbol} balance is {balance}");
            }
        }

        private void SideChainCrossCreateToken()
        {
            foreach (var symbol in TokenSymbols)
            {
                var mainChainCreateTxInfo = ChainCreateTxInfo[symbol];
                var merklePath = GetMerklePath(MainChainService, mainChainCreateTxInfo.BlockHeight,
                    mainChainCreateTxInfo.TxId);
                if (merklePath == null)
                    throw new Exception("Can't get the merkle path.");
                var crossChainCreateInput = new CrossChainCreateTokenInput
                {
                    FromChainId = MainChainService.ChainId,
                    ParentChainHeight = mainChainCreateTxInfo.BlockHeight,
                    TransactionBytes =
                        ByteString.CopyFrom(ByteArrayHelper.HexStringToByteArray(mainChainCreateTxInfo.RawTx)),
                    MerklePath = merklePath
                };

                foreach (var sideChainService in SideChainServices)
                {
                    Logger.Info("Check the index:");
                    while (!CheckSideChainBlockIndex(sideChainService, mainChainCreateTxInfo))
                    {
                        Console.WriteLine("Block is not recorded ");
                        Thread.Sleep(10000);
                    }

                    var result =
                        sideChainService.TokenService.ExecuteMethodWithResult(TokenMethod.CrossChainCreateToken,
                            crossChainCreateInput);
                    if (result.Status.ConvertTransactionResultStatus() == TransactionResultStatus.Failed)
                        throw new Exception($"Side chain {sideChainService.ChainId} create token Failed");
                    Logger.Info($"Chain {sideChainService.ChainId} create Token {symbol} success");
                }
            }
        }

        private List<ContractServices> CheckTokenExisted(ContractServices services)
        {
            var validate = new List<ContractServices>();
            foreach (var sideChainService in SideChainServices)
            {
                if (sideChainService.Equals(services)) continue;
                var symbol = services.PrimaryTokenSymbol;
                var sideTokenInfo = sideChainService.TokenService.CallViewMethod<TokenInfo>(
                    TokenMethod.GetTokenInfo,
                    new GetTokenInfoInput {Symbol = symbol});
                if (sideTokenInfo.Equals(new TokenInfo()))
                    validate.Add(sideChainService);
            }

            return validate;
        }
    }
}