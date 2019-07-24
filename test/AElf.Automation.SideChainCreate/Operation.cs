using System.Collections.Generic;
using System.Linq;
using Acs3;
using Acs7;
using AElf.Automation.Common.Contracts;
using AElf.Automation.Common.Helpers;
using AElf.Automation.Common.OptionManagers;
using AElf.Automation.Common.WebApi.Dto;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.CrossChain;
using AElf.Kernel;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AElf.Automation.SideChainCreate
{
    public class Operation
    {
        public readonly IApiHelper ApiHelper;

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
            ApiHelper = contractServices.ApiHelper;
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
                new Contracts.MultiToken.Messages.ApproveInput
                {
                    Symbol = "ELF",
                    Spender = Address.Parse(CrossChainService.ContractAddress),
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
                        ToAddress = Address.Parse(CrossChainService.ContractAddress),
                        OrganizationAddress = organizationAddress
                    });
            var transactionResult = result.InfoMsg as TransactionResultDto;
            var proposalId = transactionResult.ReadableReturnValue.Replace("\"","");
            return proposalId;
        }
        
        public void ApproveProposal(string proposalId)
        {
            var miners = GetMiners();
            foreach (var miner in miners)
            {
                ParliamentService.SetAccount(miner.GetFormatted());
                var result = ParliamentService.ExecuteMethodWithResult(ParliamentMethod.Approve, new ApproveInput
                {
                    ProposalId = Hash.LoadHex(proposalId)
                });
            }
        }

        public int ReleaseProposal(string proposalId)
        {
            ParliamentService.SetAccount(InitAccount);
            var result
                = ParliamentService.ExecuteMethodWithResult(ParliamentMethod.Release, Hash.LoadHex(proposalId));
            var transactionResult = result.InfoMsg as TransactionResultDto;
            var creationRequested = transactionResult.Logs[0].NonIndexed;
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