using System;
using System.Collections.Generic;
using System.Linq;
using AElf.Client.Service;
using AElfChain.Common;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using AElfChain.Console.InputOption;
using AElfChain.Contract;
using Newtonsoft.Json;
using Sharprompt;
using Volo.Abp.Threading;

namespace AElfChain.Console.Commands
{
    public class ChainApiCommand : BaseCommand
    {
        public ChainApiCommand(INodeManager nodeManager, ContractManager contractManager)
            : base(nodeManager, contractManager)
        {
            Logger = Log4NetHelper.GetLogger();
            AutoEngine = new ApiCompletionEngine();
        }

        private ApiCompletionEngine AutoEngine { get; }
        public AElfClient ApiClient => NodeManager.ApiClient;

        public override void RunCommand()
        {
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
                case "ListAccounts":
                    ListAllAccounts();
                    break;
                case "GetAccountPubicKey":
                    GetAccountPublicKey();
                    break;
                case "GetAllPeers":
                    GetAllPeers();
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
            var height = AsyncHelper.RunSync(ApiClient.GetBlockHeightAsync);
            $"Current chain height: {height}".WriteSuccessLine();
        }

        private void GetBlockByHash()
        {
            "Parameter: [BlockHash] [IncludeTransaction]=false".WriteSuccessLine();
            var input = CommandOption.InputParameters(1);
            var hash = input[0];
            var includeTransaction = input.Length != 1 && bool.Parse(input[1]);
            var block = AsyncHelper.RunSync(() => ApiClient.GetBlockByHashAsync(hash, includeTransaction));
            JsonConvert.SerializeObject(block, Formatting.Indented).WriteSuccessLine();
        }

        private void GetBlockByHeight()
        {
            "Parameter: [Height] [IncludeTransaction]=false".WriteSuccessLine();
            var input = CommandOption.InputParameters(1);
            var height = long.Parse(input[0]);
            var includeTransaction = input.Length != 1 && bool.Parse(input[1]);
            var block = AsyncHelper.RunSync(() => ApiClient.GetBlockByHeightAsync(height, includeTransaction));
            JsonConvert.SerializeObject(block, Formatting.Indented).WriteSuccessLine();
        }

        private void GetTransactionPoolStatus()
        {
            var transactionPoolStatusInfo = AsyncHelper.RunSync(ApiClient.GetTransactionPoolStatusAsync);
            JsonConvert.SerializeObject(transactionPoolStatusInfo, Formatting.Indented).WriteSuccessLine();
        }

        private void GetChainStatus()
        {
            var chainInfo = AsyncHelper.RunSync(ApiClient.GetChainStatusAsync);
            JsonConvert.SerializeObject(chainInfo, Formatting.Indented).WriteSuccessLine();
        }

        private void ContractFileDescriptor()
        {
            "Parameter: [ContractAddress] [WithDetails]=false".WriteSuccessLine();
            var input = CommandOption.InputParameters(1);
            var withDetails = input.Length == 2 && bool.Parse(input[1]);

            var descriptorSet = AsyncHelper.RunSync(() => ApiClient.GetContractFileDescriptorSetAsync(input[0]));
            var customContract = new CustomContractSerializer(descriptorSet);
            customContract.GetAllMethodsInfo(withDetails);
        }

        private void GetTaskQueueStatus()
        {
            var taskQueueInfo = AsyncHelper.RunSync(ApiClient.GetTaskQueueStatusAsync);
            JsonConvert.SerializeObject(taskQueueInfo, Formatting.Indented).WriteSuccessLine();
        }

        private void GetTransactionResult()
        {
            "Parameter: [TransactionId]".WriteSuccessLine();
            var input = CommandOption.InputParameters(1);
            var transactionId = input[0];
            var resultDto = AsyncHelper.RunSync(() => ApiClient.GetTransactionResultAsync(transactionId));
            var transactionFee = resultDto.GetTransactionFee();
            var resourceFee = resultDto.GetResourceTokenFee();
            JsonConvert.SerializeObject(resultDto, Formatting.Indented).WriteSuccessLine();
            $"TxFee: {JsonConvert.SerializeObject(transactionFee, Formatting.Indented)}".WriteSuccessLine();
            $"ResourceFee: {JsonConvert.SerializeObject(resourceFee, Formatting.Indented)}".WriteSuccessLine();
        }

        private void GetTransactionResults()
        {
            "Parameter: [BlockHash/BlockHeight] [Offset]=0 [Limit]=10".WriteSuccessLine();
            var input = CommandOption.InputParameters(1);
            var result = long.TryParse(input[0], out var height);
            string hash;
            if (result)
            {
                var block = AsyncHelper.RunSync(() => NodeManager.ApiClient.GetBlockByHeightAsync(height));
                hash = block.BlockHash;
            }
            else
            {
                hash = input[0];
            }

            var offset = input.Length >= 2 ? int.Parse(input[1]) : 0;
            var limit = input.Length == 3 ? int.Parse(input[2]) : 10;
            var resultDto = AsyncHelper.RunSync(() => ApiClient.GetTransactionResultsAsync(hash, offset, limit));
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

        private void GetAccountPublicKey()
        {
            $"Parameter: [Account] [Password]={NodeOption.DefaultPassword}".WriteSuccessLine();
            var input = CommandOption.InputParameters(1);
            var account = input[0];
            var password = input.Length == 2 ? input[1] : NodeOption.DefaultPassword;
            var publicKey = NodeManager.GetAccountPublicKey(account, password);
            $"PublicKey: {publicKey}".WriteSuccessLine();
        }

        private void GetAllPeers()
        {
            var nodes = NodeInfoHelper.Config.Nodes;
            foreach (var node in nodes)
            {
                var nodeManager = new NodeManager(node.Endpoint);
                var peers = nodeManager.NetGetPeers();
                var count = peers.Count;
                $"Name: {node.Name}  Address: {node.Endpoint}, PeerCount: {count}".WriteSuccessLine();
                if (count == 0)
                {
                    System.Console.WriteLine();
                    continue;
                }

                var peerInfo = string.Join("  ", peers.Select(o => o.IpAddress));
                $"Peers: {peerInfo}".WriteSuccessLine();
                System.Console.WriteLine();
            }
        }

        private void GetPeers()
        {
            var input = Prompt.Select("With details", new[] {"yes", "no"});
            var peers = NodeManager.NetGetPeers();
            if (input == "yes")
            {
                JsonConvert.SerializeObject(peers, Formatting.Indented).WriteSuccessLine();
            }
            else
            {
                var count = peers.Count;
                var peerInfo = peers.Select(o => o.IpAddress).ToList();
                $"Total peers count: {count}".WriteSuccessLine();
                if (count == 0) return;
                for (var i = 0; i < peerInfo.Count; i++) $"{i + 1:00}. {peerInfo[i]}".WriteSuccessLine();
            }
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
                "ChainStatus",
                "ContractFileDescriptor",
                "TaskQueueStatus",
                "TransactionResult",
                "TransactionResults",
                "TransactionPoolStatus",
                "ListAccounts",
                "GetAccountPubicKey",
                "GetAllPeers",
                "GetPeers",
                "AddPeer",
                "RemovePeer",
                "NetworkInfo"
            };
        }

        public override string[] InputParameters()
        {
            throw new NotImplementedException();
        }
    }
}