using System.Collections.Generic;
using Acs0;
using Acs1;
using Acs2;
using Acs3;
using Acs4;
using Acs6;
using Acs7;
using AElf.Automation.Common.Contracts;
using AElf.Contracts.AssociationAuth;
using AElf.Contracts.Configuration;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.CrossChain;
using AElf.Contracts.Election;
using AElf.Contracts.Genesis;
using AElf.Contracts.MultiToken;
using AElf.Contracts.ParliamentAuth;
using AElf.Contracts.Profit;
using AElf.Contracts.ReferendumAuth;
using AElf.Contracts.TestContract.Performance;
using AElf.Contracts.TokenConverter;
using AElf.Contracts.Treasury;
using AElf.Contracts.Vote;
using Google.Protobuf.Reflection;
using ExecutionAcs5 = AElf.Kernel.SmartContract.ExecutionPluginForAcs5.Tests.TestContract;
using ExecutionAcs8 = AElf.Kernel.SmartContract.ExecutionPluginForAcs8.Tests.TestContract;


namespace AElf.Automation.Common.ContractSerializer
{
    public class ContractHandler
    {
        public Dictionary<NameProvider, ContractInfo> ContractInfos { get; set; }

        public ContractHandler()
        {
            ContractInfos = new Dictionary<NameProvider, ContractInfo>();
        }

        public ContractInfo GetContractInfo(NameProvider name)
        {
            if (ContractInfos.ContainsKey(name))
                return ContractInfos[name];

            var descriptor = SystemContractsDescriptors[name];
            var contractInfo = new ContractInfo(descriptor);
            ContractInfos.Add(name, contractInfo);
            
            return contractInfo;
        }

        public static Dictionary<NameProvider, List<ServiceDescriptor>> SystemContractsDescriptors =>
            new Dictionary<NameProvider, List<ServiceDescriptor>>
            {
                {NameProvider.Genesis, new List<ServiceDescriptor>{ACS0Container.Descriptor, FeeChargedContractContainer.Descriptor, BasicContractZeroContainer.Descriptor}},
                {NameProvider.Election, new List<ServiceDescriptor>{FeeChargedContractContainer.Descriptor, ElectionContractContainer.Descriptor}},
                {NameProvider.Profit, new List<ServiceDescriptor>{FeeChargedContractContainer.Descriptor, ProfitContractContainer.Descriptor}},
                {NameProvider.Vote, new List<ServiceDescriptor>{FeeChargedContractContainer.Descriptor, VoteContractContainer.Descriptor}},
                {NameProvider.Treasury, new List<ServiceDescriptor>{FeeChargedContractContainer.Descriptor, TreasuryContractContainer.Descriptor}},
                {NameProvider.Token, new List<ServiceDescriptor>{FeeChargedContractContainer.Descriptor, ACS2BaseContainer.Descriptor, TokenContractContainer.Descriptor}},
                {NameProvider.TokenConverter, new List<ServiceDescriptor>{FeeChargedContractContainer.Descriptor, TokenConverterContractContainer.Descriptor}},
                {NameProvider.Consensus, new List<ServiceDescriptor>{FeeChargedContractContainer.Descriptor, ConsensusContractContainer.Descriptor, RandomNumberProviderContractContainer.Descriptor, AEDPoSContractContainer.Descriptor}},
                {NameProvider.ParliamentAuth, new List<ServiceDescriptor>{FeeChargedContractContainer.Descriptor, AuthorizationContractContainer.Descriptor, ParliamentAuthContractContainer.Descriptor}},
                {NameProvider.CrossChain, new List<ServiceDescriptor>{FeeChargedContractContainer.Descriptor, ACS7Container.Descriptor, CrossChainContractContainer.Descriptor}},
                {NameProvider.AssociationAuth, new List<ServiceDescriptor>{FeeChargedContractContainer.Descriptor, AuthorizationContractContainer.Descriptor, AssociationAuthContractContainer.Descriptor}},
                {NameProvider.Configuration, new List<ServiceDescriptor>{FeeChargedContractContainer.Descriptor, ConfigurationContainer.Descriptor}},
                {NameProvider.ReferendumAuth, new List<ServiceDescriptor>{FeeChargedContractContainer.Descriptor, AuthorizationContractContainer.Descriptor, ReferendumAuthContractContainer.Descriptor}},
                
                {NameProvider.TestPerformance, new List<ServiceDescriptor>{PerformanceContractContainer.Descriptor}},
                {NameProvider.ExecutionAcs5, new List<ServiceDescriptor>{ExecutionAcs5.ContractContainer.Descriptor}},
                {NameProvider.ExecutionAcs8, new List<ServiceDescriptor>{ExecutionAcs8.ContractContainer.Descriptor}}
            };
    }
}