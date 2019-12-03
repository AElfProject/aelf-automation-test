using System.Collections.Generic;
using Acs3;
using Acs7;
using AElfChain.Common;
using AElfChain.Common.Contracts;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.CrossChain;
using AElf.Contracts.MultiToken;
using AElf.Kernel;
using AElf.Sdk.CSharp;
using AElf.Types;
using AElfChain.Common.Managers;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using ApproveInput = AElf.Contracts.MultiToken.ApproveInput;

namespace AElf.Automation.SideChainCreate
{
    public class Operation
    {
        public readonly ConsensusContract ConsensusService;
        public readonly CrossChainContract CrossChainService;
        public readonly ParliamentAuthContract ParliamentService;

        public readonly TokenContract TokenService;
        public string InitAccount;
        public string NativeSymbol;
        public string Password;

        public string Url;

        public Operation()
        {
            var contractServices = GetContractServices();
            TokenService = contractServices.TokenService;
            CrossChainService = contractServices.CrossChainService;
            ParliamentService = contractServices.ParliamentService;
            ConsensusService = contractServices.ConsensusService;
        }

        public void TransferToken(long amount)
        {
            TokenService.SetAccount(InitAccount, Password);
            var miners = GetMiners();
            foreach (var miner in miners)
            {
                var balance = TokenService.GetUserBalance(miner.GetFormatted());
                if (miner.GetFormatted().Equals(InitAccount)|| balance > amount) continue;
                TokenService.ExecuteMethodWithResult(TokenMethod.Transfer, new TransferInput
                {
                    Symbol = NativeSymbol,
                    Amount = amount,
                    Memo = "Transfer to miners",
                    To = miner
                });
            }
        }

        public void ApproveToken(long amount)
        {
            //token approve
            TokenService.SetAccount(InitAccount, Password);
            TokenService.ExecuteMethodWithResult(TokenMethod.Approve,
                new ApproveInput
                {
                    Symbol = NodeOption.NativeTokenSymbol,
                    Spender = AddressHelper.Base58StringToAddress(CrossChainService.ContractAddress),
                    Amount = amount
                });
        }

        public string CreateProposal(long indexingPrice, long lockedTokenAmount, bool isPrivilegePreserved,
            SideChainTokenInfo tokenInfo)
        {
            var organizationAddress = GetGenesisOwnerAddress();
            var createProposalInput = new SideChainCreationRequest
            {
                IndexingPrice = indexingPrice,
                LockedTokenAmount = lockedTokenAmount,
                IsPrivilegePreserved = isPrivilegePreserved,
                SideChainTokenDecimals = tokenInfo.Decimals,
                SideChainTokenName = tokenInfo.TokenName,
                SideChainTokenSymbol = tokenInfo.Symbol,
                SideChainTokenTotalSupply = tokenInfo.TotalSupply,
                IsSideChainTokenBurnable = tokenInfo.IsBurnable
            };
            ParliamentService.SetAccount(InitAccount, Password);
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
            var proposalId = result.ReadableReturnValue.Replace("\"", "");
            return proposalId;
        }

        public void ApproveProposal(string proposalId)
        {
            var miners = GetMiners();
            foreach (var miner in miners)
            {
                ParliamentService.SetAccount(miner.GetFormatted(), "123");
                var result = ParliamentService.ExecuteMethodWithResult(ParliamentMethod.Approve, new Acs3.ApproveInput
                {
                    ProposalId = HashHelper.HexStringToHash(proposalId)
                });
            }
        }

        public int ReleaseProposal(string proposalId)
        {
            ParliamentService.SetAccount(InitAccount, Password);
            var result
                = ParliamentService.ExecuteMethodWithResult(ParliamentMethod.Release,
                    HashHelper.HexStringToHash(proposalId));
            var creationRequested = result.Logs[1].NonIndexed;
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
            NativeSymbol = environmentInfo.NativeSymbol;
            var contractService = new ContractServices(Url, InitAccount, Password);
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
            ParliamentService.SetAccount(InitAccount, Password);
            var address =
                ParliamentService.CallViewMethod<Address>(ParliamentMethod.GetDefaultOrganizationAddress, new Empty());

            return address;
        }
    }
}