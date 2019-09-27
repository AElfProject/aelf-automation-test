using System;
using System.Linq;
using System.Threading.Tasks;
using AElf.Automation.Common.Contracts;
using AElf.Automation.Common.Managers;
using AElf.Runtime.CSharp;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using Volo.Abp.Threading;

namespace AElf.Automation.ContractsTesting
{
    public class ProtoFileTest
    {
        private readonly INodeManager _nodeManager;
        public ProtoFileTest(INodeManager nodeManager)
        {
            _nodeManager = nodeManager;
            
            AsyncHelper.RunSync(SerializeProtoFileInfo);
        }

        public async Task SerializeProtoFileInfo()
        {
            var genesis = _nodeManager.GetGenesisContract();
            var token = genesis.GetTokenContract();

            var fileDescriptorBytes = await _nodeManager.ApiService.GetContractFileDescriptorSetAsync(token.ContractAddress);
            var fileDescriptorSet = FileDescriptorSet.Parser.ParseFrom(fileDescriptorBytes);

            var fileDescriptor = FileDescriptor.BuildFromByteStrings(fileDescriptorSet.File.Where(o=>o.Length !=0));

        }
    }
}