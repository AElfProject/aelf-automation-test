using System;
using System.Collections.Generic;
using System.Linq;
using AElf.Automation.Common.Contracts;
using AElf.Automation.Common.Helpers;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Automation.ScenariosExecution
{
    public class ContractServices
    {
        protected static readonly ILogHelper Logger = LogHelper.GetLogHelper();
        public readonly IApiHelper ApiHelper;
        public GenesisContract GenesisService { get; set; }
        public TokenContract TokenService { get; set; }
        public static FeeReceiverContract FeeReceiverService { get; set; }
        public VoteContract VoteService { get; set; }
        public ProfitContract ProfitService { get; set; }
        public ElectionContract ElectionService { get; set; }
        public ConsensusContract ConsensusService { get; set; }
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
            var configInfo = ConfigInfoHelper.Config;
            var bpNodes = configInfo.BpNodes;
            var fullNodes = configInfo.FullNodes;
            
            GenesisService = GenesisContract.GetGenesisContract(ApiHelper, CallAddress);
            
            //Consensus contract
            var consensusAddress = GenesisService.GetContractAddressByName(NameProvider.ConsensusName);
            ConsensusService = new ConsensusContract(ApiHelper, CallAddress, consensusAddress.GetFormatted());
            
            CurrentBpNodes = GetCurrentBpNodes(bpNodes, fullNodes);
            var rd = new Random(DateTime.Now.Millisecond); //随机选择bp执行
            ApiHelper.UpdateApiUrl(CurrentBpNodes[rd.Next(0, CurrentBpNodes.Count-1)].ServiceUrl); 
            
            //TokenService contract
            var tokenAddress = GenesisService.GetContractAddressByName(NameProvider.TokenName);
            TokenService = new TokenContract(ApiHelper, CallAddress, tokenAddress.GetFormatted());

            //FeeReceiver contract
            if (FeeReceiverService == null)
            {
                var feeReceiverAddress = GenesisService.GetContractAddressByName(NameProvider.FeeReceiverName);
                if (feeReceiverAddress == new Address())
                {
                    FeeReceiverService = new FeeReceiverContract(ApiHelper, CallAddress);
                    FeeReceiverService.InitializeFeeReceiver(tokenAddress, CallAccount);
                }
                else
                {
                    FeeReceiverService = new FeeReceiverContract(ApiHelper, CallAddress, feeReceiverAddress.GetFormatted());
                }
            }
            
            //TokenConverter contract
            //var converterAddress = GenesisService.GetContractAddressByName(NameProvider.TokenConverterName);
            //TokenConverterService = new TokenConverterContract(ApiHelper, CallAddress, converterAddress.GetFormatted());

            //ProfitService contract
            var profitAddress = GenesisService.GetContractAddressByName(NameProvider.ProfitName);
            ProfitService = new ProfitContract(ApiHelper, CallAddress, profitAddress.GetFormatted());

            //VoteService contract
            var voteAddress = GenesisService.GetContractAddressByName(NameProvider.VoteSystemName);
            VoteService = new VoteContract(ApiHelper, CallAddress, voteAddress.GetFormatted());

            //ElectionService contract
            var electionAddress = GenesisService.GetContractAddressByName(NameProvider.ElectionName);
            ElectionService = new ElectionContract(ApiHelper, CallAddress, electionAddress.GetFormatted());
        }
        
        private void ConnectionChain()
        {
            var ci = new CommandInfo(ApiMethods.GetChainInformation);
            ApiHelper.GetChainInformation(ci);
        }
        
        private List<Node> GetCurrentBpNodes(IEnumerable<Node> bpNodes, IEnumerable<Node> fullNodes)
        {
            var miners = ConsensusService.CallViewMethod<MinerList>(ConsensusMethod.GetCurrentMinerList, new Empty());
            var minersPublicKeys = miners.PublicKeys.Select(o => o.ToByteArray().ToHex()).ToList();
            var currentBps = bpNodes.Where(bp => minersPublicKeys.Contains(bp.PublicKey)).ToList();
            currentBps.AddRange(fullNodes.Where(full => minersPublicKeys.Contains(full.PublicKey)));
            Logger.WriteInfo($"Current miners are: {string.Join(",", currentBps.Select(o=>o.Name))}");

            return currentBps;
        }
    }
}