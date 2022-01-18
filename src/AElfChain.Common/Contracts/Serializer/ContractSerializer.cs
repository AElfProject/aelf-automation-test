using System.Collections.Generic;
using AElf.Standards.ACS0;
using AElf.Standards.ACS1;
using AElf.Standards.ACS2;
using AElf.Standards.ACS3;
using AElf.Standards.ACS8;
using AElf.Contracts.Association;
using AElf.Contracts.Configuration;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.CrossChain;
using AElf.Contracts.Election;
using AElf.Contracts.Genesis;
using AElf.Contracts.MultiToken;
using AElf.Contracts.Parliament;
using AElf.Contracts.Profit;
using AElf.Contracts.Referendum;
using AElf.Contracts.TokenConverter;
using AElf.Contracts.Treasury;
using AElf.Contracts.Vote;
using AElf.Standards.ACS4;
using AElf.Standards.ACS6;
using AElf.Standards.ACS7;
using Google.Protobuf.Reflection;

namespace AElfChain.Common.Contracts.Serializer
{
    public class ContractSerializer
    {
        public ContractSerializer()
        {
            ContractInfos = new Dictionary<NameProvider, ContractInfo>();
        }

        public Dictionary<NameProvider, ContractInfo> ContractInfos { get; set; }

        public static Dictionary<NameProvider, List<ServiceDescriptor>> SystemContractsDescriptors =>
            new Dictionary<NameProvider, List<ServiceDescriptor>>
            {
                {
                    NameProvider.Genesis,
                    new List<ServiceDescriptor>
                    {
                        ACS0Container.Descriptor, MethodFeeProviderContractContainer.Descriptor,
                        BasicContractZeroContainer.Descriptor
                    }
                },
                {
                    NameProvider.Election,
                    new List<ServiceDescriptor>
                        {MethodFeeProviderContractContainer.Descriptor, ElectionContractContainer.Descriptor}
                },
                {
                    NameProvider.Profit,
                    new List<ServiceDescriptor>
                        {MethodFeeProviderContractContainer.Descriptor, ProfitContractContainer.Descriptor}
                },
                {
                    NameProvider.Vote,
                    new List<ServiceDescriptor>
                        {MethodFeeProviderContractContainer.Descriptor, VoteContractContainer.Descriptor}
                },
                {
                    NameProvider.Treasury,
                    new List<ServiceDescriptor>
                        {MethodFeeProviderContractContainer.Descriptor, TreasuryContractContainer.Descriptor}
                },
                {
                    NameProvider.Token,
                    new List<ServiceDescriptor>
                    {
                        MethodFeeProviderContractContainer.Descriptor, ACS2BaseContainer.Descriptor,
                        TokenContractContainer.Descriptor,
                        TokenContractImplContainer.Descriptor
                    }
                },
                {
                    NameProvider.TokenConverter,
                    new List<ServiceDescriptor>
                        {MethodFeeProviderContractContainer.Descriptor, TokenConverterContractContainer.Descriptor}
                },
                {
                    NameProvider.Consensus,
                    new List<ServiceDescriptor>
                    {
                        MethodFeeProviderContractContainer.Descriptor, ConsensusContractContainer.Descriptor,
                        RandomNumberProviderContractContainer.Descriptor, AEDPoSContractContainer.Descriptor
                    }
                },
                {
                    NameProvider.ParliamentAuth,
                    new List<ServiceDescriptor>
                    {
                        MethodFeeProviderContractContainer.Descriptor, AuthorizationContractContainer.Descriptor,
                        ParliamentContractContainer.Descriptor
                    }
                },
                {
                    NameProvider.CrossChain,
                    new List<ServiceDescriptor>
                    {
                        MethodFeeProviderContractContainer.Descriptor, ACS7Container.Descriptor,
                        CrossChainContractContainer.Descriptor
                    }
                },
                {
                    NameProvider.AssociationAuth,
                    new List<ServiceDescriptor>
                    {
                        MethodFeeProviderContractContainer.Descriptor, AuthorizationContractContainer.Descriptor,
                        AssociationContractContainer.Descriptor
                    }
                },
                {
                    NameProvider.Configuration,
                    new List<ServiceDescriptor>
                        {MethodFeeProviderContractContainer.Descriptor, ConfigurationContainer.Descriptor}
                },
                {
                    NameProvider.ReferendumAuth,
                    new List<ServiceDescriptor>
                    {
                        MethodFeeProviderContractContainer.Descriptor, AuthorizationContractContainer.Descriptor,
                        ReferendumContractContainer.Descriptor
                    }
                }
            };

        public ContractInfo GetContractInfo(NameProvider name)
        {
            if (ContractInfos.ContainsKey(name))
                return ContractInfos[name];

            var descriptor = SystemContractsDescriptors[name];
            var contractInfo = new ContractInfo(descriptor);
            ContractInfos.Add(name, contractInfo);

            return contractInfo;
        }
    }
}