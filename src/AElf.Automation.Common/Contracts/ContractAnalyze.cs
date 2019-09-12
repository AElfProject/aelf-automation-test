using System;
using System.Threading.Tasks;
using AElf.Automation.Common.Helpers;
using AElf.Automation.Common.Managers;
using AElf.Runtime.CSharp;
using AElfChain.SDK;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using log4net;

namespace AElf.Automation.Common.Contracts
{
    public class ContractAnalyze
    {
        private INodeManager _nodeManager;
        private IApiService _apiService => _nodeManager.ApiService;
        private string _account;

        public ILog Logger = Log4NetHelper.GetLogger();

        public ContractAnalyze(INodeManager nodeManager)
        {
            _nodeManager = nodeManager;
            _account = _nodeManager.GetRandomAccount();
        }
        
        public async Task AnalyzeContractFileDescriptors()
        {
            var genesis = GenesisContract.GetGenesisContract(_nodeManager, _account);
            var gensisDescriptor = await _apiService.GetContractFileDescriptorSetAsync(genesis.ContractAddress);
            var descriptorSet = FileDescriptorSet.Parser.ParseFrom(ByteString.CopyFrom(gensisDescriptor));
            foreach (var descriptorData in descriptorSet.File)
            {
                try
                {
                    var file = FileDescriptor.BuildFromByteStrings(new []{descriptorData});
                    var reg = TypeRegistry.FromFiles(file);
                    Logger.Info("success.");
                }
                catch (Exception e)
                {
                    Logger.Error(e.Message);
                }
            }
        }
    }
}