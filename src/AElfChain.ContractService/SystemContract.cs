using System.Collections.Generic;
using System.Threading.Tasks;
using AElf;
using AElf.Contracts.Genesis;
using AElf.CSharp.Core;
using AElf.Types;
using AElfChain.AccountService;
using AElfChain.SDK;
using Microsoft.Extensions.Logging;

namespace AElfChain.ContractService
{
    public class SystemContract : ISystemContract
    {
        private Dictionary<SystemContracts, Address> SystemContractAddresses { get; }
        private readonly IAccountManager _accountManager;
        private readonly IApiService _apiService;

        public ILogger Logger { get; set; }

        public SystemContract(IAccountManager accountManager, IApiService apiService, ILoggerFactory loggerFactory)
        {
            SystemContractAddresses = new Dictionary<SystemContracts, Address>();
            _accountManager = accountManager;
            _apiService = apiService;

            Logger = loggerFactory.CreateLogger<SystemContract>();
        }

        public Hash GetContractHashName(SystemContracts contract)
        {
            return SystemContractHashNames[contract];
        }

        public async Task<Address> GetSystemContractAddress(SystemContracts contract)
        {
            var address = SystemContractAddresses[contract];
            if (address != null)
                return address;

            if (contract == SystemContracts.Genesis)
            {
                var chainStatus = await _apiService.GetChainStatusAsync();
                address = AddressHelper.Base58StringToAddress(chainStatus.GenesisContractAddress);
                
                SystemContractAddresses[contract] = address;

                return address;
            }
            else
            {
                var genesisAddress = await GetSystemContractAddress(SystemContracts.Genesis);
                var randomAccount = await _accountManager.GetRandomAccountInfoAsync();
                var genesisStub = GetTestStub<BasicContractZeroContainer.BasicContractZeroStub>(genesisAddress, randomAccount.Formatted);

                var hashName = SystemContractHashNames[contract];
                address = await genesisStub.GetContractAddressByName.CallAsync(hashName);
                SystemContractAddresses[contract] = address;

                return address;
            }
        }

        public TStub GetTestStub<TStub>(Address contract, string caller) where TStub : ContractStubBase
        {
            throw new System.NotImplementedException();
        }

        private Dictionary<SystemContracts, Hash> SystemContractHashNames => GetSystemContractHashNames();

        private Dictionary<SystemContracts, Hash> _systemHashNames;
        private Dictionary<SystemContracts, Hash> GetSystemContractHashNames()
        {
            return _systemHashNames ?? (_systemHashNames = new Dictionary<SystemContracts, Hash>
            {
                {SystemContracts.Election, Hash.FromString("AElf.ContractNames.Election")},
                {SystemContracts.Profit, Hash.FromString("AElf.ContractNames.Profit")},
                {SystemContracts.Vote, Hash.FromString("AElf.ContractNames.Vote")},
                {SystemContracts.Economic, Hash.FromString("AElf.ContractNames.Economic")},
                {SystemContracts.Treasury, Hash.FromString("AElf.ContractNames.Treasury")},
                {SystemContracts.Genesis, Hash.FromString("None")},
                {SystemContracts.ReferendumAuth, Hash.FromString("AElf.ContractNames.ReferendumAuth")},
                {SystemContracts.MultiToken, Hash.FromString("AElf.ContractNames.Token")},
                {SystemContracts.TokenConverter, Hash.FromString("AElf.ContractNames.TokenConverter")},
                {SystemContracts.FeeReceiver, Hash.FromString("AElf.ContractNames.FeeReceiver")},
                {SystemContracts.Consensus, Hash.FromString("AElf.ContractNames.Consensus")},
                {SystemContracts.ParliamentAuth, Hash.FromString("AElf.ContractNames.Parliament")},
                {SystemContracts.CrossChain, Hash.FromString("AElf.ContractNames.CrossChain")},
                {SystemContracts.AssociationAuth, Hash.FromString("AElf.ContractNames.Association")},
                {SystemContracts.Configuration, Hash.FromString("AElf.ContractNames.Configuration")},
            });
        }
    }
}