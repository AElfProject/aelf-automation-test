using System;
using System.Collections.Generic;
using System.Linq;
using Acs0;
using AElf.Automation.ScenariosExecution.Scenarios;
using AElf.Contracts.TestContract.BasicFunction;
using AElf.Types;
using AElfChain.Common.Contracts;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using log4net;

namespace AElf.Automation.ScenariosExecution
{
    public class ContractServices
    {
        private static readonly ILog Logger = Log4NetHelper.GetLogger();
        public readonly INodeManager NodeManager;

        public ContractServices(INodeManager nodeManager, string callAddress)
        {
            NodeManager = nodeManager;
            CallAddress = callAddress;
            CallAccount = AddressHelper.Base58StringToAddress(callAddress);

            //get all contract services
            GetAllContractServices();
        }

        public GenesisContract GenesisService { get; set; }
        public TokenContract TokenService { get; set; }
        public TreasuryContract TreasuryService { get; set; }
        public TokenConverterContract TokenConverterService { get; set; }
        public VoteContract VoteService { get; set; }
        public ProfitContract ProfitService { get; set; }
        public ElectionContract ElectionService { get; set; }
        public ConsensusContract ConsensusService { get; set; }
        public BasicFunctionContract FunctionContractService { get; set; }
        public BasicUpdateContract UpdateContractService { get; set; }

        public PerformanceContract PerformanceService { get; set; }
        public string CallAddress { get; set; }
        public Address CallAccount { get; set; }

        public static List<Node> CurrentBpNodes { get; set; }

        private void GetAllContractServices()
        {
            GenesisService = GenesisContract.GetGenesisContract(NodeManager, CallAddress);

            //Consensus contract
            ConsensusService = GenesisService.GetConsensusContract();

            CurrentBpNodes = GetCurrentBpNodes();
            var specifyEndpoint = ConfigInfoHelper.Config.SpecifyEndpoint;
            if (!specifyEndpoint.Enable) //随机选择bp执行
            {
                var rd = new Random(DateTime.Now.Millisecond);
                NodeManager.UpdateApiUrl(CurrentBpNodes[rd.Next(0, CurrentBpNodes.Count - 1)].ServiceUrl);
            }

            //Treasury contract
            TreasuryService = GenesisService.GetTreasuryContract();

            //TokenService contract
            TokenService = GenesisService.GetTokenContract();

            //ProfitService contract
            ProfitService = GenesisService.GetProfitContract();

            //VoteService contract
            VoteService = GenesisService.GetVoteContract();

            //ElectionService contract
            ElectionService = GenesisService.GetElectionContract();

            //TokenConverter contract
            TokenConverterService = GenesisService.GetTokenConverterContract();

            //Get or deploy other contracts
            GetOrDeployFunctionContract();
            GetOrDeployPerformanceContract();
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
                            new BasicUpdateContract(NodeManager, CallAddress, contractItem.Address);
                    else
                        FunctionContractService =
                            new BasicFunctionContract(NodeManager, CallAddress, contractItem.Address);
                }
                else
                {
                    FunctionContractService = new BasicFunctionContract(NodeManager, CallAddress);
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
                            Manager = AddressHelper.Base58StringToAddress(CallAddress)
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
                FunctionContractService = new BasicFunctionContract(NodeManager, CallAddress);
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
                        Manager = AddressHelper.Base58StringToAddress(CallAddress)
                    });

                FunctionContractService.ExecuteMethodWithResult(FunctionMethod.UpdateBetLimit, new BetLimitInput
                {
                    MinValue = 50,
                    MaxValue = 100
                });
            }
        }

        private void GetOrDeployPerformanceContract()
        {
            var contractsInfo = ConfigInfoHelper.Config.ContractsInfo;
            var autoEnable = contractsInfo.AutoUpdate;
            if (autoEnable)
            {
                var contractItem = contractsInfo.Contracts.First(o => o.Name == "Performance");
                var queryResult = QueryContractItem(ref contractItem, out _);
                if (queryResult)
                {
                    PerformanceService =
                        new PerformanceContract(NodeManager, CallAddress, contractItem.Address);
                }
                else
                {
                    PerformanceService = new PerformanceContract(NodeManager, CallAddress);
                    PerformanceService.InitializePerformance();

                    //update configInfo
                    contractItem.Address = PerformanceService.ContractAddress;
                    contractItem.Owner = CallAddress;
                }
            }
            else
            {
                //Performance contract
                PerformanceService = new PerformanceContract(NodeManager, CallAddress);
                PerformanceService.InitializePerformance();
            }
        }

        private bool QueryContractItem(ref ContractItem contractItem, out bool updated)
        {
            updated = false;
            try
            {
                var contractInfo =
                    GenesisService.CallViewMethod<ContractInfo>(GenesisMethod.GetContractInfo,
                        AddressHelper.Base58StringToAddress(contractItem.Address));

                if (contractInfo.Equals(new ContractInfo())) return false;
                contractItem.Owner = contractInfo.Author.GetFormatted();
                if (contractItem.CodeHash != "" && contractItem.CodeHash != contractInfo.CodeHash.ToHex())
                    updated = true;

                return true;
            }
            catch (Exception)
            {
                Logger.Warn($"Query {contractItem.Name} contract info got exception.");
                return false;
            }
        }

        private List<Node> GetCurrentBpNodes()
        {
            var configInfo = ConfigInfoHelper.Config;
            var bpNodes = configInfo.BpNodes;
            var fullNodes = configInfo.FullNodes;

            var minersPublicKeys = ConsensusService.GetCurrentMiners();
            var currentBps = bpNodes.Where(bp => minersPublicKeys.Contains(bp.PublicKey)).ToList();
            currentBps.AddRange(fullNodes.Where(full => minersPublicKeys.Contains(full.PublicKey)));
            Logger.Info($"Current miners are: {string.Join(",", currentBps.Select(o => o.Name))}");

            return currentBps;
        }
    }
}