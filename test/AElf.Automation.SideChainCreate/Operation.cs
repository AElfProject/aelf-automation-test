using System.Collections.Generic;
using Acs3;
using Acs7;
using AElf.Automation.Common.Contracts;
using AElf.Automation.Common.Helpers;
using AElf.Automation.Common.Managers;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.CrossChain;
using AElf.Kernel;
using AElf.Sdk.CSharp;
using AElf.Types;
using AElfChain.SDK.Models;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using ApproveInput = AElf.Contracts.MultiToken.ApproveInput;

namespace AElf.Automation.SideChainCreate
{
    public class Operation
    {
        public readonly INodeManager NodeManager;

        public readonly TokenContract TokenService;
        public readonly ConsensusContract ConsensusService;
        public readonly CrossChainContract CrossChainService;
        public readonly ParliamentAuthContract ParliamentService;

        public string Url;
        public string InitAccount;
        public string Password;

        public Operation()
        {
            var contractServices = GetContractServices();
            NodeManager = contractServices.NodeManager;
            TokenService = contractServices.TokenService;
            CrossChainService = contractServices.CrossChainService;
            ParliamentService = contractServices.ParliamentService;
            ConsensusService = contractServices.ConsensusService;
        }
        
        public void ApproveToken(long amount)
        {
            //token approve
            TokenService.SetAccount(InitAccount); 
            TokenService.ExecuteMethodWithResult(TokenMethod.Approve,
                new ApproveInput
                {
                    Symbol = "ELF",
                    Spender = AddressHelper.Base58StringToAddress(CrossChainService.ContractAddress),
                    Amount = amount,
                });
        }
        
        public string CreateProposal(long indexingPrice, long lockedTokenAmount, bool isPrivilegePreserved)
        {
            var organizationAddress = GetGenesisOwnerAddress();
            ByteString code = ByteString.FromBase64("4d5a90000300");
            var createProposalInput = new SideChainCreationRequest
            {
                ContractCode = code,
                IndexingPrice = indexingPrice,
                LockedTokenAmount = lockedTokenAmount,
                IsPrivilegePreserved = isPrivilegePreserved
            };
            ParliamentService.SetAccount(InitAccount);
            var result =
                ParliamentService.ExecuteMethodWithResult(ParliamentMethod.CreateProposal,
                    new CreateProposalInput
                    {
                        ContractMethodName = nameof(CrossChainContractMethod.CreateSideChain),
                        ExpiredTime = TimestampHelper.GetUtcNow().AddDays(1),
                        Params = createProposalInput.ToByteString(),
                        ToAddress = AddressHelper.Base58StringToAddress(CrossChainService.ContractAddress),
                        OrganizationAddress = organizationAddress
                    });
            var proposalId = result.ReadableReturnValue.Replace("\"","");
            return proposalId;
        }
        
        public void ApproveProposal(string proposalId)
        {
            var miners = GetMiners();
            foreach (var miner in miners)
            {
                ParliamentService.SetAccount(miner.GetFormatted());
                var result = ParliamentService.ExecuteMethodWithResult(ParliamentMethod.Approve, new Acs3.ApproveInput
                {
                    ProposalId = HashHelper.HexStringToHash(proposalId)
                });
            }
        }

        public int ReleaseProposal(string proposalId)
        {
            ParliamentService.SetAccount(InitAccount);
            var result
                = ParliamentService.ExecuteMethodWithResult(ParliamentMethod.Release, HashHelper.HexStringToHash(proposalId));
            var creationRequested = result.Logs[0].NonIndexed;
            var byteString = ByteString.FromBase64(creationRequested);
            var chainId = CreationRequested.Parser.ParseFrom(byteString).ChainId;
            return chainId;
        }

        
        private ContractServices GetContractServices()
        {
            var testEnvironment = ConfigHelper.Config.TestEnvironment;
            var environmentInfo =
                ConfigHelper.Config.EnvironmentInfos.Find(o => o.Environment.Contains(testEnvironment));

            InitAccount = environmentInfo.InitAccount;
            Url = environmentInfo.Url;
            Password = environmentInfo.Password;
            var contractService = new ContractServices(Url,InitAccount,Password);
            return contractService;
        }

        private IEnumerable<Address> GetMiners()
        {
            var minerList = new List<Address>();
            var miners = ConsensusService.CallViewMethod<MinerList>(ConsensusMethod.GetCurrentMinerList, new Empty());
            foreach (var publicKey in miners.Pubkeys)
            {
                var address = Address.FromPublicKey(publicKey.ToByteArray());
                minerList.Add(address);
            }

            return minerList;
        }

        private Address GetGenesisOwnerAddress()
        {
            ParliamentService.SetAccount(InitAccount);
            var address =
                ParliamentService.CallViewMethod<Address>(ParliamentMethod.GetGenesisOwnerAddress, new Empty());

            return address;
        }
    }
}