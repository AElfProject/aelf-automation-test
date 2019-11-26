using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Acs0;
using Acs3;
using AElfChain.Common;
using AElfChain.Common.Contracts;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.MultiToken;
using AElf.Types;
using AElfChain.SDK.Models;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using ApproveInput = Acs3.ApproveInput;

namespace AElf.Automation.SideChain.Verification.CrossChainTransfer
{
    public class CrossChainRegister : CrossChainBase
    {
        public CrossChainRegister()
        {
            MainChainService = InitMainChainServices();
            SideChainServices = InitSideChainServices();

            ChainValidateTxInfo = new Dictionary<int, CrossChainTransactionInfo>();
            ChainCreateTxInfo = new Dictionary<int, CrossChainTransactionInfo>();
        }

        private Dictionary<int, CrossChainTransactionInfo> ChainValidateTxInfo { get; }
        private Dictionary<int, CrossChainTransactionInfo> ChainCreateTxInfo { get; }

        public void DoCrossChainPrepare()
        {
            Logger.Info("Check token address");
            var checkResult = CheckTokenAddress();
            if (checkResult)
            {
                Logger.Info("All the token address is registered");
                return;
            }

            Logger.Info("Validate token address");
            ValidateMainChainTokenAddress();
            ValidateSideChainTokenAddress();
            Logger.Info("Waiting for indexing");
            Thread.Sleep(200000);

            Logger.Info("Transfer side chain token");
            IssueSideTokenForBpAccount();
            Logger.Info("Register address");
            SideChainRegisterMainChain();
            MainChainRegister();
            SideChainRegisterSideChain();
        }

        private bool CheckTokenAddress()
        {
            var mainTokenAddress = AddressHelper.Base58StringToAddress(MainChainService.TokenService.ContractAddress);
            foreach (var sideChainService in SideChainServices)
            {
                var mainAddress = MainChainService.TokenService.CallViewMethod<Address>(
                    TokenMethod.GetCrossChainTransferTokenContractAddress,
                    new GetCrossChainTransferTokenContractAddressInput
                    {
                        ChainId = sideChainService.ChainId
                    });
                var sideTokenAddress =
                    AddressHelper.Base58StringToAddress(sideChainService.TokenService.ContractAddress);
                if (!mainAddress.Equals(sideTokenAddress)) return false;
                Logger.Info($"{MainChainService.ChainId} already register {mainAddress}.");

                var sideAddress = sideChainService.TokenService.CallViewMethod<Address>(
                    TokenMethod.GetCrossChainTransferTokenContractAddress,
                    new GetCrossChainTransferTokenContractAddressInput
                    {
                        ChainId = MainChainService.ChainId
                    });
                if (!sideAddress.Equals(mainTokenAddress)) return false;
                Logger.Info($"{sideChainService.ChainId} already register {sideAddress}.");

                foreach (var sideService in SideChainServices)
                {
                    if (sideService == sideChainService) continue;
                    var sideAddress1 = sideChainService.TokenService.CallViewMethod<Address>(
                        TokenMethod.GetCrossChainTransferTokenContractAddress,
                        new GetCrossChainTransferTokenContractAddressInput
                        {
                            ChainId = sideService.ChainId
                        });
                    var side1TokenAddress =
                        AddressHelper.Base58StringToAddress(sideService.TokenService.ContractAddress);
                    if (!sideAddress1.Equals(side1TokenAddress)) return false;
                    Logger.Info($"{sideChainService.ChainId} already register {sideAddress1}.");
                }
            }

            return true;
        }

        // validate
        private void ValidateMainChainTokenAddress()
        {
            var validateTransaction = MainChainService.GenesisService.NodeManager.GenerateRawTransaction(
                MainChainService.CallAddress, MainChainService.GenesisService.ContractAddress,
                GenesisMethod.ValidateSystemContractAddress.ToString(), new ValidateSystemContractAddressInput
                {
                    Address = AddressHelper.Base58StringToAddress(MainChainService.TokenService.ContractAddress),
                    SystemContractHashName = Hash.FromString("AElf.ContractNames.Token")
                });
            var txId = ExecuteMethodWithTxId(MainChainService, validateTransaction);
            var txResult = CheckTransactionResult(MainChainService, txId);
            if (txResult.Status.ConvertTransactionResultStatus() == TransactionResultStatus.Failed)
                throw new Exception($"Validate chain {MainChainService.ChainId} token contract failed");
            var mainChainTx = new CrossChainTransactionInfo(txResult.BlockNumber, txId, validateTransaction);
            ChainValidateTxInfo.Add(MainChainService.ChainId, mainChainTx);
            Logger.Info($"Validate main chain token address {MainChainService.TokenService.ContractAddress}");
        }

        private void ValidateSideChainTokenAddress()
        {
            foreach (var sideChainService in SideChainServices)
            {
                var validateTransaction = sideChainService.GenesisService.NodeManager.GenerateRawTransaction(
                    sideChainService.CallAddress, sideChainService.GenesisService.ContractAddress,
                    GenesisMethod.ValidateSystemContractAddress.ToString(), new ValidateSystemContractAddressInput
                    {
                        Address = AddressHelper.Base58StringToAddress(sideChainService.TokenService.ContractAddress),
                        SystemContractHashName = Hash.FromString("AElf.ContractNames.Token")
                    });
                var sideTxId = ExecuteMethodWithTxId(sideChainService, validateTransaction);
                var txResult = CheckTransactionResult(sideChainService, sideTxId);
                if (txResult.Status.ConvertTransactionResultStatus() == TransactionResultStatus.Failed)
                    throw new Exception($"Validate chain {sideChainService.ChainId} token contract failed");
                var sideChainTx = new CrossChainTransactionInfo(txResult.BlockNumber, sideTxId, validateTransaction);
                ChainValidateTxInfo.Add(sideChainService.ChainId, sideChainTx);
                Logger.Info(
                    $"Validate chain {sideChainService.ChainId} token address {sideChainService.TokenService.ContractAddress}");
            }
        }

        private void IssueSideTokenForBpAccount()
        {
            var nodeConfig = NodeInfoHelper.Config;
            foreach (var sideChainService in SideChainServices)
            {
                var nodes = nodeConfig.Nodes;

                foreach (var node in nodes)
                {
                    var balance =
                        sideChainService.TokenService.GetUserBalance(node.Account, sideChainService.PrimaryTokenSymbol);
                    if (node.Account == sideChainService.CallAddress || balance > 0) continue;
                    IssueSideChainToken(sideChainService, node.Account);
                }

                foreach (var node in nodes)
                {
                    var accountBalance = GetBalance(sideChainService, node.Account, sideChainService.PrimaryTokenSymbol);
                    Logger.Info(
                        $"Account:{node.Account}, {sideChainService.PrimaryTokenSymbol} balance is: {accountBalance}");
                }
            }
        }

        // register
        private void MainChainRegister()
        {
            foreach (var sideChainService in SideChainServices)
            {
                var chainTxInfo = ChainValidateTxInfo[sideChainService.ChainId];

                Logger.Info("Check the index:");
                CheckSideChainBlockIndexParentChainHeight(sideChainService, chainTxInfo);

                var crossChainMerkleProofContext =
                    GetCrossChainMerkleProofContext(sideChainService, chainTxInfo.BlockHeight);
                var merklePath = GetMerklePath(sideChainService, chainTxInfo.BlockHeight, chainTxInfo.TxId);
                if (merklePath == null)
                    throw new Exception("Can't get the merkle path.");

                var registerInput = new RegisterCrossChainTokenContractAddressInput
                {
                    FromChainId = sideChainService.ChainId,
                    ParentChainHeight = crossChainMerkleProofContext.BoundParentChainHeight,
                    TokenContractAddress =
                        AddressHelper.Base58StringToAddress(sideChainService.TokenService.ContractAddress),
                    TransactionBytes = ByteString.CopyFrom(ByteArrayHelper.HexStringToByteArray(chainTxInfo.RawTx)),
                    MerklePath = merklePath
                };
                registerInput.MerklePath.MerklePathNodes.AddRange(crossChainMerkleProofContext.MerklePathFromParentChain.MerklePathNodes);
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
                throw new Exception("Can't get the merkle path.");
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
                    throw new Exception("Can't get the merkle path.");
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
                    .MerklePathFromParentChain
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
                services.ParliamentService.CallViewMethod<Address>(ParliamentMethod.GetDefaultOrganizationAddress,
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
            if (createProposalResult.Status.ConvertTransactionResultStatus() == TransactionResultStatus.Failed)
                throw new Exception(
                    $"Release proposal failed, token address can't register on chain {services.ChainId}");
            var proposalId = createProposalResult.ReadableReturnValue.Replace("\"", "");

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
                if (approveResult.Status.ConvertTransactionResultStatus() == TransactionResultStatus.Failed)
                    throw new Exception(
                        $"Approve proposal failed, token address can't register on chain {services.ChainId}");
            }

            services.ParliamentService.SetAccount(InitAccount);
            var releaseResult
                = services.ParliamentService.ExecuteMethodWithResult(ParliamentMethod.Release,
                    HashHelper.HexStringToHash(proposalId));
            if (releaseResult.Status.ConvertTransactionResultStatus() != TransactionResultStatus.Mined)
                throw new Exception(
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