using System;
using System.Collections.Generic;
using System.Linq;
using AElf.Types;
using AElfChain.Common;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using AElfChain.Console.InputOption;
using AElfChain.SDK;
using AElfChain.SDK.Models;
using Sharprompt;
using Volo.Abp.Threading;

namespace AElfChain.Console.Commands
{
    public class AnalyzeCommand : BaseCommand
    {
        public AnalyzeCommand(INodeManager nodeManager, ContractServices contractServices)
            : base(nodeManager, contractServices)
        {
            Logger = Log4NetHelper.GetLogger();
            AutoEngine = new ApiCompletionEngine();
        }

        private ApiCompletionEngine AutoEngine { get; }
        public IApiService ApiService => NodeManager.ApiService;

        public override void RunCommand()
        {
            var command = Prompt.Select("Select api command", GetSubCommands());
            switch (command)
            {
                case "BlockAnalyze":
                    BlockAnalyze();
                    break;
                case "TransactionAnalyze":
                    TransactionAnalyze();
                    break;
                default:
                    Logger.Error("Not supported api method.");
                    var subCommands = GetSubCommands();
                    string.Join("\r\n", subCommands).WriteSuccessLine();
                    break;
            }
        }

        private void BlockAnalyze()
        {
            "Parameter: [StartHeight] [EndHeight]=null".WriteSuccessLine();
            var input = CommandOption.InputParameters(1);
            var startHeight = long.Parse(input[0]);
            var blockHeight = AsyncHelper.RunSync(ApiService.GetBlockHeightAsync);
            if (blockHeight < startHeight)
                Logger.Error("Wrong block height");
            var endHeight = input.Length == 2 ? long.Parse(input[1]) : blockHeight;
            Node minerNode = null;
            var minerKey = "";
            NodeInfoHelper.Config.CheckNodesAccount();
            var nodes = NodeInfoHelper.Config.Nodes;
            Logger.Info("Begin analyze block generate and transactions information:");
            for (var i = startHeight; i <= endHeight; i++)
            {
                var height = i;
                var block = AsyncHelper.RunSync(() => ApiService.GetBlockByHeightAsync(height));
                var signerKey = block.Header.SignerPubkey;
                if (minerKey == "")
                {
                    minerKey = signerKey;
                    minerNode = nodes.FirstOrDefault(o => o.PublicKey == signerKey);
                }
                else
                {
                    if (minerKey != signerKey)
                    {
                        System.Console.WriteLine();
                        minerKey = signerKey;
                        minerNode = nodes.FirstOrDefault(o => o.PublicKey == signerKey);
                    }
                }

                var minerInfo = minerNode == null ? minerKey.Substring(0, 10) : minerNode.Name;
                Logger.Info(
                    $"Time: {block.Header.Time:O}  Height: {height.ToString().PadRight(8)} Miner: {minerInfo.PadRight(10)} TxCount: {block.Body.TransactionsCount:000}");
            }
        }

        private void TransactionAnalyze()
        {
            "Parameter: [StartHeight] [EndHeight]=null IgnoreSystemTransactions=true".WriteSuccessLine();
            var input = CommandOption.InputParameters(1);
            var startHeight = long.Parse(input[0]);
            var blockHeight = AsyncHelper.RunSync(ApiService.GetBlockHeightAsync);
            if (blockHeight < startHeight)
                Logger.Error("Wrong block height");
            var endHeight = input.Length == 2 ? long.Parse(input[1]) : blockHeight;
            var ignoreTx = input.Length != 3 || bool.Parse(input[2]);
            Logger.Info("Begin analyze block transactions information:");
            for (var i = startHeight; i <= endHeight; i++)
            {
                var height = i;
                var block = AsyncHelper.RunSync(() => NodeManager.ApiService.GetBlockByHeightAsync(height, true));
                Logger.Info($"BlockHeight: {height}, Hash: {block.BlockHash}, Transactions: {block.Body.TransactionsCount}");
                if (ignoreTx && block.Body.TransactionsCount >= 3)
                    foreach (var txId in block.Body.Transactions)
                    {
                        var transaction =
                            AsyncHelper.RunSync(() => NodeManager.ApiService.GetTransactionResultAsync(txId));
                        if (transaction.Status.ConvertTransactionResultStatus() == TransactionResultStatus.Mined)
                        {
                            Logger.Info(
                                $"TxId: {txId}, Name: {transaction.Transaction.MethodName}, Status: {transaction.Status}, Fee: {transaction.TransactionFee.GetTransactionFeeInfo()}");
                        }
                        else
                        {
                            Logger.Error(
                                $"TxId: {txId}, Name: {transaction.Transaction.MethodName}, Status: {transaction.Status}, Fee: {transaction.TransactionFee.GetTransactionFeeInfo()}");
                            Logger.Error(transaction.Error);
                        }
                    }

                System.Console.WriteLine();
            }
        }

        public override CommandInfo GetCommandInfo()
        {
            return new CommandInfo
            {
                Name = "analyze",
                Description = "Analyze block chain blocks and transactions."
            };
        }

        public override string[] InputParameters()
        {
            throw new NotImplementedException();
        }

        private IEnumerable<string> GetSubCommands()
        {
            return new List<string>
            {
                "BlockAnalyze",
                "TransactionAnalyze"
            };
        }
    }
}