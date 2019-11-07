using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Acs0;
using AElf.Automation.Common.Contracts;
using AElf.Automation.Common.Helpers;
using AElf.Automation.Common.Utils;
using AElf.Kernel;
using AElf.Types;
using Google.Protobuf;
using log4net;
using Shouldly;

namespace AElf.Automation.Common.Managers
{
    public class AuthorityManager
    {
        private static readonly ILog Logger = Log4NetHelper.GetLogger();
        private readonly ConsensusContract _consensus;
        private readonly TokenContract _token;
        private readonly GenesisContract _genesis;
        private readonly ParliamentAuthContract _parliament;
        private NodesInfo _info;

        public AuthorityManager(INodeManager nodeManager, string caller = "")
        {
            GetConfigNodeInfo();
            _genesis = GenesisContract.GetGenesisContract(nodeManager, caller);
            _consensus = _genesis.GetConsensusContract();
            _token = _genesis.GetTokenContract();
            _parliament = _genesis.GetParliamentAuthContract();
            
            CheckBpBalance();
        }

        public Address DeployContractWithAuthority(string caller, string contractName)
        {
            Logger.Info($"Deploy contract: {contractName}");
            var fileName = contractName.Contains(".dll") ? contractName : $"{contractName}.dll";
            var contractPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "aelf", "contracts");
            var code = File.ReadAllBytes(Path.Combine(contractPath, fileName));

            return DeployContractWithAuthority(caller, code);
        }
        
        public void UpdateContractWithAuthority(string caller, string address, string contractName)
        {
            Logger.Info($"Update contract: {contractName}");
            var fileName = contractName.Contains(".dll") ? contractName : $"{contractName}.dll";
            var contractPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "aelf", "contracts");
            var code = File.ReadAllBytes(Path.Combine(contractPath, fileName));

            UpdateContractWithAuthority(caller, address, code);
        }

        private Address DeployContractWithAuthority(string caller, byte[] code)
        {
            var input = new ContractDeploymentInput
            {
                Code = ByteString.CopyFrom(code),
                Category = KernelConstants.CodeCoverageRunnerCategory
            };
            var organizationAddress = _parliament.GetGenesisOwnerAddress();
            var currentMiners = _info.GetMinerNodes(_consensus).Select(o => o.Account).ToList();

            var transactionResult = ExecuteTransactionWithAuthority(_genesis.ContractAddress,
                nameof(GenesisMethod.DeploySmartContract),
                input, organizationAddress, currentMiners, caller);
            var byteString = transactionResult.Logs.Last().NonIndexed;
            var address = ContractDeployed.Parser.ParseFrom(byteString).Address;
            Logger.Info($"Contract deploy passed authority, contract address: {address}");

            return address;
        }
        
        private void UpdateContractWithAuthority(string caller, string address, byte[] code)
        {
            var input = new ContractUpdateInput
            {
                Address = address.ConvertAddress(),
                Code = ByteString.CopyFrom(code)
            };
            var organizationAddress = _parliament.GetGenesisOwnerAddress();
            var currentMiners = _info.GetMinerNodes(_consensus).Select(o => o.Account).ToList();

            var transactionResult = ExecuteTransactionWithAuthority(_genesis.ContractAddress,
                nameof(GenesisMethod.UpdateSmartContract),
                input, organizationAddress, currentMiners, caller);
            transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        public List<string> GetCurrentMiners()
        {
            var currentMiners = _info.GetMinerNodes(_consensus).Select(o => o.Account).ToList();
            return currentMiners;
        }

        public Address GetGenesisOwnerAddress()
        {
            return _parliament.GetGenesisOwnerAddress();
        }

        public TransactionResult ExecuteTransactionWithAuthority(string contractAddress, string method, IMessage input,
            Address organizationAddress, IEnumerable<string> approveUsers, string callUser)
        {
            //create proposal
            var proposalId = _parliament.CreateProposal(contractAddress,
                method, input,
                organizationAddress, callUser);

            //approve
            foreach (var account in approveUsers) _parliament.ApproveProposal(proposalId, account);

            //release
            return _parliament.ReleaseProposal(proposalId, callUser);
        }

        private void GetConfigNodeInfo()
        {
            var nodes = NodeInfoHelper.Config;
            nodes.CheckNodesAccount();

            _info = nodes;
        }

        private void CheckBpBalance()
        {
            Logger.Info("Check bp balance and transfer for authority.");
            var bps = GetCurrentMiners();
            if (NodeOption.IsMainChain)
            {
                foreach (var bp in bps.Skip(1))
                {
                    var balance = _token.GetUserBalance(bp);
                    if (balance < 10000_00000000)
                        _token.TransferBalance(bps[0], bp, 100000_00000000 - balance, NodeOption.ChainToken);
                }
            }
            else
            {
                foreach (var bp in bps)
                {
                    var issuer = NodeInfoHelper.Config.Nodes.First().Account;
                    var balance = _token.GetUserBalance(bp);
                    if (balance < 100000_00000000)
                        _token.IssueBalance(issuer, bp, 100000_00000000 - balance, NodeOption.ChainToken);
                }
            }
        }
    }
}