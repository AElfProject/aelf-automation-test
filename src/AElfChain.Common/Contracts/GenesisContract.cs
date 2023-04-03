using System.Collections.Generic;
using AElf.Standards.ACS0;
using AElf;
using AElf.Client.Dto;
using AElf.Contracts.Genesis;
using AElf.Types;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
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
        DeployUserSmartContract,
        UpdateUserSmartContract,
        ReleaseApprovedUserSmartContract,
        PerformDeployUserSmartContract,
        PerformUpdateUserSmartContract,
        SetContractAuthor,

        //view
        CurrentContractSerialNumber,
        GetContractInfo,
        GetContractAuthor,
        GetContractHash,
        GetContractAddressByName,
        GetSmartContractRegistrationByAddress,
        GetContractDeploymentController,
        GetCodeCheckController,
        GetSmartContractRegistrationByCodeHash,

        //Fee
        GetMethodFee,
        SetMethodFee,
        GetMethodFeeController
    }

    public class GenesisContract : BaseContract<GenesisMethod>
    {
        private readonly Dictionary<NameProvider, Address> _systemContractAddresses =
            new Dictionary<NameProvider, Address>();

        public GenesisContract(INodeManager nodeManager, string callAddress, string genesisAddress)
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
            if (contractOwner.ToBase58() != account)
                Logger.Error("Account have no permission to update.");

            SetAccount(account);
            var txResult = ExecuteMethodWithResult(GenesisMethod.UpdateSmartContract, new ContractUpdateInput
            {
                Address = contractAddress.ConvertAddress(),
                Code = ByteString.CopyFrom(codeArray)
            });

            return txResult.Status.ConvertTransactionResultStatus() == TransactionResultStatus.Mined;
        }

        public TransactionResultDto DeployUserSmartContract(string contractFileName, string author = null)
        {
            var contractReader = new SmartContractReader();
            var codeArray = contractReader.Read(contractFileName);
            var sender = author ?? CallAddress;
            SetAccount(sender);
            var txResult = ExecuteMethodWithResult(GenesisMethod.DeployUserSmartContract, new ContractDeploymentInput
            {
                Code = ByteString.CopyFrom(codeArray),
                Category = 0
            });

            return txResult;
        }

        public string DeployUserSmartContractWithoutResult(string contractFileName, string author = null)
        {
            var contractReader = new SmartContractReader();
            var codeArray = contractReader.Read(contractFileName);
            var sender = author ?? CallAddress;
            SetAccount(sender);
            var txId = ExecuteMethodWithTxId(GenesisMethod.DeployUserSmartContract, new ContractDeploymentInput
            {
                Code = ByteString.CopyFrom(codeArray),
                Category = 0
            });

            return txId;
        }

        public string GenerateDeployUserSmartContract(string contractFileName, string author = null)
        {
            var contractReader = new SmartContractReader();
            var codeArray = contractReader.Read(contractFileName);
            var sender = author ?? CallAddress;
            SetAccount(sender);
            var rawTx = NodeManager.GenerateRawTransaction(sender, ContractAddress,
                nameof(GenesisMethod.DeployUserSmartContract), new ContractDeploymentInput
                {
                    Code = ByteString.CopyFrom(codeArray),
                    Category = 0
                });

            return rawTx;
        }


        public TransactionResultDto UpdateUserSmartContract(string contractFileName, string contractAddress,
            string author)
        {
            var contractReader = new SmartContractReader();
            var codeArray = contractReader.Read(contractFileName);
            SetAccount(author);
            var txResult = ExecuteMethodWithResult(GenesisMethod.UpdateUserSmartContract, new ContractUpdateInput
            {
                Code = ByteString.CopyFrom(codeArray),
                Address = Address.FromBase58(contractAddress)
            });

            return txResult;
        }

        public string UpdateUserSmartContractWithoutResult(string contractFileName, string contractAddress,
            string author)
        {
            var contractReader = new SmartContractReader();
            var codeArray = contractReader.Read(contractFileName);
            SetAccount(author);
            var txId = ExecuteMethodWithTxId(GenesisMethod.UpdateUserSmartContract, new ContractUpdateInput
            {
                Code = ByteString.CopyFrom(codeArray),
                Address = Address.FromBase58(contractAddress)
            });

            return txId;
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

        public TransactionResultDto ReleaseApprovedContract(ReleaseContractInput input,
            string caller)
        {
            SetAccount(caller);
            var result = ExecuteMethodWithResult(GenesisMethod.ReleaseApprovedContract, new ReleaseContractInput
            {
                ProposalId = input.ProposalId,
                ProposedContractInputHash = input.ProposedContractInputHash
            });
            return result;
        }

        public string ReleaseApprovedContractWithoutResult(ReleaseContractInput input,
            string caller)
        {
            SetAccount(caller);
            var txId = ExecuteMethodWithTxId(GenesisMethod.ReleaseApprovedContract, new ReleaseContractInput
            {
                ProposalId = input.ProposalId,
                ProposedContractInputHash = input.ProposedContractInputHash
            });
            return txId;
        }

        public TransactionResultDto ReleaseCodeCheckedContract(ReleaseContractInput input,
            string caller)
        {
            SetAccount(caller);
            var result = ExecuteMethodWithResult(GenesisMethod.ReleaseCodeCheckedContract, new ReleaseContractInput
            {
                ProposalId = input.ProposalId,
                ProposedContractInputHash = input.ProposedContractInputHash
            });
            return result;
        }

        public TransactionResult ProposeNewContract(ContractDeploymentInput input,
            string caller = null)
        {
            var tester = GetTestStub<BasicContractZeroImplContainer.BasicContractZeroImplStub>(caller);
            var result = AsyncHelper.RunSync(() => tester.ProposeNewContract.SendAsync(input));
            return result.TransactionResult;
        }

        public string ProposeNewContractWithoutResult(ContractDeploymentInput input,
            string caller = null)
        {
            var sender = caller ?? CallAddress;
            SetAccount(sender);
            var result = ExecuteMethodWithTxId(GenesisMethod.ProposeNewContract, input);
            return result;
        }

        public TransactionResult ProposeUpdateContract(ContractUpdateInput input,
            string caller = null)
        {
            var tester = GetTestStub<BasicContractZeroImplContainer.BasicContractZeroImplStub>(caller);
            var result = AsyncHelper.RunSync(() => tester.ProposeUpdateContract.SendAsync(input));
            return result.TransactionResult;
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

        public SmartContractRegistration GetSmartContractRegistrationByCodeHash(Hash codeHash)
        {
            return CallViewMethod<SmartContractRegistration>(GenesisMethod.GetSmartContractRegistrationByCodeHash,
                codeHash);
        }
        
        public SmartContractRegistration GetSmartContractRegistrationByAddress(Address address)
        {
            return CallViewMethod<SmartContractRegistration>(GenesisMethod.GetSmartContractRegistrationByAddress,
                address);
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
                { NameProvider.Genesis, Hash.Empty },
                { NameProvider.Election, HashHelper.ComputeFrom("AElf.ContractNames.Election") },
                { NameProvider.Profit, HashHelper.ComputeFrom("AElf.ContractNames.Profit") },
                { NameProvider.Vote, HashHelper.ComputeFrom("AElf.ContractNames.Vote") },
                { NameProvider.Treasury, HashHelper.ComputeFrom("AElf.ContractNames.Treasury") },
                { NameProvider.Token, HashHelper.ComputeFrom("AElf.ContractNames.Token") },
                { NameProvider.TokenHolder, HashHelper.ComputeFrom("AElf.ContractNames.TokenHolder") },
                { NameProvider.TokenConverter, HashHelper.ComputeFrom("AElf.ContractNames.TokenConverter") },
                { NameProvider.Consensus, HashHelper.ComputeFrom("AElf.ContractNames.Consensus") },
                { NameProvider.ParliamentAuth, HashHelper.ComputeFrom("AElf.ContractNames.Parliament") },
                { NameProvider.CrossChain, HashHelper.ComputeFrom("AElf.ContractNames.CrossChain") },
                { NameProvider.AssociationAuth, HashHelper.ComputeFrom("AElf.ContractNames.Association") },
                { NameProvider.Configuration, HashHelper.ComputeFrom("AElf.ContractNames.Configuration") },
                { NameProvider.ReferendumAuth, HashHelper.ComputeFrom("AElf.ContractNames.Referendum") },
                { NameProvider.Economic, HashHelper.ComputeFrom("AElf.ContractNames.Economic") }
            };

            return dic;
        }
    }
}