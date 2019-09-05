using System.Collections.Generic;
using System.Linq;
using Acs0;
using AElf.Automation.Common.Helpers;
using AElf.Automation.Common.Managers;
using AElf.Contracts.Genesis;
using AElf.Types;
using AElfChain.SDK.Models;
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
        ChangeGenesisOwner,
        ValidateSystemContractAddress,

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
        VoteName,
        TreasuryName,
        TokenName,
        TokenConverterName,
        FeeReceiverName,
        ConsensusName,
        ParliamentName,
        CrossChainName,
        AssociationName,
        Configuration,

        BasicFunction
    }

    public class GenesisContract : BaseContract<GenesisMethod>
    {
        public static Dictionary<NameProvider, Hash> NameProviderInfos => InitializeSystemContractsName();

        private GenesisContract(INodeManager nodeManager, string callAddress, string genesisAddress) 
            : base(nodeManager, genesisAddress)
        {
            SetAccount(callAddress);
            Logger = Log4NetHelper.GetLogger();
        }

        public static GenesisContract GetGenesisContract(INodeManager nm, string callAddress)
        {
            var genesisContract = nm.GetGenesisContractAddress();
            Logger.Info($"Genesis contract Address: {genesisContract}");

            return new GenesisContract(nm, callAddress, genesisContract);
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
                Address = AddressHelper.Base58StringToAddress(contractAddress),
                Code = ByteString.CopyFrom(codeArray)
            });
            
            return txResult.Status.ConvertTransactionResultStatus() == TransactionResultStatus.Mined;
        }

        public Address GetContractAddressByName(NameProvider name)
        {
            if (_systemContractAddresses.Keys.Contains(name))
                return _systemContractAddresses[name];
            
            var hash = NameProviderInfos[name];
            var address = CallViewMethod<Address>(GenesisMethod.GetContractAddressByName, hash);
            _systemContractAddresses[name] = address;
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
            return GetContractAuthor(AddressHelper.Base58StringToAddress(contractAddress));
        }

        public BasicContractZeroContainer.BasicContractZeroStub GetGensisStub(string callAddress = null)
        {
            var caller = callAddress ?? CallAddress;
            var stub = new ContractTesterFactory(NodeManager);
            var contractStub =
                stub.Create<BasicContractZeroContainer.BasicContractZeroStub>(
                    AddressHelper.Base58StringToAddress(ContractAddress), caller);
            return contractStub;
        }

        private static Dictionary<NameProvider, Hash> InitializeSystemContractsName()
        {
            var dic = new Dictionary<NameProvider, Hash>
            {
                {NameProvider.ElectionName, Hash.FromString("AElf.ContractNames.Election")},
                {NameProvider.ProfitName, Hash.FromString("AElf.ContractNames.Profit")},
                {NameProvider.VoteName, Hash.FromString("AElf.ContractNames.Vote")},
                {NameProvider.TreasuryName, Hash.FromString("AElf.ContractNames.Treasury")},
                {NameProvider.TokenName, Hash.FromString("AElf.ContractNames.Token")},
                {NameProvider.TokenConverterName, Hash.FromString("AElf.ContractNames.TokenConverter")},
                {NameProvider.FeeReceiverName, Hash.FromString("AElf.ContractNames.FeeReceiver")},
                {NameProvider.ConsensusName, Hash.FromString("AElf.ContractNames.Consensus")},
                {NameProvider.ParliamentName, Hash.FromString("AElf.ContractNames.Parliament")},
                {NameProvider.CrossChainName, Hash.FromString("AElf.ContractNames.CrossChain")},
                {NameProvider.AssociationName, Hash.FromString("AElf.ContractNames.Association")},
                {NameProvider.Configuration, Hash.FromString("AElf.ContractNames.Configuration")},
                {NameProvider.BasicFunction, Hash.FromString("AElf.Contracts.BasicFunction")}
            };

            return dic;
        }
        
        private readonly Dictionary<NameProvider, Address> _systemContractAddresses = new Dictionary<NameProvider, Address>();
    }
}