using System.Collections.Generic;
using System.Linq;
using AElf.Automation.Common.Contracts;
using AElf.Contracts.AssociationAuth;
using AElf.Contracts.Configuration;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.CrossChain;
using AElf.Contracts.Election;
using AElf.Contracts.MultiToken;
using AElf.Contracts.ParliamentAuth;
using AElf.Contracts.Profit;
using AElf.Contracts.ReferendumAuth;
using AElf.Contracts.TestContract.Performance;
using AElf.Contracts.TokenConverter;
using AElf.Contracts.Treasury;
using AElf.Contracts.Vote;
using Google.Protobuf.Reflection;

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

        public static Dictionary<NameProvider, ServiceDescriptor> SystemContractsDescriptors =>
            new Dictionary<NameProvider, ServiceDescriptor>
            {
                {NameProvider.Election, ElectionContractContainer.Descriptor},
                {NameProvider.Profit, ProfitContractContainer.Descriptor},
                {NameProvider.Vote, VoteContractContainer.Descriptor},
                {NameProvider.Treasury, TreasuryContractContainer.Descriptor},
                {NameProvider.Token, TokenContractContainer.Descriptor},
                {NameProvider.TokenConverter, TokenConverterContractContainer.Descriptor},
                {NameProvider.Consensus, AEDPoSContractContainer.Descriptor},
                {NameProvider.ParliamentAuth, ParliamentAuthContractContainer.Descriptor},
                {NameProvider.CrossChain, CrossChainContractContainer.Descriptor},
                {NameProvider.AssociationAuth, AssociationAuthContractContainer.Descriptor},
                {NameProvider.Configuration, ConfigurationContainer.Descriptor},
                {NameProvider.ReferendumAuth, ReferendumAuthContractContainer.Descriptor},
                
                {NameProvider.TestPerformance, PerformanceContractContainer.Descriptor}
            };
    }
}