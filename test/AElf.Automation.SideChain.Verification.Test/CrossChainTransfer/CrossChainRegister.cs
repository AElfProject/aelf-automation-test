using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Acs0;
using Acs3;
using AElf.Automation.Common.Contracts;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.MultiToken;
using AElf.Types;
using AElfChain.SDK.Models;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ApproveInput = Acs3.ApproveInput;


namespace AElf.Automation.SideChain.Verification.CrossChainTransfer
{
    public class CrossChainRegister : CrossChainBase
    {
        private Dictionary<int, CrossChainTransactionInfo> ChainValidateTxInfo { get; set; }
        private Dictionary<int, CrossChainTransactionInfo> ChainCreateTxInfo { get; set; }

        public CrossChainRegister()
        {
            MainChainService = InitMainChainServices();
            SideChainServices = InitSideChainServices();

            ChainValidateTxInfo = new Dictionary<int, CrossChainTransactionInfo>();
            ChainCreateTxInfo = new Dictionary<int, CrossChainTransactionInfo>();
        }

        public void DoCrossChainPrepare()
        {
            Logger.Info("Validate token address");
            ValidateMainChainTokenAddress();
            ValidateSideChainTokenAddress();
            Logger.Info("Waiting for indexing");
            Thread.Sleep(200000);

            Logger.Info("Register address");
            SideChainRegisterMainChain();
            MainChainRegister();
            SideChainRegisterSideChain();
        }

        // validate
        private void ValidateMainChainTokenAddress()
        {
            var validateTransaction = MainChainService.GenesisService.ApiHelper.GenerateTransactionRawTx(
                MainChainService.CallAddress, MainChainService.GenesisService.ContractAddress,
                GenesisMethod.ValidateSystemContractAddress.ToString(), new ValidateSystemContractAddressInput
                {
                    Address = AddressHelper.Base58StringToAddress(MainChainService.TokenService.ContractAddress),
                    SystemContractHashName = Hash.FromString("AElf.ContractNames.Token")
                });
            var txId = ExecuteMethodWithTxId(MainChainService, validateTransaction);
            var result = CheckTransactionResult(MainChainService, txId);
            if (!(result.InfoMsg is TransactionResultDto txResult)) return;
            if (txResult.Status.ConvertTransactionResultStatus() == TransactionResultStatus.Failed)
                Assert.IsTrue(false, $"Validate chain {MainChainService.ChainId} token contract failed");
            var mainChainTx = new CrossChainTransactionInfo(txResult.BlockNumber, txId, validateTransaction);
            ChainValidateTxInfo.Add(MainChainService.ChainId, mainChainTx);
            Logger.Info($"Validate main chain token address {MainChainService.TokenService.ContractAddress}");
        }

        private void ValidateSideChainTokenAddress()
        {
            foreach (var sideChainService in SideChainServices)
            {
                var validateTransaction = sideChainService.GenesisService.ApiHelper.GenerateTransactionRawTx(
                    sideChainService.CallAddress, sideChainService.GenesisService.ContractAddress,
                    GenesisMethod.ValidateSystemContractAddress.ToString(), new ValidateSystemContractAddressInput
                    {
                        Address = AddressHelper.Base58StringToAddress(sideChainService.TokenService.ContractAddress),
                        SystemContractHashName = Hash.FromString("AElf.ContractNames.Token")
                    });
                var sideTxId = ExecuteMethodWithTxId(sideChainService, validateTransaction);
                var result = CheckTransactionResult(sideChainService, sideTxId);
                if (!(result.InfoMsg is TransactionResultDto txResult)) return;
                if (txResult.Status.ConvertTransactionResultStatus() == TransactionResultStatus.Failed)
                    Assert.IsTrue(false, $"Validate chain {sideChainService.ChainId} token contract failed");
                var sideChainTx = new CrossChainTransactionInfo(txResult.BlockNumber, sideTxId, validateTransaction);
                ChainValidateTxInfo.Add(sideChainService.ChainId, sideChainTx);
                Logger.Info(
                    $"Validate chain {sideChainService.ChainId} token address {sideChainService.TokenService.ContractAddress}");
            }
        }

        // register
        private void MainChainRegister()
        {
            foreach (var sideChainService in SideChainServices)
            {
                var chainTxInfo = ChainValidateTxInfo[sideChainService.ChainId];

                Logger.Info("Check the index:");
                while (!CheckParentChainBlockIndex(sideChainService, chainTxInfo))
                {
                    Logger.Info("Block is not recorded ");
                    Thread.Sleep(10000);
                }

                var crossChainMerkleProofContext =
                    GetCrossChainMerkleProofContext(sideChainService, chainTxInfo.BlockHeight);
                var merklePath = GetMerklePath(sideChainService, chainTxInfo.BlockHeight, chainTxInfo.TxId);
                if (merklePath == null)
                    Assert.IsTrue(false, "Can't get the merkle path.");

                var registerInput = new RegisterCrossChainTokenContractAddressInput
                {
                    FromChainId = sideChainService.ChainId,
                    ParentChainHeight = crossChainMerkleProofContext.BoundParentChainHeight,
                    TokenContractAddress =
                        AddressHelper.Base58StringToAddress(sideChainService.TokenService.ContractAddress),
                    TransactionBytes = ByteString.CopyFrom(ByteArrayHelper.HexStringToByteArray(chainTxInfo.RawTx)),
                    MerklePath = merklePath
                };
                registerInput.MerklePath.MerklePathNodes.AddRange(crossChainMerkleProofContext
                    .MerklePathForParentChainRoot.MerklePathNodes);
                Proposal(MainChainService, registerInput);
                Logger.Info(
                    $"Main chain register chain {sideChainService.ChainId} token address {sideChainService.TokenService.ContractAddress}");
            }
        }

        private void SideChainRegisterMainChain()
        {
            //register main chain token address
            var mainChainTxInfo = ChainValidateTxInfo[MainChainService.ChainId];
            foreach (var sideChainService in SideChainServices)
            {
                Logger.Info("Check the index:");
                while (!CheckSideChainBlockIndex(sideChainService, mainChainTxInfo))
                {
                    Logger.Info("Block is not recorded ");
                    Thread.Sleep(10000);
                }
            }

            var merklePath = GetMerklePath(MainChainService, mainChainTxInfo.BlockHeight, mainChainTxInfo.TxId);
            if (merklePath == null)
                Assert.IsTrue(false, "Can't get the merkle path.");
            var registerInput = new RegisterCrossChainTokenContractAddressInput
            {
                FromChainId = MainChainService.ChainId,
                ParentChainHeight = mainChainTxInfo.BlockHeight,
                TokenContractAddress =
                    AddressHelper.Base58StringToAddress(MainChainService.TokenService.ContractAddress),
                TransactionBytes = ByteString.CopyFrom(ByteArrayHelper.HexStringToByteArray(mainChainTxInfo.RawTx)),
                MerklePath = merklePath
            };

            foreach (var sideChainService in SideChainServices)
            {
                Proposal(sideChainService, registerInput);
                Logger.Info(
                    $"Chain {sideChainService.ChainId} register Main chain token address {MainChainService.TokenService.ContractAddress}");
            }
        }

        private void SideChainRegisterSideChain()
        {
            foreach (var sideChainService in SideChainServices)
            {
                //verify side chain token address
                var chainTxInfo = ChainValidateTxInfo[sideChainService.ChainId];
                var crossChainMerkleProofContext =
                    GetCrossChainMerkleProofContext(sideChainService, chainTxInfo.BlockHeight);
                var sideChainMerklePath =
                    GetMerklePath(sideChainService, chainTxInfo.BlockHeight, chainTxInfo.TxId);
                if (sideChainMerklePath == null)
                    Assert.IsTrue(false, "Can't get the merkle path.");
                var sideChainRegisterInput = new RegisterCrossChainTokenContractAddressInput
                {
                    FromChainId = sideChainService.ChainId,
                    ParentChainHeight = crossChainMerkleProofContext.BoundParentChainHeight,
                    TokenContractAddress =
                        AddressHelper.Base58StringToAddress(sideChainService.TokenService.ContractAddress),
                    TransactionBytes = ByteString.CopyFrom(ByteArrayHelper.HexStringToByteArray(chainTxInfo.RawTx)),
                    MerklePath = sideChainMerklePath
                };
                sideChainRegisterInput.MerklePath.MerklePathNodes.AddRange(crossChainMerkleProofContext
                    .MerklePathForParentChainRoot
                    .MerklePathNodes);

                foreach (var registerChain in SideChainServices)
                {
                    if (registerChain == sideChainService) continue;
                    Proposal(registerChain, sideChainRegisterInput);
                    Logger.Info(
                        $"Chain {registerChain.ChainId} register chain {sideChainService.ChainId} token address {sideChainService.TokenService.ContractAddress}");
                }
            }
        }

        // proposal
        private void Proposal(ContractServices services, IMessage input)
        {
            //get default organization
            var organizationAddress =
                services.ParliamentService.CallViewMethod<Address>(ParliamentMethod.GetGenesisOwnerAddress,
                    new Empty());
            //create proposal
            var createProposalInput = new CreateProposalInput
            {
                OrganizationAddress = organizationAddress,
                ToAddress = AddressHelper.Base58StringToAddress(services.TokenService.ContractAddress),
                ContractMethodName = TokenMethod.RegisterCrossChainTokenContractAddress.ToString(),
                ExpiredTime = DateTime.UtcNow.AddDays(1).ToTimestamp(),
                Params = input.ToByteString()
            };
            var createProposalResult =
                services.ParliamentService.ExecuteMethodWithResult(ParliamentMethod.CreateProposal,
                    createProposalInput);
            if (!(createProposalResult.InfoMsg is TransactionResultDto txResult)) return;
            if (txResult.Status.ConvertTransactionResultStatus() == TransactionResultStatus.Failed)
                Assert.IsTrue(false,
                    $"Release proposal failed, token address can't register on chain {services.ChainId}");
            var proposalId = txResult.ReadableReturnValue.Replace("\"", "");

            //approve
            var miners = GetMiners(services);
            var enumerable = miners as Address[] ?? miners.ToArray();
            foreach (var miner in enumerable)
            {
                services.ParliamentService.SetAccount(miner.GetFormatted());
                var approveResult = services.ParliamentService.ExecuteMethodWithResult(ParliamentMethod.Approve,
                    new ApproveInput
                    {
                        ProposalId = HashHelper.HexStringToHash(proposalId)
                    });
                if (!(approveResult.InfoMsg is TransactionResultDto approveTxResult)) return;
                if (approveTxResult.Status.ConvertTransactionResultStatus() == TransactionResultStatus.Failed)
                    Assert.IsTrue(false,
                        $"Approve proposal failed, token address can't register on chain {services.ChainId}");
            }

            services.ParliamentService.SetAccount(InitAccount);
            var releaseResult
                = services.ParliamentService.ExecuteMethodWithResult(ParliamentMethod.Release,
                    HashHelper.HexStringToHash(proposalId));
            if (!(releaseResult.InfoMsg is TransactionResultDto releaseTxResult)) return;
            if (releaseTxResult.Status.ConvertTransactionResultStatus() != TransactionResultStatus.Mined)
                Assert.IsTrue(false,
                    $"Release proposal failed, token address can't register on chain {services.ChainId}");
        }

        // get miners
        private IEnumerable<Address> GetMiners(ContractServices services)
        {
            var minerList = new List<Address>();
            var miners =
                services.ConsensusService.CallViewMethod<MinerList>(ConsensusMethod.GetCurrentMinerList, new Empty());
            foreach (var publicKey in miners.Pubkeys)
            {
                var address = Address.FromPublicKey(publicKey.ToByteArray());
                minerList.Add(address);
            }

            return minerList;
        }
    }
}