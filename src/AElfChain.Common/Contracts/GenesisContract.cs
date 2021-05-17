using System.Collections.Generic;
using System.Linq;
using AElf.Standards.ACS0;
using AElf;
using AElf.Contracts.Genesis;
using AElf.Types;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Volo.Abp.Threading;

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
        ReleaseCodeCheckedContract,
        ChangeContractDeploymentController,
        ChangeCodeCheckController,

        //view
        CurrentContractSerialNumber,
        GetContractInfo,
        GetContractAuthor,
        GetContractHash,
        GetContractAddressByName,
        GetSmartContractRegistrationByAddress,
        GetContractDeploymentController,
        GetCodeCheckController,
        GetSmartContractRegistrationByCodeHash
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

        public Address DeployContract(string account, byte[] code,string password = "")
        {
            SetAccount(account, password);
            var txResult = ExecuteMethodWithResult(GenesisMethod.DeploySmartContract, new ContractDeploymentInput
            {
                Category = KernelHelper.DefaultRunnerCategory,
                Code = ByteString.CopyFrom(code)
            });
            txResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var byteString = ByteString.FromBase64(txResult.Logs.First(l => l.Name.Contains(nameof(ContractDeployed))).NonIndexed);
            var address = ContractDeployed.Parser.ParseFrom(byteString).Address;
            return address;
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
            var addString = address != new Address() ? address.ToBase58() : "null";
            Logger.Info($"{name} contract address: {addString}");

            return address;
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

        public AuthorityInfo GetContractDeploymentController()
        {
            return CallViewMethod<AuthorityInfo>(GenesisMethod.GetContractDeploymentController, new Empty());
        }

        public BasicContractZeroContainer.BasicContractZeroStub GetGensisStub(string callAddress = null, string password = "")
        {
            var caller = callAddress ?? CallAddress;
            var stub = new ContractTesterFactory(NodeManager);
            var contractStub =
                stub.Create<BasicContractZeroContainer.BasicContractZeroStub>(
                    ContractAddress.ConvertAddress(), caller, password);
            return contractStub;
        }

        private static Dictionary<NameProvider, Hash> InitializeSystemContractsName()
        {
            var dic = new Dictionary<NameProvider, Hash>
            {
                {NameProvider.Genesis, Hash.Empty},
                {NameProvider.Consensus, HashHelper.ComputeFrom("AElf.ContractNames.Consensus")},
            };

            return dic;
        }
    }
}