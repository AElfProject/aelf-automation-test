using System;
using System.Threading;
using AElf.Contracts.MultiToken;
using AElfChain.Common.Contracts;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using AElfChain.Common.Utils;

namespace AElf.Automation.ScenariosExecution.ContractActions
{
    public partial class TransactionJob
    {
        public string TokenAddress => Services.TokenService.ContractAddress;
        public string Symbol;

        public void AddTokenActions(int timeInterval)
        {
            var nodeManager = GetNodeManager();
            Symbol = nodeManager.GetPrimaryTokenSymbol();
            while (true)
            {
                AddTransaction(GenerateTransferTx(nodeManager));

                var (from, to) = TesterJob.GetRandomAccountPair();
                var randomBalance = 1000 * CommonHelper.GenerateRandomNumber(1, 20);
                AddTransaction(GenerateApproveTx(nodeManager, from, to, randomBalance));
                AddTransaction(GenerateTransferFrom(nodeManager, from, to, randomBalance));
                Thread.Sleep(timeInterval * 1000);
            }
        }

        public string GenerateTransferTx(INodeManager nodeManager)
        {
            var (from, to) = TesterJob.GetRandomAccountPair();
            var rawTransaction = nodeManager.GenerateRawTransaction(from, TokenAddress, nameof(TokenMethod.Transfer),
                new TransferInput
                {
                    To = to.ConvertAddress(),
                    Symbol = Symbol,
                    Amount = 1000 * CommonHelper.GenerateRandomNumber(1, 10),
                    Memo = $"transfer action {Guid.NewGuid()}"
                });
            return rawTransaction;
        }

        public string GenerateApproveTx(INodeManager nodeManager, string from, string to, long amount)
        {
            var rawTransaction = nodeManager.GenerateRawTransaction(from, TokenAddress, nameof(TokenMethod.Approve),
                new ApproveInput
                {
                    Spender = to.ConvertAddress(),
                    Symbol = Symbol,
                    Amount = amount
                });
            return rawTransaction;
        }

        public string GenerateTransferFrom(INodeManager nodeManager, string from, string spender, long amount)
        {
            var rawTransaction = nodeManager.GenerateRawTransaction(spender, TokenAddress,
                nameof(TokenMethod.TransferFrom),
                new TransferFromInput
                {
                    From = from.ConvertAddress(),
                    To = spender.ConvertAddress(),
                    Symbol = Symbol,
                    Amount = amount,
                    Memo = $"transfer from action {Guid.NewGuid()}"
                });
            return rawTransaction;
        }
    }
}