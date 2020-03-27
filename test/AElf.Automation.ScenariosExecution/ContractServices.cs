using System;
using System.Collections.Generic;
using System.Linq;
using AElf.Types;
using AElfChain.Common;
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
        public string CallAddress { get; set; }
        public Address CallAccount { get; set; }
        public static List<Node> CurrentBpNodes { get; set; }

        public void UpdateRandomEndpoint()
        {
            while (true)
            {
                var nodes = NodeInfoHelper.Config.Nodes;
                var randomId = CommonHelper.GenerateRandomNumber(0, nodes.Count);
                if (nodes[randomId].Endpoint == NodeManager.GetApiUrl()) continue;

                var updateUrl = nodes[randomId].Endpoint;
                NodeManager.UpdateApiUrl(updateUrl);
                break;
            }
        }

        public ContractServices CloneServices()
        {
            return MemberwiseClone() as ContractServices;
        }

        private void GetAllContractServices()
        {
            GenesisService = GenesisContract.GetGenesisContract(NodeManager, CallAddress);

            //Consensus contract
            ConsensusService = GenesisService.GetConsensusContract();

            CurrentBpNodes = GetCurrentBpNodes();
            var specifyEndpoint = ScenarioConfig.ReadInformation.SpecifyEndpoint;
            if (!specifyEndpoint.Enable) //check whether select random endpoint to run
            {
                var rd = new Random(DateTime.Now.Millisecond);
                NodeManager.UpdateApiUrl(CurrentBpNodes[rd.Next(0, CurrentBpNodes.Count - 1)].Endpoint);
            }

            //Treasury contract
            TreasuryService = GenesisService.GetTreasuryContract();

            //Token contract
            TokenService = GenesisService.GetTokenContract();

            //Profit contract
            ProfitService = GenesisService.GetProfitContract();

            //Vote contract
            VoteService = GenesisService.GetVoteContract();

            //Election contract
            ElectionService = GenesisService.GetElectionContract();

            //TokenConverter contract
            TokenConverterService = GenesisService.GetTokenConverterContract();
        }

        private List<Node> GetCurrentBpNodes()
        {
            var nodes = NodeInfoHelper.Config.Nodes;
            var minersPublicKeys = ConsensusService.GetCurrentMinersPubkey();
            var currentBps = nodes.Where(bp => minersPublicKeys.Contains(bp.PublicKey)).ToList();
            Logger.Info($"Current miners are: {string.Join(",", currentBps.Select(o => o.Name))}");

            return currentBps;
        }
    }
}