using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Acs0;
using Acs3;
using AElf.Client.Dto;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Types;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
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

        public INodeManager NodeManager { get; set; }

        public Address DeployContractWithAuthority(string caller, string contractName)
        {
            Logger.Info($"Deploy contract: {contractName}");
            var contractPath = GetContractFilePath(contractName);
            var code = File.ReadAllBytes(contractPath);
            code = GenerateUniqContractCode(code);

            return DeployContractWithAuthority(caller, code);
        }

        public void UpdateContractWithAuthority(string caller, string address, string contractName)
        {
            Logger.Info($"Update contract: {contractName}");
            var contractPath = GetContractFilePath(contractName);
            var code = File.ReadAllBytes(contractPath);
            code = GenerateUniqContractCode(code);

            UpdateContractWithAuthority(caller, address, code);
        }

        private Address DeployContractWithAuthority(string caller, byte[] code)
        {
            var input = new ContractDeploymentInput
            {
                Code = ByteString.CopyFrom(code),
                Category = KernelHelper.DefaultRunnerCategory
            };
            var approveUsers = GetMinApproveMiners();

            var proposalNewContact = _genesis.ProposeNewContract(input, caller);
            var proposalId = ProposalCreated.Parser
                .ParseFrom(proposalNewContact.Logs.First(l => l.Name.Contains(nameof(ProposalCreated))).NonIndexed)
                .ProposalId;
            var proposalHash = ContractProposed.Parser
                .ParseFrom(proposalNewContact.Logs.First(l => l.Name.Contains(nameof(ContractProposed))).NonIndexed)
                .ProposedContractInputHash;

            var releaseInput = new ReleaseContractInput
            {
                ProposalId = proposalId,
                ProposedContractInputHash = proposalHash
            };

            var transactionResult = ApproveAndRelease(releaseInput, approveUsers, caller);
            transactionResult.Status.ShouldBe("MINED");

            var deployProposalId = ProposalCreated.Parser
                .ParseFrom(ByteString.FromBase64(transactionResult.Logs
                    .First(l => l.Name.Contains(nameof(ProposalCreated))).NonIndexed))
                .ProposalId;
            Logger.Info(
                $"Deploy contract proposal info: \n proposal id: {deployProposalId}\n proposal input hash: {proposalHash}");

            if (!CheckProposalStatue(deployProposalId))
            {
                Logger.Error("Contract code didn't pass the code check");
                return null;
            }

            var checkCodeRelease = new ReleaseContractInput
            {
                ProposedContractInputHash = proposalHash,
                ProposalId = deployProposalId
            };

            var release = _genesis.ReleaseCodeCheckedContract(checkCodeRelease, caller);
            release.Status.ShouldBe("MINED");
            var byteString3 =
                ByteString.FromBase64(release.Logs.First(l => l.Name.Contains(nameof(ContractDeployed))).NonIndexed);
            var deployAddress = ContractDeployed.Parser.ParseFrom(byteString3).Address;
            Logger.Info($"Contract deploy passed authority, contract address: {deployAddress}");

            return deployAddress;
        }

        private void UpdateContractWithAuthority(string caller, string address, byte[] code)
        {
            var input = new ContractUpdateInput
            {
                Address = address.ConvertAddress(),
                Code = ByteString.CopyFrom(code)
            };
            var approveUsers = GetMinApproveMiners();

            var proposalUpdateContact = _genesis.ProposeUpdateContract(input, caller);
            var proposalId = ProposalCreated.Parser
                .ParseFrom(proposalUpdateContact.Logs.First(l => l.Name.Contains(nameof(ProposalCreated))).NonIndexed)
                .ProposalId;
            var proposalHash = ContractProposed.Parser
                .ParseFrom(proposalUpdateContact.Logs.First(l => l.Name.Contains(nameof(ContractProposed))).NonIndexed)
                .ProposedContractInputHash;

            var releaseInput = new ReleaseContractInput
            {
                ProposalId = proposalId,
                ProposedContractInputHash = proposalHash
            };

            var transactionResult = ApproveAndRelease(releaseInput, approveUsers, caller);
            var deployProposalId = ProposalCreated.Parser
                .ParseFrom(ByteString.FromBase64(transactionResult.Logs
                    .First(l => l.Name.Contains(nameof(ProposalCreated))).NonIndexed))
                .ProposalId;
            Logger.Info(
                $"Update contract proposal info: \n proposal id: {deployProposalId}\n proposal input hash: {proposalHash}");

            if (!CheckProposalStatue(deployProposalId)) Logger.Error("Contract code didn't pass the code check");

            var checkCodeRelease = new ReleaseContractInput
            {
                ProposedContractInputHash = proposalHash,
                ProposalId = deployProposalId
            };

            var release = _genesis.ReleaseCodeCheckedContract(checkCodeRelease, caller);
            release.Status.ShouldBe("MINED");
            var byteString =
                ByteString.FromBase64(release.Logs.First(l => l.Name.Contains(nameof(CodeUpdated))).NonIndexed);
            var updateAddress = CodeUpdated.Parser.ParseFrom(byteString).Address;
            Logger.Info($"Contract update passed authority, contract address: {updateAddress}");
        }

        public List<string> GetCurrentMiners()
        {
            var currentMiners = _info.GetMinerNodes(_consensus).Select(o => o.Account).ToList();
            return currentMiners;
        }

        public List<string> GetMinApproveMiners()
        {
            var minersCount = _consensus
                .CallViewMethod<PubkeyList>(ConsensusMethod.GetCurrentMinerPubkeyList, new Empty())
                .Pubkeys.Count;
            var organization = _genesis.GetContractDeploymentController().OwnerAddress;
            var voteInfo = _parliament.GetOrganization(organization).ProposalReleaseThreshold.MinimalVoteThreshold;
            var minNumber = (int) (minersCount * 10000 / voteInfo);
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
            _parliament.MinersApproveProposal(proposalId, approveUsers);

            //release
            return _parliament.ReleaseProposal(proposalId, callUser);
        }

        public TransactionResult ExecuteTransactionWithAuthority(string contractAddress, string method, IMessage input,
            string callUser, Address organization = null)
        {
            var parliamentOrganization = organization ?? GetGenesisOwnerAddress();
            var miners = GetCurrentMiners();

            return ExecuteTransactionWithAuthority(contractAddress, method, input, parliamentOrganization, miners,
                callUser);
        }

        private TransactionResultDto ApproveAndRelease(ReleaseContractInput input, IEnumerable<string> approveUsers,
            string callUser)
        {
            //approve
            _parliament.MinersApproveProposal(input.ProposalId, approveUsers);

            //release
            return _genesis.ReleaseApprovedContract(input, callUser);
        }

        private void GetConfigNodeInfo()
        {
            var nodes = NodeInfoHelper.Config;
            nodes.CheckNodesAccount();

            _info = nodes;
        }

        private bool CheckProposalStatue(Hash proposalId)
        {
            var proposal = _parliament.CheckProposal(proposalId);
            var expired = false;
            var stopwatch = Stopwatch.StartNew();
            while (!proposal.ToBeReleased && !expired)
            {
                Thread.Sleep(1000);
                var dateTime = KernelHelper.GetUtcNow();
                proposal = _parliament.CheckProposal(proposalId);
                if (dateTime >= proposal.ExpiredTime) expired = true;
                Console.Write(
                    $"\rCheck proposal status, time using: {CommonHelper.ConvertMileSeconds(stopwatch.ElapsedMilliseconds)}");
            }

            Console.WriteLine();
            return proposal.ToBeReleased;
        }

        private void CheckBpBalance()
        {
            Logger.Info("Check bp balance and transfer for authority.");
            var bps = GetCurrentMiners();
            var primaryToken = NodeManager.GetPrimaryTokenSymbol();
            foreach (var bp in bps.Skip(1))
            {
                var balance = _token.GetUserBalance(bp, primaryToken);
                if (balance < 1000_00000000)
                    _token.TransferBalance(bps[0], bp, 10000_00000000 - balance, primaryToken);
            }
        }

        private string GetContractFilePath(string contractName)
        {
            var localPath = CommonHelper.MapPath("aelf/contracts");
            var defaultPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "aelf", "contracts");
            var contractPaths = new List<string>();
            if (contractName.Contains("\\") || contractName.Contains("/"))
            {
                contractPaths.Add(contractName);
            }
            else if (contractName.Contains(".dll"))
            {
                contractPaths.Add(Path.Combine(localPath, contractName));
                contractPaths.Add(Path.Combine(defaultPath, contractName));
            }
            else
            {
                contractPaths.Add(Path.Combine(localPath, $"{contractName}.dll.patched"));
                contractPaths.Add(Path.Combine(defaultPath, $"{contractName}.dll.patched"));
                contractPaths.Add(Path.Combine(localPath, $"{contractName}.dll"));
                contractPaths.Add(Path.Combine(defaultPath, $"{contractName}.dll"));
            }

            foreach (var path in contractPaths)
            {
                var exist = File.Exists(path);
                if (exist)
                {
                    Logger.Info($"Deploy contract file: {path}");
                    return path;
                }
            }

            throw new FileNotFoundException($"contract file {contractName} not found.");
        }

        private byte[] GenerateUniqContractCode(byte[] code)
        {
            while (true)
            {
                var hash = Hash.FromRawBytes(code);
                var registration =
                    _genesis.CallViewMethod<SmartContractRegistration>(GenesisMethod.GetSmartContractRegistration,
                        hash);
                if (registration.Equals(new SmartContractRegistration())) return code;
                code = CodeInjectHelper.ChangeContractCodeHash(code);
            }
        }
    }
}