using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Acs0;
using Acs3;
using AElf;
using AElf.Automation.Common.Contracts;
using AElf.Automation.Common.Helpers;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.ParliamentAuth;
using AElf.Kernel;
using AElf.Sdk.CSharp;
using AElf.Types;
using AElfChain.AccountService;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Shouldly;

namespace AElfChain.ContractService
{
    public interface IAuthorityManager
    {
        Task<Address> DeployContractWithAuthority(AccountInfo accountInfo, string contractName);
        
        Task<TransactionResult> ExecuteTransactionWithAuthority(Address contract, string method, IMessage input,
            Address organizationAddress, IEnumerable<string> approveUsers, AccountInfo accountInfo);
    }
    
    public class AuthorityManager : IAuthorityManager
    {
        public ILogger Logger { get; set; }
        private readonly ISystemContract _systemContract;
        private readonly IAccountManager _accountManager;
        private readonly SmartContractReader _contractReader; 

        public AuthorityManager(ISystemContract systemContract, IAccountManager accountManager, SmartContractReader contractReader, ILoggerFactory loggerFactory)
        {
            Logger = loggerFactory.CreateLogger<AuthorityManager>();
            _contractReader = contractReader;
            _accountManager = accountManager;
            _systemContract = systemContract;
        }
        
        public async Task<Address> DeployContractWithAuthority(AccountInfo accountInfo, string contractName)
        {
            var parliamentContract = await _systemContract.GetSystemContractAddressAsync(SystemContracts.ParliamentAuth);
            var parliament =
                _systemContract.GetTestStub<ParliamentAuthContractContainer.ParliamentAuthContractStub>(parliamentContract,
                    accountInfo);
            
            var code = _contractReader.Read(contractName);
            var input = new ContractDeploymentInput
            {
                Code = ByteString.CopyFrom(code),
                Category = KernelConstants.CodeCoverageRunnerCategory
            };
            var organizationAddress = await parliament.GetGenesisOwnerAddress.CallAsync(new Empty());
            var currentMiners = await GetConfigNodeInfo(accountInfo);
            Logger.LogInformation($"Current miners: {string.Join(",", currentMiners)}");
            var gensisContract = await _systemContract.GetSystemContractAddressAsync(SystemContracts.Genesis);
            var transactionResult = await ExecuteTransactionWithAuthority(gensisContract,
                nameof(GenesisMethod.DeploySmartContract),
                input, organizationAddress, currentMiners, accountInfo);
            var byteString = transactionResult.Logs.First().NonIndexed;
            var address = ContractDeployed.Parser.ParseFrom(byteString).Address;
            Logger.LogInformation($"Contract deploy passed authority, contract address: {address}");

            return address;
        }

        public async Task<TransactionResult> ExecuteTransactionWithAuthority(Address contract, string method, IMessage input, Address organizationAddress,
            IEnumerable<string> approveUsers, AccountInfo accountInfo)
        {
            var parliamentContract = await _systemContract.GetSystemContractAddressAsync(SystemContracts.ParliamentAuth);
            var parliament =
                _systemContract.GetTestStub<ParliamentAuthContractContainer.ParliamentAuthContractStub>(
                    parliamentContract, accountInfo);
            
            //create proposal
            var proposalTransactionResult = await parliament.CreateProposal.SendAsync(new CreateProposalInput
            {
                ContractMethodName = method,
                ExpiredTime = TimestampHelper.GetUtcNow().AddHours(1),
                OrganizationAddress = organizationAddress,
                Params = input.ToByteString(),
                ToAddress = accountInfo.Account
            });
            var proposalId = HashHelper.HexStringToHash( proposalTransactionResult.TransactionResult.ReadableReturnValue);
            Logger.LogInformation($"Proposal Id: {proposalId}");

            //approve
            foreach (var account in approveUsers)
            {
                var approveInfo = await _accountManager.GetAccountInfoAsync(account);
                var approveParliament =
                    _systemContract.GetTestStub<ParliamentAuthContractContainer.ParliamentAuthContractStub>(
                        parliamentContract, approveInfo);
                var approveResult = await approveParliament.Approve.SendAsync(new ApproveInput
                {
                    ProposalId = proposalId
                });
                approveResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            }

            //release
            parliament =
                _systemContract.GetTestStub<ParliamentAuthContractContainer.ParliamentAuthContractStub>(
                    parliamentContract, accountInfo);
            var result = await parliament.Release.SendAsync(proposalId);
            
            return result.TransactionResult;
        }
        
        private async Task<List<string>> GetConfigNodeInfo(AccountInfo accountInfo)
        {
            var nodes = NodeInfoHelper.Config;
            nodes.CheckNodesAccount();
            
            var consensusContract = await _systemContract.GetSystemContractAddressAsync(SystemContracts.Consensus);
            var consensus =
                _systemContract.GetTestStub<AEDPoSContractContainer.AEDPoSContractStub>(consensusContract, accountInfo);
            var minerList = (await consensus.GetCurrentMinerList.CallAsync(new Empty()))
                    .Pubkeys.Select(o=>o.ToByteArray().ToHex());
            
            return nodes.Nodes.
                    Where(o => minerList.Contains(o.PublicKey)).
                    Select(o =>o.Account).ToList();
        }
    }
}