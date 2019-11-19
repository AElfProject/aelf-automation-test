using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Acs0;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Kernel;
using AElf.Types;
using AElfChain.Common.Contracts;
using AElfChain.Common.Helpers;
using AElfChain.Common.Utils;
using AElfChain.SDK.Models;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using log4net;
using Shouldly;

namespace AElfChain.Common.Managers
{
    public class AuthorityManager
    {
        private static readonly ILog Logger = Log4NetHelper.GetLogger();
        private readonly ConsensusContract _consensus;
        private readonly GenesisContract _genesis;
        private readonly ParliamentAuthContract _parliament;
        private readonly TokenContract _token;
        private NodesInfo _info;

        public INodeManager NodeManager { get; set; }

        public AuthorityManager(INodeManager nodeManager, string caller = "")
        {
            GetConfigNodeInfo();
            NodeManager = nodeManager;
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
            var approveUsers = GetMinApproveMiners();

            var transactionResult = ExecuteTransactionWithAuthority(_genesis.ContractAddress,
                nameof(GenesisMethod.DeploySmartContract),
                input, organizationAddress, approveUsers, caller);
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
            var approveUsers = GetMinApproveMiners();

            var transactionResult = ExecuteTransactionWithAuthority(_genesis.ContractAddress,
                nameof(GenesisMethod.UpdateSmartContract),
                input, organizationAddress, approveUsers, caller);
            transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        public List<string> GetCurrentMiners()
        {
            var currentMiners = _info.GetMinerNodes(_consensus).Select(o => o.Account).ToList();
            return currentMiners;
        }

        public List<string> GetMinApproveMiners()
        {
            var minersCount = _consensus.CallViewMethod<PubkeyList>(ConsensusMethod.GetTermSnapshot, new Empty())
                .Pubkeys.Count;
            var minNumber = minersCount * 2 / 3 + 1;
            var currentMiners = GetCurrentMiners();
            return currentMiners.Take(minNumber).ToList();
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
            var primaryToken = NodeManager.GetPrimaryTokenSymbol();
            foreach (var bp in bps.Skip(1))
            {
                var balance = _token.GetUserBalance(bp);
                if (balance < 1000_00000000)
                    _token.TransferBalance(bps[0], bp, 10000_00000000 - balance, primaryToken);
            }
        }
    }
}