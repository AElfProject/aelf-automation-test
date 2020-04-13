using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Acs0;
using Acs3;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.MultiToken;
using AElf.Types;
using AElfChain.Common;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;

namespace AElf.Automation.SideChain.Verification.CrossChainTransfer
{
    public class CrossChainRegister : CrossChainBase
    {
        private const long amount = 10000_00000000;

        public CrossChainRegister()
        {
            MainChainService = InitMainChainServices();
            SideChainServices = InitSideChainServices();
            ChainValidateTxInfo = new Dictionary<int, CrossChainTransactionInfo>();
        }

        private Dictionary<int, CrossChainTransactionInfo> ChainValidateTxInfo { get; }

        public void DoCrossChainPrepare()
        {
            Logger.Info("Check token address");
            var checkResult = CheckTokenAddress();
            if (checkResult)
            {
                Logger.Info("All the token address is registered");
                return;
            }

            TransferToInitAccount();
            Logger.Info("Validate token address");
            ValidateSideChainTokenAddress();
            Logger.Info("Transfer side chain token");
            TransferTokenToMainChainBpAccount();
            IssueSideTokenForBpAccount();
            Logger.Info("Waiting for indexing");
            Thread.Sleep(60000);

            Logger.Info("Register address");
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
                var txResult = sideChainService.NodeManager.CheckTransactionResult(sideTxId);
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
            var nodes = nodeConfig.Nodes;
            foreach (var sideChainService in SideChainServices)
            {
                if (!CheckSideChainPrivilegePreserved(sideChainService))
                {
                    foreach (var node in nodes)
                    {
                        var balance =
                            sideChainService.TokenService.GetUserBalance(node.Account,
                                sideChainService.PrimaryTokenSymbol);
                        if (node.Account == sideChainService.CallAddress || balance > 10000_00000000) continue;
                        TransferToken(sideChainService, node.Account);
                    }
                }
                else
                {
                    foreach (var node in nodes)
                    {
                        var balance =
                            sideChainService.TokenService.GetUserBalance(node.Account,
                                sideChainService.PrimaryTokenSymbol);
                        if (node.Account == sideChainService.CallAddress || balance > 0) continue;
                        if (IsSupplyAllToken(sideChainService))
                            TransferToken(sideChainService, node.Account);
                        else
                            IssueSideChainToken(sideChainService, node.Account);
                    }
                }

                foreach (var node in nodes)
                {
                    var accountBalance =
                        sideChainService.TokenService.GetUserBalance(node.Account, sideChainService.PrimaryTokenSymbol);
                    Logger.Info(
                        $"Account:{node.Account}, {sideChainService.PrimaryTokenSymbol} balance is: {accountBalance}");
                }
            }
        }

        private void TransferTokenToMainChainBpAccount()
        {
            var nodeConfig = NodeInfoHelper.Config;
            var nodes = nodeConfig.Nodes;

            foreach (var node in nodes)
            {
                var balance =
                    MainChainService.TokenService.GetUserBalance(node.Account, MainChainService.PrimaryTokenSymbol);
                if (node.Account == MainChainService.CallAddress || balance > 10000_00000000) continue;
                TransferToken(MainChainService, node.Account);
            }

            foreach (var node in nodes)
            {
                var accountBalance =
                    MainChainService.TokenService.GetUserBalance(node.Account, MainChainService.PrimaryTokenSymbol);
                Logger.Info(
                    $"Account:{node.Account}, {MainChainService.PrimaryTokenSymbol} balance is: {accountBalance}");
            }
        }

        // register
        private void MainChainRegister()
        {
            foreach (var sideChainService in SideChainServices)
            {
                var chainTxInfo = ChainValidateTxInfo[sideChainService.ChainId];

                Logger.Info("Check the index:");
                MainChainCheckSideChainBlockIndex(sideChainService, chainTxInfo.BlockHeight);
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
                registerInput.MerklePath.MerklePathNodes.AddRange(crossChainMerkleProofContext.MerklePathFromParentChain
                    .MerklePathNodes);
                Proposal(MainChainService, registerInput);
                Logger.Info(
                    $"Main chain register chain {sideChainService.ChainId} token address {sideChainService.TokenService.ContractAddress}");
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

        private void TransferToInitAccount()
        {
            var initRawTxInfos = new Dictionary<int, CrossChainTransactionInfo>();
            var miners = GetMiners(MainChainService);
            var enumerable = miners as Address[] ?? miners.ToArray();
            foreach (var sideChainService in SideChainServices)
            {
                if (CheckSideChainPrivilegePreserved(sideChainService)) continue;
                var balance = sideChainService.TokenService.GetUserBalance(InitAccount, NativeToken);
                if (balance >= amount * (Count + enumerable.Count()))
                {
                    Logger.Info(
                        $"Side chain {sideChainService.ChainId} account {sideChainService.CallAddress}" +
                        $"{NativeToken} token balance is {balance}");
                    return;
                }

                var rawTxInfo = CrossChainTransferWithResult(MainChainService, NativeToken, InitAccount, InitAccount,
                    sideChainService.ChainId, amount * Count * 2);
                initRawTxInfos.Add(sideChainService.ChainId, rawTxInfo);
                Logger.Info(
                    $"the transactions block is:{rawTxInfo.BlockHeight},transaction id is: {rawTxInfo.TxId}");

                Logger.Info("Waiting for the index");
                Thread.Sleep(30000);

                Logger.Info($"Side chain {sideChainService.ChainId} received token");
                Logger.Info(
                    $"Receive CrossTransfer Transaction id is : {initRawTxInfos[sideChainService.ChainId].TxId}");
                Logger.Info("Check the index:");
                while (!CheckSideChainBlockIndex(sideChainService, initRawTxInfos[sideChainService.ChainId]))
                {
                    Console.WriteLine("Block is not recorded ");
                    Thread.Sleep(10000);
                }

                var input = ReceiveFromMainChainInput(initRawTxInfos[sideChainService.ChainId]);
                sideChainService.TokenService.SetAccount(InitAccount);
                var result = sideChainService.TokenService.ExecuteMethodWithResult(
                    TokenMethod.CrossChainReceiveToken,
                    input);
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
                Logger.Info($"check the balance on the side chain {sideChainService.ChainId}");
                var accountBalance = sideChainService.TokenService.GetUserBalance(InitAccount, NativeToken);
                Logger.Info(
                    $"On side chain {sideChainService.ChainId}, InitAccount:{InitAccount}, {NativeToken} balance is {accountBalance}");
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
            var proposalId =
                Hash.Parser.ParseFrom(ByteArrayHelper.HexStringToByteArray(createProposalResult.ReturnValue));
            //approve
            var miners = GetMiners(services);
            var enumerable = miners as Address[] ?? miners.ToArray();
            foreach (var miner in enumerable)
            {
                var proposalStatue = services.ParliamentService.CheckProposal(proposalId);
                if (proposalStatue.ToBeReleased) goto Release;
                services.ParliamentService.SetAccount(miner.GetFormatted());
                var approveResult =
                    services.ParliamentService.ExecuteMethodWithResult(ParliamentMethod.Approve, proposalId);
                if (approveResult.Status.ConvertTransactionResultStatus() == TransactionResultStatus.Failed)
                    throw new Exception(
                        $"Approve proposal failed, token address can't register on chain {services.ChainId}");
            }

            Release:
            services.ParliamentService.SetAccount(InitAccount);
            var releaseResult
                = services.ParliamentService.ExecuteMethodWithResult(ParliamentMethod.Release, proposalId);
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