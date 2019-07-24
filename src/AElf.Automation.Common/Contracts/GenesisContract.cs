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
        ChangeContractAuthor,

        //view
        CurrentContractSerialNumber,
        GetContractInfo,
        GetContractAuthor,
        GetContractHash,
        GetContractAddressByName,
        GetSmartContractRegistrationByAddress
    }

    public enum NameProvider
    {
        ElectionName,
        ProfitName,
        VoteSystemName,
        TreasuryName,
        TokenName,
        TokenConverterName,
        FeeReceiverName,
        ConsensusName,
        ParliamentName,
        CrossChainName,
        AssciationName,
        Configuration,

        BasicFunction
    }

    public class GenesisContract : BaseContract<GenesisMethod>
    {
        public static Dictionary<NameProvider, Hash> NameProviderInfos => InitializeSystemContractsName();

        private GenesisContract(IApiHelper apiHelper, string callAddress, string genesisAddress) :
            base(apiHelper, genesisAddress)
        {
            CallAddress = callAddress;
            UnlockAccount(CallAddress);
        }

        public static GenesisContract GetGenesisContract(IApiHelper ch, string callAddress)
        {
            var genesisContract = ch.GetGenesisContractAddress();
            Logger.Info($"Genesis contract Address: {genesisContract}");

            return new GenesisContract(ch, callAddress, genesisContract);
        }

        public bool UpdateContract(string account, string contractAddress, string contractFileName)
        {
            var contractReader = new SmartContractReader();
            var codeArray = contractReader.Read(contractFileName);

            var contractOwner = GetContractAuthor(contractAddress);
            if (contractOwner.GetFormatted() != account)
                Logger.Error("Account have no permission to update.");

            SetAccount(account);
            var txResult = ExecuteMethodWithResult(GenesisMethod.UpdateSmartContract, new ContractUpdateInput
            {
                Address = Address.Parse(contractAddress),
                Code = ByteString.CopyFrom(codeArray)
            });
            if (!(txResult.InfoMsg is TransactionResultDto txDto)) return false;
            return txDto.Status.ConvertTransactionResultStatus() == TransactionResultStatus.Mined;
        }

        public Address GetContractAddressByName(NameProvider name)
        {
            var hash = NameProviderInfos[name];

            var address = CallViewMethod<Address>(GenesisMethod.GetContractAddressByName, hash);
            var addString = address != new Address() ? address.GetFormatted() : "null";
            Logger.Info($"{name.ToString().Replace("Name", "")} contract address: {addString}");

            return address;
        }

        public Address GetContractAuthor(Address contractAddress)
        {
            var address = CallViewMethod<Address>(GenesisMethod.GetContractAuthor, contractAddress);

            return address;
        }

        public Address GetContractAuthor(string contractAddress)
        {
            return GetContractAuthor(Address.Parse(contractAddress));
        }

        private static Dictionary<NameProvider, Hash> InitializeSystemContractsName()
        {
            var dic = new Dictionary<NameProvider, Hash>
            {
                {NameProvider.ElectionName, Hash.FromString("AElf.ContractNames.Election")},
                {NameProvider.ProfitName, Hash.FromString("AElf.ContractNames.Profit")},
                {NameProvider.VoteSystemName, Hash.FromString("AElf.ContractNames.Vote")},
                {NameProvider.TreasuryName, Hash.FromString("AElf.ContractNames.Treasury")},
                {NameProvider.TokenName, Hash.FromString("AElf.ContractNames.Token")},
                {NameProvider.TokenConverterName, Hash.FromString("AElf.ContractNames.TokenConverter")},
                {NameProvider.FeeReceiverName, Hash.FromString("AElf.ContractNames.FeeReceiver")},
                {NameProvider.ConsensusName, Hash.FromString("AElf.ContractNames.Consensus")},
                {NameProvider.ParliamentName, Hash.FromString("AElf.ContractsName.Parliament")},
                {NameProvider.CrossChainName, Hash.FromString("AElf.ContractNames.CrossChain")},
                {NameProvider.AssciationName, Hash.FromString("AElf.ContractNames.Association")},
                {NameProvider.Configuration, Hash.FromString("AElf.ContractNames.Configuration")},
                {NameProvider.BasicFunction, Hash.FromString("AElf.Contracts.BasicFunction")}
            };

            return dic;
        }
    }
}