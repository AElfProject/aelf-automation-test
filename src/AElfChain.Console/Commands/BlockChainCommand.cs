using System.Collections.Generic;
using AElf.Automation.Common.Helpers;
using AElf.Automation.Common.Managers;
using AElfChain.Console.InputOption;
using AElfChain.SDK;
using Newtonsoft.Json;
using Volo.Abp.Threading;

namespace AElfChain.Console.Commands
{
    public class BlockChainCommand : BaseCommand
    {
        private ConsoleReader Reader { get; set; }
        private ApiCompletionEngine AutoEngine { get; set; }
        public IApiService ApiService => NodeManager.ApiService;

        public BlockChainCommand(INodeManager nodeManager, ContractServices contractServices) : base(nodeManager,
            contractServices)
        {
            Logger = Log4NetHelper.GetLogger();
            AutoEngine = new ApiCompletionEngine();
        }

        public override void RunCommand()
        {
            Reader = new ConsoleReader(AutoEngine);
            var input = CommandOption.InputParameters(1, Reader);
            var command = input[0].Trim();
            switch (command)
            {
                case "BlockHeight":
                    GetBlockHeight();
                    break;
                case "BlockByHash":
                    GetBlockByHash();
                    break;
                case "BlockByHeight":
                    GetBlockByHeight();
                    break;
                case "TransactionPoolStatus":
                    GetTransactionPoolStatus();
                    break;
                case "BlockState":
                    GetBlockState();
                    break;
                case "CurrentRoundInformation":
                    GetCurrentRoundInformation();
                    break;
                case "ChainStatus":
                    GetChainStatus();
                    break;
                case "TaskQueueStatus":
                    GetTaskQueueStatus();
                    break;
                case "TransactionResult":
                    GetTransactionResult();
                    break;
                case "TransactionResults":
                    GetTransactionResults();
                    break;
                case "ListAccounts":
                    ListAllAccounts();
                    break;
                default:
                    Logger.Error("Not supported api method.");
                    var subCommands = GetSubCommands();
                    string.Join("\r\n", subCommands).WriteSuccessLine();
                    break;
            }
        }

        private void GetBlockHeight()
        {
            var height = AsyncHelper.RunSync(ApiService.GetBlockHeightAsync);
            $"Current chain height: {height}".WriteSuccessLine();
        }

        private void GetBlockByHash()
        {
            "Parameter: [BlockHash] [IncludeTransaction]=false".WriteSuccessLine();
            var input = CommandOption.InputParameters(1);
            var hash = input[0];
            var includeTransaction = input.Length != 1 && bool.Parse(input[1]);
            var block = AsyncHelper.RunSync(() => ApiService.GetBlockAsync(hash, includeTransaction));
            JsonConvert.SerializeObject(block, Formatting.Indented).WriteSuccessLine();
        }

        private void GetBlockByHeight()
        {
            "Parameter: [Height] [IncludeTransaction]=false".WriteSuccessLine();
            var input = CommandOption.InputParameters(1);
            var height = long.Parse(input[0]);
            var includeTransaction = input.Length != 1 && bool.Parse(input[1]);
            var block = AsyncHelper.RunSync(() => ApiService.GetBlockByHeightAsync(height, includeTransaction));
            JsonConvert.SerializeObject(block, Formatting.Indented).WriteSuccessLine();
        }

        private void GetCurrentRoundInformation()
        {
            var roundInformation = AsyncHelper.RunSync(ApiService.GetCurrentRoundInformationAsync);
            JsonConvert.SerializeObject(roundInformation, Formatting.Indented).WriteSuccessLine();
        }

        private void GetTransactionPoolStatus()
        {
            var transactionPoolStatusInfo = AsyncHelper.RunSync(ApiService.GetTransactionPoolStatusAsync);
            JsonConvert.SerializeObject(transactionPoolStatusInfo, Formatting.Indented).WriteSuccessLine();
        }

        private void GetBlockState()
        {
            "Parameter: [BlockHash]".WriteSuccessLine();
            var input = CommandOption.InputParameters(1);
            var hash = input[0];
            var blockState = AsyncHelper.RunSync(() => ApiService.GetBlockStateAsync(hash));
            JsonConvert.SerializeObject(blockState, Formatting.Indented).WriteSuccessLine();
        }

        private void GetChainStatus()
        {
            var chainInfo = AsyncHelper.RunSync(ApiService.GetChainStatusAsync);
            JsonConvert.SerializeObject(chainInfo, Formatting.Indented).WriteSuccessLine();
        }

        private void GetTaskQueueStatus()
        {
            var taskQueueInfo = AsyncHelper.RunSync(ApiService.GetTaskQueueStatusAsync);
            JsonConvert.SerializeObject(taskQueueInfo, Formatting.Indented).WriteSuccessLine();
        }

        private void GetTransactionResult()
        {
            "Parameter: [TransactionId]".WriteSuccessLine();
            var input = CommandOption.InputParameters(1);
            var transactionId = input[0];
            var resultDto = AsyncHelper.RunSync(() => ApiService.GetTransactionResultAsync(transactionId));
            JsonConvert.SerializeObject(resultDto, Formatting.Indented).WriteSuccessLine();
        }

        private void GetTransactionResults()
        {
            "Parameter: [BlockHash] [Offset]=0 [Limit]=10".WriteSuccessLine();
            var input = CommandOption.InputParameters(1);
            var blockHash = input[0];
            var offset = input.Length >= 2 ? int.Parse(input[1]) : 0;
            var limit = input.Length == 3 ? int.Parse(input[2]) : 10;
            var resultDto = AsyncHelper.RunSync(() => ApiService.GetTransactionResultsAsync(blockHash, offset, limit));
            JsonConvert.SerializeObject(resultDto, Formatting.Indented).WriteSuccessLine();
        }

        private void ListAllAccounts()
        {
            var accounts = NodeManager.ListAccounts();
            "Accounts List:".WriteSuccessLine();
            for (var i = 0; i < accounts.Count; i++)
            {
                $"{accounts[i].PadRight(54)}".WriteSuccessLine(changeLine: false);
                if (i % 3 == 2)
                    System.Console.WriteLine();
            }

            if (accounts.Count % 2 != 0)
                System.Console.WriteLine();
        }

        public override CommandInfo GetCommandInfo()
        {
            return new CommandInfo
            {
                Name = "chain",
                Description = "Query block chain api"
            };
        }

        public List<string> GetSubCommands()
        {
            return new List<string>
            {
                "BlockHeight",
                "BlockByHash",
                "BlockByHeight",
                "BlockState",
                "ChainStatus",
                "CurrentRoundInformation",
                "TaskQueueStatus",
                "TransactionResult",
                "TransactionResults",
                "TransactionPoolStatus",
                "ListAccounts"
            };
        }

        public override string[] InputParameters()
        {
            throw new System.NotImplementedException();
        }
    }
}