using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf;
using AElf.Types;
using AElfChain.Common;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using AElfChain.SDK;
using AElfChain.SDK.Models;
using Google.Protobuf.WellKnownTypes;
using Newtonsoft.Json;
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
        }

        public IApiService ApiService => NodeManager.ApiService;

        public override void RunCommand()
        {
            var command = Prompt.Select("Select api command", GetSubCommands());
            switch (command)
            {
                case "ChainsStatus":
                    ChainsStatus();
                    break;
                case "BlockAnalyze":
                    BlockAnalyze();
                    break;
                case "TransactionAnalyze":
                    TransactionAnalyze();
                    break;
                case "TransactionPoolAnalyze":
                    TransactionPoolAnalyze();
                    break;
                case "NodeElectionAnalyze":
                    NodeElectionAnalyze();
                    break;
                case "CheckAccountsToken":
                    CheckAccountsToken();
                    break;
                default:
                    Logger.Error("Not supported api method.");
                    var subCommands = GetSubCommands();
                    string.Join("\r\n", subCommands).WriteSuccessLine();
                    break;
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
        private void ChainsStatus()
        {
            //"Parameter: [ServiceUrl] [ServiceUrl]...".WriteSuccessLine();
            //var input = CommandOption.InputParameters(1);
            var endpoints = NodeInfoHelper.Config.Nodes.Select(o => o.Endpoint).ToList();
            var input = Prompt.MultiSelect("Select endpoint(s)", endpoints);
            var nodeManagers = new List<NodeManager>();
            input.ToList().ForEach(o =>
            {
                var manager = new NodeManager(o);
                nodeManagers.Add(manager);
            });
            foreach(var manager in nodeManagers)
            {
                var chainStatus = AsyncHelper.RunSync(manager.ApiService.GetChainStatusAsync);
                $"Node: {manager.GetApiUrl()}".WriteSuccessLine();
                JsonConvert.SerializeObject(chainStatus, Formatting.Indented).WriteSuccessLine();
               System.Console.WriteLine();
            }
        }
        private void BlockAnalyze()
        {
            "Parameter: [StartHeight] [EndHeight]=null [Continuous]=false".WriteSuccessLine();
            var input = CommandOption.InputParameters(1);
            var startHeight = long.Parse(input[0]);
            var blockHeight = AsyncHelper.RunSync(ApiService.GetBlockHeightAsync);
            if (blockHeight < startHeight)
                Logger.Error("Wrong block height");
            var endHeight = input.Length == 2 ? long.Parse(input[1]) : blockHeight;
            var continuous = input.Length == 3 && bool.Parse(input[2]);
            Node minerNode = null;
            var minerKey = "";
            NodeInfoHelper.Config.CheckNodesAccount();
            var nodes = NodeInfoHelper.Config.Nodes;
            Logger.Info("Begin analyze block generate and transactions information:");
            var continueBlocks = 0;
            while (true)
            {
                for (var i = startHeight; i <= endHeight; i++)
                {
                    var height = i;
                    var block = AsyncHelper.RunSync(() => ApiService.GetBlockByHeightAsync(height));
                    var signerKey = block.Header.SignerPubkey;
                    if (minerKey == "")
                    {
                        minerKey = signerKey;
                        minerNode = nodes.FirstOrDefault(o => o.PublicKey == signerKey);
                        continueBlocks++;
                    }
                    else
                    {
                        if (minerKey == signerKey)
                            continueBlocks++;
                        else
                        {
                            $"Continue blocks: {continueBlocks}".WriteSuccessLine();
                            System.Console.WriteLine();
                            minerKey = signerKey;
                            minerNode = nodes.FirstOrDefault(o => o.PublicKey == signerKey);
                            continueBlocks = 1;
                        }
                    }

                    var minerInfo = minerNode == null ? minerKey.Substring(0, 10) : minerNode.Name;
                    Logger.Info(
                        $"Time: {block.Header.Time:HH:mm:ss.fff}    Height: {height.ToString()}    Miner: {minerInfo}    TxCount: {block.Body.TransactionsCount}");
                }

                if (!continuous) break;
                startHeight = endHeight + 1;
                while (true)
                {
                    blockHeight = AsyncHelper.RunSync(ApiService.GetBlockHeightAsync);
                    if (blockHeight == endHeight)
                        Thread.Sleep(3000);
                    endHeight = blockHeight;
                    break;
                }
            }
        }
        private void TransactionAnalyze()
        {
            "Parameter: [StartHeight] [EndHeight]=null [Continuous]=false".WriteSuccessLine();
            var input = CommandOption.InputParameters(1);
            var startHeight = long.Parse(input[0]);
            var blockHeight = AsyncHelper.RunSync(ApiService.GetBlockHeightAsync);
            if (blockHeight < startHeight)
                Logger.Error("Wrong block height");
            var endHeight = input.Length == 2 ? long.Parse(input[1]) : blockHeight;
            var continuous = input.Length != 3 || bool.Parse(input[2]);
            Logger.Info("Begin analyze block transactions information:");
            while (true)
            {
                for (var i = startHeight; i <= endHeight; i++)
                {
                    var height = i;
                    var block = AsyncHelper.RunSync(() => NodeManager.ApiService.GetBlockByHeightAsync(height, true));
                    Logger.Info(
                        $"BlockHeight: {height}, Hash: {block.BlockHash}, Transactions: {block.Body.TransactionsCount}");
                    foreach (var txId in block.Body.Transactions)
                    {
                        var transaction =
                            AsyncHelper.RunSync(() => NodeManager.ApiService.GetTransactionResultAsync(txId));
                        if (transaction.Status.ConvertTransactionResultStatus() == TransactionResultStatus.Mined)
                        {
                            Logger.Info(
                                $"{txId} {transaction.Transaction.MethodName} {transaction.Status} {transaction.TransactionFee.GetTransactionFeeInfo()}");
                        }
                        else
                        {
                            var errorMsg = transaction.Error.Split("\n")[1];
                            Logger.Error(
                                $"{txId} {transaction.Transaction.MethodName} {transaction.Status} {transaction.TransactionFee.GetTransactionFeeInfo()}");
                            Logger.Error($"Error: {errorMsg}");
                        }
                    }

                    System.Console.WriteLine();
                }

                if (!continuous) break;
                startHeight = endHeight + 1;
                while (true)
                {
                    blockHeight = AsyncHelper.RunSync(ApiService.GetBlockHeightAsync);
                    if (blockHeight == endHeight)
                        Thread.Sleep(3000);
                    endHeight = blockHeight;
                    break;
                }
            }
        }
        private void NodeElectionAnalyze()
        {
            NodeInfoHelper.Config.CheckNodesAccount();
            long termNumber = 1;

            while (true)
            {
                var termNo = Services.Consensus.GetCurrentTermInformation();
                if (termNo != termNumber)
                {
                    termNumber = termNo;
                    var minerList =
                        AsyncHelper.RunSync(() => Services.ConsensusStub.GetCurrentMinerList.CallAsync(new Empty()));
                    var pubKeys = minerList.Pubkeys.Select(item => item.ToByteArray().ToHex()).ToList();
                    var bpCollection = "";
                    foreach (var node in NodeOption.AllNodes)
                        if (pubKeys.Contains(node.PublicKey))
                        {
                            bpCollection += $"{node.Name}  ";
                        }

                    $"Current bp account info: {bpCollection.Trim()}".WriteSuccessLine();
                }

                while (true)
                {
                    var nextTerm = AsyncHelper.RunSync(() =>
                        Services.ConsensusStub.GetNextElectCountDown.CallAsync(new Empty()));
                    if (nextTerm.Value <= 2)
                    {
                        Thread.Sleep(5);
                        System.Console.WriteLine();
                        break;
                    }

                    System.Console.Write($"\rNextElectCountDown: {nextTerm.Value}s");
                    Thread.Sleep(950);
                }
            }
        }
        private void TransactionPoolAnalyze()
        {
            //"Parameter: [ServiceUrl] [ServiceUrl]...".WriteSuccessLine();
            //var input = CommandOption.InputParameters(1);
            var endpoints = NodeInfoHelper.Config.Nodes.Select(o => o.Endpoint).ToList();
            var input = Prompt.MultiSelect("Select endpoint(s)", endpoints);
            var nodeManagers = new List<NodeManager>();
            input.ToList().ForEach(o =>
            {
                var manager = new NodeManager(o);
                nodeManagers.Add(manager);
            });

            while (true)
            {
                Parallel.ForEach(nodeManagers, manager =>
                {
                    var transactionPoolStatus = AsyncHelper.RunSync(manager.ApiService.GetTransactionPoolStatusAsync);
                    $"{DateTime.Now:HH:mm:ss} {manager.GetApiUrl()} QueuedTxs: {transactionPoolStatus.Queued} ValidatedTxs: {transactionPoolStatus.Validated}"
                        .WriteSuccessLine();
                });
                System.Console.WriteLine();
                Thread.Sleep(500);
            }
        }
        private void CheckAccountsToken()
        {
            var accounts = NodeManager.ListAccounts();
            _ = Services.Token.ContractAddress;
            var primaryToken = NodeManager.GetPrimaryTokenSymbol();
            Parallel.ForEach(accounts, acc =>
            {
                var balance = Services.Token.GetUserBalance(acc, primaryToken);
                if (balance != 0)
                    $"Account: {acc}  {primaryToken}={balance}".WriteSuccessLine();
            });
        }
        private IEnumerable<string> GetSubCommands()
        {
            return new List<string>
            {
                "ChainsStatus",
                "BlockAnalyze",
                "TransactionAnalyze",
                "TransactionPoolAnalyze",
                "NodeElectionAnalyze",
                "CheckAccountsToken"
            };
        }
    }
}