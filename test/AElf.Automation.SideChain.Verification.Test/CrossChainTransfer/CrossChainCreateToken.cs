using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AElfChain.Common.Contracts;
using AElfChain.Common.Helpers;
using AElf.Contracts.MultiToken;
using AElf.Contracts.TokenConverter;
using AElf.Types;
using AElfChain.Common.Utils;
using Google.Protobuf;
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
            ChainCreateTxInfo = new Dictionary<string, CrossChainTransactionInfo>();
        }

        public Dictionary<string, CrossChainTransactionInfo> ChainCreateTxInfo { get; set; }

        public void DoCrossChainCreateToken()
        {
            Logger.Info("Create token:");
            MainChainCreateToken();
            Logger.Info("Issue token:");
            IssueToken();

            Logger.Info("Waiting for indexing");
            Thread.Sleep(150000);
            SideChainCrossCreateToken();
        }

        //cross create 
        private void MainChainCreateToken()
        {
            for (var i = 0; i < CreateTokenNumber; i++)
            {
                var symbol = $"ELF{CommonHelper.RandomString(4, false)}";
                var createTransaction = MainChainService.TokenService.NodeManager.GenerateRawTransaction(
                    MainChainService.CallAddress, MainChainService.TokenService.ContractAddress,
                    TokenMethod.Create.ToString(), new CreateInput
                    {
                        Symbol = symbol,
                        Decimals = 2,
                        IsBurnable = true,
                        Issuer = MainChainService.CallAccount,
                        TokenName = "Token of test",
                        TotalSupply = 5_0000_0000
                    });
                var txId = ExecuteMethodWithTxId(MainChainService, createTransaction);
                var txResult = CheckTransactionResult(MainChainService, txId);

                if (txResult.Status.ConvertTransactionResultStatus() == TransactionResultStatus.Failed &&
                    txResult.Error.Contains("Token already exists."))
                {
                    Logger.Info($"Token {symbol} already created");
                    continue;
                }

                if (txResult.Status.ConvertTransactionResultStatus() == TransactionResultStatus.Failed)
                    throw new Exception($"Create token {symbol} Failed");
                var mainChainTx = new CrossChainTransactionInfo(txResult.BlockNumber, txId, createTransaction);
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
                    Amount = 5_0000_0000,
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
                        Logger.Info("Block is not recorded ");
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

        private async Task BuyResources(long amount)
        {
            Logger.Info("Prepare resources token.");
            var genesis = MainChainService.GenesisService;
            var tokenConverter = genesis.GetContractAddressByName(NameProvider.TokenConverter);
            var converter = new TokenConverterContract(MainChainService.NodeManager, MainChainService.CallAddress,
                tokenConverter.GetFormatted());
            var testStub =
                converter.GetTestStub<TokenConverterContractContainer.TokenConverterContractStub>(MainChainService
                    .CallAddress);

            var symbols = new List<string> {"CPU", "NET", "STO"};
            foreach (var symbol in symbols)
            {
                var transactionResult = await testStub.Buy.SendAsync(new BuyInput
                {
                    Symbol = symbol,
                    Amount = amount
                });
                transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            }
        }
    }
}