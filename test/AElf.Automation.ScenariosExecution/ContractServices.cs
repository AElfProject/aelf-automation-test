using System;
using System.Collections.Generic;
using System.Linq;
using Acs0;
using AElf.Automation.Common.Contracts;
using AElf.Automation.Common.Helpers;
using AElf.Automation.ScenariosExecution.Scenarios;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.TestContract.BasicFunction;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Automation.ScenariosExecution
{
    public class ContractServices
    {
        private static readonly ILogHelper Logger = LogHelper.GetLogHelper();
        public readonly IApiHelper ApiHelper;
        public GenesisContract GenesisService { get; set; }
        public TokenContract TokenService { get; set; }
        public TokenConverterContract TokenConverterService { get; set; }
        public static FeeReceiverContract FeeReceiverService { get; set; }
        public VoteContract VoteService { get; set; }
        public ProfitContract ProfitService { get; set; }
        public ElectionContract ElectionService { get; set; }
        public ConsensusContract ConsensusService { get; set; }
        public BasicFunctionContract FunctionContractService { get; set; }
        public BasicUpdateContract UpdateContractService { get; set; }
        public string CallAddress { get; set; }
        public Address CallAccount { get; set; }

        public static List<Node> CurrentBpNodes { get; set; }

        public ContractServices(IApiHelper apiHelper, string callAddress)
        {
            ApiHelper = apiHelper;
            CallAddress = callAddress;
            CallAccount = Address.Parse(callAddress);

            //connect chain
            ConnectionChain();

            //get all contract services
            GetAllContractServices();
        }

        private void GetAllContractServices()
        {
            GenesisService = GenesisContract.GetGenesisContract(ApiHelper, CallAddress);

            //Consensus contract
            var consensusAddress = GenesisService.GetContractAddressByName(NameProvider.ConsensusName);
            ConsensusService = new ConsensusContract(ApiHelper, CallAddress, consensusAddress.GetFormatted());

            CurrentBpNodes = GetCurrentBpNodes();
            var specifyEndpoint = ConfigInfoHelper.Config.SpecifyEndpoint;
            if (!specifyEndpoint.Enable) //随机选择bp执行
            {
                var rd = new Random(DateTime.Now.Millisecond);
                ApiHelper.UpdateApiUrl(CurrentBpNodes[rd.Next(0, CurrentBpNodes.Count - 1)].ServiceUrl);
            }

            //TokenService contract
            var tokenAddress = GenesisService.GetContractAddressByName(NameProvider.TokenName);
            TokenService = new TokenContract(ApiHelper, CallAddress, tokenAddress.GetFormatted());

            //ProfitService contract
            var profitAddress = GenesisService.GetContractAddressByName(NameProvider.ProfitName);
            ProfitService = new ProfitContract(ApiHelper, CallAddress, profitAddress.GetFormatted());

            //VoteService contract
            var voteAddress = GenesisService.GetContractAddressByName(NameProvider.VoteSystemName);
            VoteService = new VoteContract(ApiHelper, CallAddress, voteAddress.GetFormatted());

            //ElectionService contract
            var electionAddress = GenesisService.GetContractAddressByName(NameProvider.ElectionName);
            ElectionService = new ElectionContract(ApiHelper, CallAddress, electionAddress.GetFormatted());

            //Get or deploy other contracts
            GetOrDeployFeeReceiverContract();
            GetOrDeployFunctionContract();
        }

        private void GetOrDeployFeeReceiverContract()
        {
            var contractsInfo = ConfigInfoHelper.Config.ContractsInfo;
            var autoEnable = contractsInfo.AutoUpdate;
            if (autoEnable)
            {
                var contractItem = contractsInfo.Contracts.First(o => o.Name == "FeeReceiver");
                var queryResult = QueryContractItem(ref contractItem, out _);
                if (queryResult)
                {
                    FeeReceiverService =
                        new FeeReceiverContract(ApiHelper, CallAddress, contractItem.Address);
                }
                else
                {
                    FeeReceiverService = new FeeReceiverContract(ApiHelper, CallAddress);
                    FeeReceiverService.InitializeFeeReceiver(Address.Parse(TokenService.ContractAddress),
                        CallAccount);

                    //update configInfo
                    contractItem.Address = FeeReceiverService.ContractAddress;
                    contractItem.CodeHash = contractItem.CodeHash;
                    contractItem.Owner = CallAddress;
                }

                //write to config file
                ConfigInfoHelper.UpdateConfig(contractsInfo);
            }
            else
            {
                //FeeReceiver contract
                var feeReceiverAddress = GenesisService.GetContractAddressByName(NameProvider.FeeReceiverName);
                if (feeReceiverAddress == new Address())
                {
                    FeeReceiverService = new FeeReceiverContract(ApiHelper, CallAddress);
                    FeeReceiverService.InitializeFeeReceiver(Address.Parse(TokenService.ContractAddress), CallAccount);
                }
                else
                {
                    FeeReceiverService =
                        new FeeReceiverContract(ApiHelper, CallAddress, feeReceiverAddress.GetFormatted());
                }
            }
        }

        private void GetOrDeployTokenConverterContract()
        {
            var contractsInfo = ConfigInfoHelper.Config.ContractsInfo;
            var autoEnable = contractsInfo.AutoUpdate;
            if (autoEnable)
            {
                //Token converter contract
                {
                    var contractItem = contractsInfo.Contracts.First(o => o.Name == "TokenConverter");
                    var queryResult = QueryContractItem(ref contractItem, out _);
                    if (queryResult)
                    {
                        TokenConverterService =
                            new TokenConverterContract(ApiHelper, CallAddress, contractItem.Address);
                    }
                    else
                    {
                        TokenConverterService = new TokenConverterContract(ApiHelper, CallAddress);

                        //update configInfo
                        contractItem.Address = TokenConverterService.ContractAddress;
                        contractItem.Owner = CallAddress;
                    }
                }

                //write to config file
                ConfigInfoHelper.UpdateConfig(contractsInfo);
            }
            else
            {
                //Token converter contract
                TokenConverterService = new TokenConverterContract(ApiHelper, CallAddress);
            }
        }

        private void GetOrDeployFunctionContract()
        {
            var contractsInfo = ConfigInfoHelper.Config.ContractsInfo;
            var autoEnable = contractsInfo.AutoUpdate;
            if (autoEnable)
            {
                var contractItem = contractsInfo.Contracts.First(o => o.Name == "BasicFunction");
                var queryResult = QueryContractItem(ref contractItem, out var updated);

                if (queryResult)
                {
                    ContractScenario.IsUpdateContract = updated;
                    ContractScenario.ContractOwner = contractItem.Owner;
                    ContractScenario.ContractManager = CallAddress;

                    if (updated)
                        UpdateContractService =
                            new BasicUpdateContract(ApiHelper, CallAddress, contractItem.Address);
                    else
                        FunctionContractService =
                            new BasicFunctionContract(ApiHelper, CallAddress, contractItem.Address);
                }
                else
                {
                    FunctionContractService = new BasicFunctionContract(ApiHelper, CallAddress);
                    ContractScenario.IsUpdateContract = false;
                    ContractScenario.ContractOwner = CallAddress;
                    ContractScenario.ContractManager = CallAddress;

                    //update configInfo
                    contractItem.Address = FunctionContractService.ContractAddress;
                    contractItem.Owner = CallAddress;

                    QueryContractItem(ref contractItem, out _);
                    contractItem.CodeHash = contractItem.CodeHash;

                    //Initialize contract
                    FunctionContractService.ExecuteMethodWithResult(FunctionMethod.InitialBasicFunctionContract,
                        new InitialBasicContractInput
                        {
                            ContractName = "Test Contract1",
                            MinValue = 10L,
                            MaxValue = 1000L,
                            MortgageValue = 1000_000_000L,
                            Manager = Address.Parse(CallAddress)
                        });

                    FunctionContractService.ExecuteMethodWithResult(FunctionMethod.UpdateBetLimit, new BetLimitInput
                    {
                        MinValue = 50,
                        MaxValue = 100
                    });
                }

                //write to config file
                ConfigInfoHelper.UpdateConfig(contractsInfo);
            }
            else
            {
                //BasicFunction contract
                FunctionContractService = new BasicFunctionContract(ApiHelper, CallAddress);
                ContractScenario.IsUpdateContract = false;
                ContractScenario.ContractOwner = CallAddress;
                ContractScenario.ContractManager = CallAddress;

                //Initialize contract
                FunctionContractService.ExecuteMethodWithResult(FunctionMethod.InitialBasicFunctionContract,
                    new InitialBasicContractInput
                    {
                        ContractName = "Test Contract1",
                        MinValue = 10L,
                        MaxValue = 1000L,
                        MortgageValue = 1000_000_000L,
                        Manager = Address.Parse(CallAddress)
                    });

                FunctionContractService.ExecuteMethodWithResult(FunctionMethod.UpdateBetLimit, new BetLimitInput
                {
                    MinValue = 50,
                    MaxValue = 100
                });
            }
        }

        private bool QueryContractItem(ref ContractItem contractItem, out bool updated)
        {
            updated = false;
            try
            {
                var contractInfo =
                    GenesisService.CallViewMethod<ContractInfo>(GenesisMethod.GetContractInfo,
                        Address.Parse(contractItem.Address));

                if (contractInfo.Equals(new ContractInfo())) return false;
                contractItem.Owner = contractInfo.Author.GetFormatted();
                if (contractItem.CodeHash != "" && contractItem.CodeHash != contractInfo.CodeHash.ToHex())
                    updated = true;

                return true;
            }
            catch (Exception)
            {
                Logger.WriteWarn($"Query {contractItem.Name} contract info got exception.");
                return false;
            }
        }

        private void ConnectionChain()
        {
            var ci = new CommandInfo(ApiMethods.GetChainInformation);
            ApiHelper.GetChainInformation(ci);
        }

        private List<Node> GetCurrentBpNodes()
        {
            var configInfo = ConfigInfoHelper.Config;
            var bpNodes = configInfo.BpNodes;
            var fullNodes = configInfo.FullNodes;

            var miners = ConsensusService.CallViewMethod<MinerList>(ConsensusMethod.GetCurrentMinerList, new Empty());
            var minersPublicKeys = miners.Pubkeys.Select(o => o.ToByteArray().ToHex()).ToList();
            var currentBps = bpNodes.Where(bp => minersPublicKeys.Contains(bp.PublicKey)).ToList();
            currentBps.AddRange(fullNodes.Where(full => minersPublicKeys.Contains(full.PublicKey)));
            Logger.WriteInfo($"Current miners are: {string.Join(",", currentBps.Select(o => o.Name))}");

            return currentBps;
        }
    }
}