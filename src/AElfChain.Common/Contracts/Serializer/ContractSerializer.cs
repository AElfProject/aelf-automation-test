using System.Collections.Generic;
using Acs0;
using Acs1;
using Acs2;
using Acs3;
using Acs4;
using Acs6;
using Acs7;
using Acs8;
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
using AElf.Contracts.TestContract.A;
using AElf.Contracts.TestContract.B;
using AElf.Contracts.TestContract.BasicFunction;
using AElf.Contracts.TestContract.BasicSecurity;
using AElf.Contracts.TestContract.BasicUpdate;
using AElf.Contracts.TestContract.C;
using AElf.Contracts.TestContract.Events;
using AElf.Contracts.TestContract.Performance;
using AElf.Contracts.TestContract.TransactionFees;
using AElf.Contracts.TokenConverter;
using AElf.Contracts.Treasury;
using AElf.Contracts.Vote;
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
                },
                {
                    NameProvider.TestBasicFunction,
                    new List<ServiceDescriptor> {BasicFunctionContractContainer.Descriptor}
                },
                {
                    NameProvider.TestUpdateFunction,
                    new List<ServiceDescriptor> {BasicUpdateContractContainer.Descriptor}
                },
                {
                    NameProvider.TestBasicSecurity,
                    new List<ServiceDescriptor> {BasicSecurityContractContainer.Descriptor}
                },
                {NameProvider.TestPerformance, new List<ServiceDescriptor> {PerformanceContractContainer.Descriptor}},
                {
                    NameProvider.TestTransactionFees,
                    new List<ServiceDescriptor>
                    {
                        MethodFeeProviderContractContainer.Descriptor, ResourceConsumptionContractContainer.Descriptor,
                        TransactionFeesContractContainer.Descriptor
                    }
                },
                {
                    NameProvider.TestEvents,
                    new List<ServiceDescriptor>
                        {MethodFeeProviderContractContainer.Descriptor, EventsContractContainer.Descriptor}
                },
                {
                    NameProvider.TestA,
                    new List<ServiceDescriptor>
                        {MethodFeeProviderContractContainer.Descriptor, AContractContainer.Descriptor}
                },
                {
                    NameProvider.TestB,
                    new List<ServiceDescriptor>
                        {MethodFeeProviderContractContainer.Descriptor, BContractContainer.Descriptor}
                },
                {
                    NameProvider.TestC,
                    new List<ServiceDescriptor>
                        {MethodFeeProviderContractContainer.Descriptor, CContractContainer.Descriptor}
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