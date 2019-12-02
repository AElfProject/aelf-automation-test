using System.Collections.Generic;
using Acs0;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using AElf.Contracts.Genesis;
using AElf.Types;
using AElfChain.Common.Utils;
using AElfChain.SDK.Models;
using Google.Protobuf;

namespace AElfChain.Common.Contracts
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
        ReleaseApprovedContract,
        ProposeNewContract,
        ProposeUpdateContract,

        //view
        CurrentContractSerialNumber,
        GetContractInfo,
        GetContractAuthor,
        GetContractHash,
        GetContractAddressByName,
        GetSmartContractRegistrationByAddress,
        GetDeployedContractAddressList
    }

    public class GenesisContract : BaseContract<GenesisMethod>
    {
        private readonly Dictionary<NameProvider, Address> _systemContractAddresses =
            new Dictionary<NameProvider, Address>();

        private GenesisContract(INodeManager nodeManager, string callAddress, string genesisAddress)
            : base(nodeManager, genesisAddress)
        {
            SetAccount(callAddress);
            Logger = Log4NetHelper.GetLogger();
        }

        public static Dictionary<NameProvider, Hash> NameProviderInfos => InitializeSystemContractsName();

        public static GenesisContract GetGenesisContract(INodeManager nm, string callAddress = "")
        {
            if (callAddress == "")
                callAddress = nm.GetRandomAccount();

            var genesisContract = nm.GetGenesisContractAddress();

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
                Address = contractAddress.ConvertAddress(),
                Code = ByteString.CopyFrom(codeArray)
            });

            return txResult.Status.ConvertTransactionResultStatus() == TransactionResultStatus.Mined;
        }

        public Address GetContractAddressByName(NameProvider name)
        {
            if (_systemContractAddresses.ContainsKey(name))
                return _systemContractAddresses[name];

            if (name == NameProvider.Genesis)
            {
                _systemContractAddresses[name] = Contract;
                return Contract;
            }

            var hash = NameProviderInfos[name];
            var address = CallViewMethod<Address>(GenesisMethod.GetContractAddressByName, hash);
            _systemContractAddresses[name] = address;
            var addString = address != new Address() ? address.GetFormatted() : "null";
            Logger.Info($"{name} contract address: {addString}");

            return address;
        }

        public TransactionResultDto ReleaseApprovedContract(ReleaseApprovedContractInput input,
            string caller = null)
        {
            SetAccount(caller);
            var result = ExecuteMethodWithResult(GenesisMethod.ReleaseApprovedContract, new ReleaseApprovedContractInput
            {
                ProposalId = input.ProposalId,
                ProposedContractInputHash = input.ProposedContractInputHash
            });
            return result;
        }
        
        public Dictionary<NameProvider, Address> GetAllSystemContracts()
        {
            var dic = new Dictionary<NameProvider, Address>();
            foreach (var provider in NameProviderInfos.Keys)
            {
                var address = GetContractAddressByName(provider);
                dic.Add(provider, address);
            }

            return dic;
        }

        public Address GetContractAuthor(Address contractAddress)
        {
            var address = CallViewMethod<Address>(GenesisMethod.GetContractAuthor, contractAddress);

            return address;
        }

        public Address GetContractAuthor(string contractAddress)
        {
            return GetContractAuthor(contractAddress.ConvertAddress());
        }

        public BasicContractZeroContainer.BasicContractZeroStub GetGensisStub(string callAddress = null)
        {
            var caller = callAddress ?? CallAddress;
            var stub = new ContractTesterFactory(NodeManager);
            var contractStub =
                stub.Create<BasicContractZeroContainer.BasicContractZeroStub>(
                    ContractAddress.ConvertAddress(), caller);
            return contractStub;
        }

        private static Dictionary<NameProvider, Hash> InitializeSystemContractsName()
        {
            var dic = new Dictionary<NameProvider, Hash>
            {
                {NameProvider.Genesis, Hash.Empty},
                {NameProvider.Election, Hash.FromString("AElf.ContractNames.Election")},
                {NameProvider.Profit, Hash.FromString("AElf.ContractNames.Profit")},
                {NameProvider.Vote, Hash.FromString("AElf.ContractNames.Vote")},
                {NameProvider.Treasury, Hash.FromString("AElf.ContractNames.Treasury")},
                {NameProvider.Token, Hash.FromString("AElf.ContractNames.Token")},
                {NameProvider.TokenConverter, Hash.FromString("AElf.ContractNames.TokenConverter")},
                {NameProvider.Consensus, Hash.FromString("AElf.ContractNames.Consensus")},
                {NameProvider.ParliamentAuth, Hash.FromString("AElf.ContractNames.Parliament")},
                {NameProvider.CrossChain, Hash.FromString("AElf.ContractNames.CrossChain")},
                {NameProvider.AssociationAuth, Hash.FromString("AElf.ContractNames.AssociationAuth")},
                {NameProvider.Configuration, Hash.FromString("AElf.ContractNames.Configuration")},
                {NameProvider.ReferendumAuth, Hash.FromString("AElf.ContractNames.ReferendumAuth")}
            };

            return dic;
        }
    }
}