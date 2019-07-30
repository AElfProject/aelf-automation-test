using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Acs0;
using AElf.Automation.Common.Contracts;
using AElf.Automation.Common.Helpers;
using AElf.Automation.Common.WebApi;
using AElf.Kernel;
using AElf.Types;
using Google.Protobuf;
using log4net;

namespace AElf.Automation.Common.OptionManagers.Authority
{
    public class AuthorityHelper
    {
        private readonly ILog Logger = Log4NetHelper.GetLogger();
        private NodesInfo _info;
        private IApiService _apiService;
        private GenesisContract _genesis;
        private ConsensusContract _consensus;
        private ParliamentAuthContract _parliament;

        public AuthorityHelper(string serviceUrl, string caller)
        {
            var apiHelper = new WebApiHelper(serviceUrl);
            _apiService = apiHelper.ApiService;

            GetConfigNodeInfo();

            _genesis = GenesisContract.GetGenesisContract(apiHelper, caller);

            var consensusAddress = _genesis.GetContractAddressByName(NameProvider.ConsensusName);
            _consensus = new ConsensusContract(apiHelper, caller, consensusAddress.GetFormatted());

            var parliamentAddress = _genesis.GetContractAddressByName(NameProvider.ParliamentName);
            _parliament = new ParliamentAuthContract(apiHelper, caller, parliamentAddress.GetFormatted());
        }

        public Address DeployContractWithAuthority(string caller, string contractName)
        {
            var fileName = contractName.Contains(".dll") ? contractName : $"{contractName}.dll";
            var contractPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "aelf", "contracts");
            var code = File.ReadAllBytes(Path.Combine(contractPath, fileName));
            
            return DeployContractWithAuthority(caller, code);
        }

        public Address DeployContractWithAuthority(string caller, byte[] code)
        {
            var input = new ContractDeploymentInput
            {
                Code = ByteString.CopyFrom(code),
                Category = KernelConstants.CodeCoverageRunnerCategory
            };
            var organizationAddress = _parliament.GetGenesisOwnerAddress();
            var currentMiners = _info.GetMinerNodes(_consensus).Select(o=>o.Account).ToList();

            var transactionResult = ExecuteTransactionWithAuthority(_genesis.ContractAddress, nameof(GenesisMethod.DeploySmartContract),
                input, organizationAddress, currentMiners, caller);
            var byteString = transactionResult.Logs.First().NonIndexed;
            return ContractDeployed.Parser.ParseFrom(byteString).Address;
        }

        public TransactionResult ExecuteTransactionWithAuthority(string contractAddress, string method, IMessage input,
            Address organizationAddress, IEnumerable<string> approveUsers, string callUser)
        {
            //create proposal
            var proposalId = _parliament.CreateProposal(contractAddress,
                method, input,
                organizationAddress, callUser);
            
            //approve
            foreach (var account in approveUsers)
            {
                _parliament.ApproveProposal(proposalId, account);
            }

            //release
            return _parliament.ReleaseProposal(proposalId, callUser);
        }

        private void GetConfigNodeInfo()
        {
            var nodes = NodeInfoHelper.Config;
            nodes.CheckNodesAccount();

            _info = nodes;
        }
    }
}