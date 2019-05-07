using System.Collections.Generic;
using AElf.Automation.Common.Helpers;

namespace AElf.Automation.Common.Contracts
{
    public enum GenesisMethod
    {
        //action
        DeploySystemSmartContract,
        DeploySmartContract,
        UpdateSmartContract,
        ChangeContractOwner,
        
        //view
        CurrentContractSerialNumber,
        GetContractInfo,
        GetContractOwner,
        GetContractHash,
        GetContractAddressByName,
        GetSmartContractRegistrationByAddress
    }

    public enum NameProvider
    {
        ElectionName,
        ProfitName,
        VoteSystemName,
        TokenName,
        TokenConverterName,
        FeeReceiverName,        
        ConsensusName,
    }
    public class GenesisContract : BaseContract<GenesisMethod>
    {
        private Dictionary<NameProvider, Hash> _nameProviders = new Dictionary<NameProvider, Hash>();
        private GenesisContract(IApiHelper apiHelper, string callAddress, string genesisAddress) :
            base(apiHelper, genesisAddress)
        {
            CallAddress = callAddress;
            UnlockAccount(CallAddress);
            InitializeSystemContractName();
        }

        public static GenesisContract GetGenesisContract(IApiHelper ch, string callAddress)
        {
            var chainInfo = new CommandInfo(ApiMethods.GetChainInformation);
            ch.GetChainInformation(chainInfo);
            var genesisContract = ch.GetGenesisContractAddress();
            
            return new GenesisContract(ch, callAddress, genesisContract);
        }

        public Address GetContractAddressByName(NameProvider name)
        {
            var hash = _nameProviders[name];

            var address = CallViewMethod<Address>(GenesisMethod.GetContractAddressByName, hash);

            return address;
        }

        public Address GetContractOwner(Address contractAddress)
        {
            var address = CallViewMethod<Address>(GenesisMethod.GetContractOwner, contractAddress);

            return address;
        }
        
        public Address GetContractOwner(string contractAddress)
        {
            var address = CallViewMethod<Address>(GenesisMethod.GetContractOwner, Address.Parse(contractAddress));

            return address;
        }

        private void InitializeSystemContractName()
        {
            _nameProviders.Add(NameProvider.ElectionName, Hash.FromString("AElf.ContractNames.Election"));
            _nameProviders.Add(NameProvider.ProfitName, Hash.FromString("AElf.ContractNames.Profit"));
            _nameProviders.Add(NameProvider.VoteSystemName, Hash.FromString("AElf.ContractNames.Vote"));
            _nameProviders.Add(NameProvider.TokenName, Hash.FromString("AElf.ContractNames.Token"));
            _nameProviders.Add(NameProvider.TokenConverterName, Hash.FromString("AElf.ContractNames.TokenConverter"));
            _nameProviders.Add(NameProvider.FeeReceiverName, Hash.FromString("AElf.ContractNames.FeeReceiver"));
            _nameProviders.Add(NameProvider.ConsensusName, Hash.FromString("AElf.ContractNames.Consensus"));
        }
    }
}