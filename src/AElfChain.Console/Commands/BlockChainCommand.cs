using System.Collections.Generic;
using System.Linq;
using AElfChain.Common;
using AElfChain.Common.ContractSerializer;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using AElfChain.Common;
using AElfChain.Common.Managers;
using AElfChain.Console.InputOption;
using AElfChain.SDK;
using Newtonsoft.Json;
using Sharprompt;
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
            /*
            Reader = new ConsoleReader(AutoEngine);
            var input = CommandOption.InputParameters(1, Reader);
            var command = input[0].Trim();
            */
            var command = Prompt.Select("Select api command", GetSubCommands());
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
                case "BlockAnalyze":
                    BlockAnalyze();
                    break;
                case "CurrentRoundInformation":
                    GetCurrentRoundInformation();
                    break;
                case "ChainStatus":
                    GetChainStatus();
                    break;
                case "ContractFileDescriptor":
                    ContractFileDescriptor();
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
                case "GetRoundFromBase64":
                    GetRoundFromBase64();
                    break;
                case "GetMiningSequences":
                    GetMiningSequences();
                    break;
                case "ListAccounts":
                    ListAllAccounts();
                    break;
                case "GetAccountPubicKey":
                    GetAccountPublicKey();
                    break;
                case "GetPeers":
                    GetPeers();
                    break;
                case "AddPeer":
                    AddPeer();
                    break;
                case "RemovePeer":
                    RemovePeer();
                    break;
                case "NetworkInfo":
                    NetworkInfo();
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

        private void BlockAnalyze()
        {
            "Parameter: [StartHeight] [EndHeight]=null".WriteSuccessLine();
            var input = CommandOption.InputParameters(1);
            var startHeight = long.Parse(input[0]);
            var blockHeight = AsyncHelper.RunSync(ApiService.GetBlockHeightAsync);
            if(blockHeight < startHeight)
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
                Logger.Info($"Time: {block.Header.Time:O}  Height: {height.ToString().PadRight(8)} Miner: {minerInfo.PadRight(10)} TxCount: {block.Body.TransactionsCount:000}");
            }
        }

        private void GetChainStatus()
        {
            var chainInfo = AsyncHelper.RunSync(ApiService.GetChainStatusAsync);
            JsonConvert.SerializeObject(chainInfo, Formatting.Indented).WriteSuccessLine();
        }

        private void ContractFileDescriptor()
        {
            "Parameter: [ContractAddress]".WriteSuccessLine();
            var input = CommandOption.InputParameters(1);
            var descriptorSet = AsyncHelper.RunSync(() => ApiService.GetContractFileDescriptorSetAsync(input[0]));
            var customContract = new CustomContractHandler(descriptorSet);
            customContract.GetAllMethodsInfo();
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

        private void GetRoundFromBase64()
        {
            "Parameter: [base64Info]".WriteSuccessLine();
            var input = CommandOption.InputParameters(1);
            var roundInfo = AsyncHelper.RunSync(() => ApiService.GetRoundFromBase64Async(input[0]));
            JsonConvert.SerializeObject(roundInfo, Formatting.Indented).WriteSuccessLine();
        }

        private void GetMiningSequences()
        {
            "Parameter: [Count]".WriteSuccessLine();
            var input = CommandOption.InputParameters(1);
            int.TryParse(input[0], out var count);
            var sequences = AsyncHelper.RunSync(() => ApiService.GetMiningSequencesAsync(count));
            JsonConvert.SerializeObject(sequences, Formatting.Indented).WriteSuccessLine();
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

        private void GetAccountPublicKey()
        {
            $"Parameter: [Account] [Password]={NodeOption.DefaultPassword}".WriteSuccessLine();
            var input = CommandOption.InputParameters(1);
            var account = input[0];
            var password = input.Length == 2 ? input[1] : NodeOption.DefaultPassword;
            var publicKey = NodeManager.GetAccountPublicKey(account, password);
            $"PublicKey: {publicKey}".WriteSuccessLine();
        }

        private void GetPeers()
        {
            var peers = NodeManager.NetGetPeers();
            JsonConvert.SerializeObject(peers, Formatting.Indented).WriteSuccessLine();
        }

        private void AddPeer()
        {
            "Parameter: [NetServiceAddress]".WriteSuccessLine();
            var input = CommandOption.InputParameters(1);
            var result = NodeManager.NetAddPeer(input[0]);
            $"AddResult: {result}".WriteSuccessLine();
        }

        private void RemovePeer()
        {
            "Parameter: [NetServiceAddress]".WriteSuccessLine();
            var input = CommandOption.InputParameters(1);
            var result = NodeManager.NetRemovePeer(input[0]);
            $"RemoveResult: {result}".WriteSuccessLine();
        }

        private void NetworkInfo()
        {
            var networkInfo = NodeManager.NetworkInfo();
            JsonConvert.SerializeObject(networkInfo, Formatting.Indented).WriteSuccessLine();
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
                "BlockAnalyze",
                "ChainStatus",
                "ContractFileDescriptor",
                "CurrentRoundInformation",
                "TaskQueueStatus",
                "TransactionResult",
                "TransactionResults",
                "TransactionPoolStatus",
                "GetRoundFromBase64",
                "GetMiningSequences",
                "ListAccounts",
                "GetAccountPubicKey",
                "GetPeers",
                "AddPeer",
                "RemovePeer",
                "NetworkInfo"
            };
        }

        public override string[] InputParameters()
        {
            throw new System.NotImplementedException();
        }
    }
}