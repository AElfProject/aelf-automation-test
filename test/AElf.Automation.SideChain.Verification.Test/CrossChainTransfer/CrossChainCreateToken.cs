using System.Collections.Generic;
using System.Threading;
using AElf.Automation.Common.Contracts;
using AElf.Automation.Common.Helpers;
using AElf.Automation.Common.WebApi.Dto;
using AElf.Contracts.MultiToken.Messages;
using AElf.Types;
using Google.Protobuf;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AElf.Automation.SideChain.Verification.CrossChainTransfer
{
    public class CrossChainCreateToken : CrossChainBase
    {
        public Dictionary<string, CrossChainTransactionInfo> ChainCreateTxInfo { get; set; }

        public CrossChainCreateToken()
        {
            MainChainService = InitMainChainServices();
            SideChainServices = InitSideChainServices();
            TokenSymbol = new List<string>();
            ChainCreateTxInfo = new Dictionary<string, CrossChainTransactionInfo>();
        }

        public void DoCrossChainCreateToken()
        {
            Logger.Info("Create token:");
            MainChainCreateToken();
            Logger.Info("Issue token:");
            IssueToken();
            
            Logger.Info("Waiting for indexing");
            Thread.Sleep(120000);
            SideChainCrossCreateToken();
        }

        //cross create 
        private void MainChainCreateToken()
        {
            for (var i = 0; i < CreateTokenNumber; i++)
            {
                var symbol = $"ELF{CommonHelper.RandomString(4, false)}";
                var createTransaction = MainChainService.TokenService.ApiHelper.GenerateTransactionRawTx(
                    MainChainService.CallAddress, MainChainService.TokenService.ContractAddress,
                    TokenMethod.Create.ToString(), new CreateInput()
                    {
                        Symbol = symbol,
                        Decimals = 2,
                        IsBurnable = true,
                        Issuer = MainChainService.CallAccount,
                        TokenName = "Token of test",
                        TotalSupply = 5_0000_0000
                    });
                var txId = ExecuteMethodWithTxId(MainChainService, createTransaction);
                var result = CheckTransactionResult(MainChainService, txId);

                if (!(result.InfoMsg is TransactionResultDto txResult))
                {
                    Logger.Error($"Token {symbol} create failed. ");
                    return;
                }

                if (txResult.Status.ConvertTransactionResultStatus() == TransactionResultStatus.Failed &&
                    txResult.Error.Contains("Token already exists."))
                {
                    Logger.Info($"Token {symbol} already created");
                    TokenSymbol.Add(symbol);
                    continue;
                }

                if (txResult.Status.ConvertTransactionResultStatus()!= TransactionResultStatus.Mined)
                    Assert.IsTrue(false, $"Create token {symbol} Failed");
                var mainChainTx = new CrossChainTransactionInfo(txResult.BlockNumber, txId, createTransaction);
                ChainCreateTxInfo.Add(symbol, mainChainTx);
                Logger.Info($"Create token {symbol} success");
                TokenSymbol.Add(symbol);
            }
        }

        private void IssueToken()
        {
            foreach (var symbol in TokenSymbol)
            {
                var issueToken = MainChainService.TokenService.ExecuteMethodWithResult(TokenMethod.Issue, new IssueInput
                {
                    Symbol = symbol,
                    Amount = 5_0000_0000,
                    Memo = "Issue token",
                    To = MainChainService.CallAccount
                });
                if (!(issueToken.InfoMsg is TransactionResultDto issueTokenResult)) return;
                if (issueTokenResult.Status.ConvertTransactionResultStatus() != TransactionResultStatus.Mined)
                    Assert.IsTrue(false, $"Issue token {symbol} failed");

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
            foreach (var symbol in TokenSymbol)
            {
                var mainChainCreateTxInfo = ChainCreateTxInfo[symbol];

                var merklePath = GetMerklePath(MainChainService, mainChainCreateTxInfo.BlockHeight,mainChainCreateTxInfo.TxId);
                if (merklePath == null)
                    Assert.IsTrue(false, "Can't get the merkle path.");
                var crossChainCreateInput = new CrossChainCreateTokenInput
                {
                    FromChainId = MainChainService.ChainId,
                    ParentChainHeight = mainChainCreateTxInfo.BlockHeight,
                    TransactionBytes =
                        ByteString.CopyFrom(ByteArrayHelper.HexStringToByteArray(mainChainCreateTxInfo.RawTx))
                };
                crossChainCreateInput.MerklePath.AddRange(merklePath.Path);

                foreach (var sideChainService in SideChainServices)
                {
                    var result =
                        sideChainService.TokenService.ExecuteMethodWithResult(TokenMethod.CrossChainCreateToken,
                            crossChainCreateInput);
                    if (!(result.InfoMsg is TransactionResultDto txResult)) return;
                    if (txResult.Status.ConvertTransactionResultStatus() != TransactionResultStatus.Mined)
                        Assert.IsTrue(false, $"Side chain {sideChainService.ChainId} create token Failed");
                    Logger.Info($"Chain {sideChainService.ChainId} create Token {symbol} success");
                }
            }
        }
    }
}