using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.Types;
using AElfChain.Common.Contracts;
using Google.Protobuf;
using Shouldly;

namespace AElf.Automation.SideChainEconomicTest
{
    public class MainChainTests : TestBase
    {
        public void GetTokenInfo()
        {
            Logger.Info("Query main chain token info");

            Main.GetTokenInfos();
            Main.GetTokenBalances(Main.CallAddress);
        }

        public async Task Transfer_From_Main_To_Side()
        {
            Main.GetTokenBalances(Main.CallAddress);
            var mainToken = Main.GenesisService.GetTokenStub();

            var transactionResults = new Dictionary<Transaction, TransactionResult>();
            //main chain transfer
            foreach (var symbol in Main.Symbols)
            {
                var transactionResult = await mainToken.CrossChainTransfer.SendAsync(new CrossChainTransferInput
                {
                    Symbol = symbol,
                    Amount = 2000_00000000,
                    IssueChainId = ChainConstInfo.MainChainId,
                    To = SideA.CallAccount,
                    ToChainId = ChainConstInfo.SideChainIdA,
                    Memo = $"cross chain transfer - {Guid.NewGuid()}"
                });
                transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
                transactionResults.Add(transactionResult.Transaction, transactionResult.TransactionResult);
            }

            Main.GetTokenBalances(Main.CallAddress);

            //wait index
            await MainManager.WaitSideChainIndex(SideA, transactionResults.Values.Last().BlockNumber);

            //side chain accept
            SideA.GetTokenBalances(SideA.CallAddress);

            var sideToken = SideA.GenesisService.GetTokenStub();
            foreach (var (transaction, transactionResult) in transactionResults)
            {
                var rawInfo = transaction.ToByteArray().ToHex();
                var merklePath = await Main.GetMerklePath(transactionResult.TransactionId.ToHex());
                var receiveResult = await sideToken.CrossChainReceiveToken.SendAsync(new CrossChainReceiveTokenInput
                {
                    FromChainId = ChainConstInfo.MainChainId,
                    ParentChainHeight = transactionResult.BlockNumber,
                    MerklePath = merklePath,
                    TransferTransactionBytes = ByteString.CopyFrom(ByteArrayHelper.HexStringToByteArray(rawInfo))
                });
                receiveResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            }

            SideA.GetTokenBalances(SideA.CallAddress);
        }
    }
}