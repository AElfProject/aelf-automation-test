using System.Collections.Generic;
using Acs0;
using AElf.Automation.Common.Helpers;
using AElf.Automation.Common.WebApi.Dto;
using AElf.Types;
using Google.Protobuf;

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
        ParliamentName,
        CrossChainName,
        AssciationName,
        Configuration
    }
    
    public class GenesisContract : BaseContract<GenesisMethod>
    {
        public static readonly Dictionary<NameProvider, Hash> NameProviderInfos = new Dictionary<NameProvider, Hash>();

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
            Logger.WriteInfo($"Genesis contract Address: {genesisContract}");

            return new GenesisContract(ch, callAddress, genesisContract);
        }

        public bool UpdateContract(string account, string contractAddress, string contractFileName)
        {
            var contractReader = new SmartContractReader();
            var codeArray = contractReader.Read(contractFileName);

            var contractOwner = GetContractOwner(contractAddress);
            if (contractOwner.GetFormatted() != account)
                Logger.WriteError("Account have no permission to update.");

            SetAccount(account);
            var txResult = ExecuteMethodWithResult(GenesisMethod.UpdateSmartContract, new ContractUpdateInput
            {
                Address = Address.Parse(contractAddress),
                Code = ByteString.CopyFrom(codeArray)
            });
            if (!(txResult.InfoMsg is TransactionResultDto txDto)) return false;
            return txDto.Status == "Mined";
        }

        public Address GetContractAddressByName(NameProvider name)
        {
            var hash = NameProviderInfos[name];

            var address = CallViewMethod<Address>(GenesisMethod.GetContractAddressByName, hash);
            var addString = address != new Address() ? address.GetFormatted() : "null";
            Logger.WriteInfo($"{name.ToString().Replace("Name", "")} contract address: {addString}");

            return address;
        }

        public Address GetContractOwner(Address contractAddress)
        {
            var address = CallViewMethod<Address>(GenesisMethod.GetContractOwner, contractAddress);

            return address;
        }

        public Address GetContractOwner(string contractAddress)
        {
            return GetContractOwner(Address.Parse(contractAddress));
        }

        private void InitializeSystemContractName()
        {
            NameProviderInfos.Add(NameProvider.ElectionName, Hash.FromString("AElf.ContractNames.Election"));
            NameProviderInfos.Add(NameProvider.ProfitName, Hash.FromString("AElf.ContractNames.Profit"));
            NameProviderInfos.Add(NameProvider.VoteSystemName, Hash.FromString("AElf.ContractNames.Vote"));
            NameProviderInfos.Add(NameProvider.TokenName, Hash.FromString("AElf.ContractNames.Token"));
            NameProviderInfos.Add(NameProvider.TokenConverterName, Hash.FromString("AElf.ContractNames.TokenConverter"));
            NameProviderInfos.Add(NameProvider.FeeReceiverName, Hash.FromString("AElf.ContractNames.FeeReceiver"));
            NameProviderInfos.Add(NameProvider.ConsensusName, Hash.FromString("AElf.ContractNames.Consensus"));
            NameProviderInfos.Add(NameProvider.ParliamentName, Hash.FromString("AElf.ContractsName.Parliament"));
            NameProviderInfos.Add(NameProvider.CrossChainName, Hash.FromString("AElf.ContractNames.CrossChain"));
            NameProviderInfos.Add(NameProvider.AssciationName, Hash.FromString("AElf.ContractNames.Association"));
            NameProviderInfos.Add(NameProvider.Configuration, Hash.FromString("AElf.Contracts.Configuration"));
        }
    }
}