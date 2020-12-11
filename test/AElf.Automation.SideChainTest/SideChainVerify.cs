using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using AElf.Standards.ACS0;
using AElf.Standards.ACS3;
using AElf.Standards.ACS7;
using AElf.Client.Dto;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.MultiToken;
using AElf.Types;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Managers;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using Volo.Abp.Threading;

namespace AElf.Automation.SideChainTests
{
    [TestClass]
    public class SideChainVerify : SideChainTestBase
    {
        [TestInitialize]
        public void InitializeNodeTests()
        {
            Initialize();
        }

        [TestMethod]
        public void GetResourceTokenOnSideChain()
        {
            var symbols = new[] {"CPU", "NET", "DISK", "RAM", "READ", "WRITE", "STORAGE", "TRAFFIC", "SHARE"};
            foreach (var sideService in SideServices)
            foreach (var symbol in symbols)
            {
                var infoOnMain = MainServices.TokenService.GetTokenInfo(symbol);
                var info = sideService.TokenService.GetTokenInfo(symbol);
                Logger.Info($"{symbol}:");
                info.Symbol.ShouldBe(infoOnMain.Symbol);
                info.Decimals.ShouldBe(infoOnMain.Decimals);
                info.IssueChainId.ShouldBe(infoOnMain.IssueChainId);
                info.Issuer.ShouldBe(infoOnMain.Issuer);
//                    info.Supply.ShouldBe(infoOnMain.Supply);
                info.TotalSupply.ShouldBe(infoOnMain.TotalSupply);
                info.IsBurnable.ShouldBe(infoOnMain.IsBurnable);
            }
        }

        [TestMethod]
        public void GetSideChainBalance()
        {
            foreach (var service in SideServices)
            {
                var balance1 =
                    MainServices.CrossChainService.GetSideChainBalance(service.ChainId);
           
                Logger.Info($"chain {service.ChainId} balance: {balance1}\n ");
            }
           
        }

        [TestMethod]
        public void GetSideChainIndex()
        {
            foreach (var service in SideServices)
            {
                var mainIndex =
                    MainServices.CrossChainService.GetSideChainHeight(service.ChainId);
                var sideIndex = service.CrossChainService.GetParentChainHeight();
                Logger.Info($"Main chain index side {service.ChainId}: {mainIndex}\n" +
                            $"Side chain index main:{sideIndex}");
            }
        }

        [TestMethod]
        public void GetSideChainIndexingFeeDebt()
        {
            var chainId = ChainHelper.ConvertBase58ToChainId("tDVW");
            var indexingFeeDebt =
                MainServices.CrossChainService.CallViewMethod<Int64Value>(
                    CrossChainContractMethod.GetSideChainIndexingFeeDebt, new Int32Value {Value = chainId});
            Logger.Info($"chain tDVW index fee debt {indexingFeeDebt.Value}");
        }

        [TestMethod]
        public void CheckTokenInfo()
        {
            var symbol = "TEST";
            var mainInfo = MainServices.TokenService.GetTokenInfo(symbol);
            Logger.Info(mainInfo);
            foreach (var sideService in SideServices)
            {
                var sideInfo = sideService.TokenService.GetTokenInfo(symbol);
                Logger.Info(sideInfo);
            }
        }

        #region register

        [TestMethod]
        public void GetCrossChainTransferTokenContractAddress()
        {
            foreach (var service in SideServices)
            {
                var mainAddress = MainServices.TokenService.CallViewMethod<Address>(
                    TokenMethod.GetCrossChainTransferTokenContractAddress,
                    new GetCrossChainTransferTokenContractAddressInput
                    {
                        ChainId = service.ChainId
                    });
                var sideTokenAddress =
                    service.TokenService.Contract;
                if (sideTokenAddress.Equals(mainAddress))
                    Logger.Info($"{MainServices.ChainId} already register {mainAddress}.");
            }
        }

        [TestMethod]
        public async Task MainChainRegisterSideChains()
        {
            foreach (var service in SideServices)
            {
                await MainChainRegisterSideChain(service);
            }
        }

        public async Task MainChainRegisterSideChain(ContractServices services)
        {
            var rawTx = ValidateTokenAddress(services);
            var txId = ExecuteMethodWithTxId(services, rawTx);
            var txResult = services.NodeManager.CheckTransactionResult(txId);

            if (txResult.Status.ConvertTransactionResultStatus() != TransactionResultStatus.Mined)
                Assert.IsTrue(false,
                    $"Validate chain {services.ChainId} token contract failed");
            Logger.Info($"Validate Transaction block: {txResult.BlockNumber}, rawTx: {rawTx}, txId:{txId}");
            Logger.Info(
                $"Validate chain {services.ChainId} token address {services.TokenService.ContractAddress}");

            await MainChainCheckSideChainBlockIndex(services, txResult.BlockNumber);
            var merklePath = GetMerklePath(txResult.BlockNumber, txId, services, out var root);
            if (merklePath == null)
                Assert.IsTrue(false, "Can't get the merkle path.");
            var crossChainMerkleProofContext =
                GetBoundParentChainHeightAndMerklePathByHeight(services, InitAccount, txResult.BlockNumber);
            var registerInput = new RegisterCrossChainTokenContractAddressInput
            {
                FromChainId = services.ChainId,
                ParentChainHeight = crossChainMerkleProofContext.BoundParentChainHeight,
                TokenContractAddress = services.TokenService.Contract,
                TransactionBytes = ByteString.CopyFrom(ByteArrayHelper.HexStringToByteArray(rawTx)),
                MerklePath = merklePath
            };
            registerInput.MerklePath.MerklePathNodes.AddRange(crossChainMerkleProofContext.MerklePathFromParentChain
                .MerklePathNodes);
            Proposal(MainServices, registerInput);
            Logger.Info(
                $"Main chain register chain {services.ChainId} token address {services.TokenService.ContractAddress}");
        }

        [TestMethod]
        public void SideChainRegisterMainChain()
        {
            var service = SideServices.First();
            var rawTx = ValidateTokenAddress(MainServices);
            var txId = ExecuteMethodWithTxId(MainServices, rawTx);
            var txResult = MainServices.NodeManager.CheckTransactionResult(txId);

            if (txResult.Status.ConvertTransactionResultStatus() != TransactionResultStatus.Mined)
                Assert.IsTrue(false,
                    $"Validate chain {MainServices.ChainId} token contract failed");
            Logger.Info($"Validate Transaction block: {txResult.BlockNumber}, rawTx: {rawTx}, txId:{txId}");
            Logger.Info(
                $"Validate chain {MainServices.ChainId} token address {MainServices.TokenService.ContractAddress}");

            while (txResult.BlockNumber > GetIndexParentHeight(service))
            {
                Logger.Info("Block is not recorded ");
                Thread.Sleep(10000);
            }

            //register main chain token address
            var merklePath = GetMerklePath(txResult.BlockNumber, txId, MainServices, out var root);
            if (merklePath == null)
                Assert.IsTrue(false, "Can't get the merkle path.");
            var registerInput = new RegisterCrossChainTokenContractAddressInput
            {
                FromChainId = MainServices.ChainId,
                ParentChainHeight = txResult.BlockNumber,
                TokenContractAddress =
                    service.TokenService.Contract,
                TransactionBytes = ByteString.CopyFrom(ByteArrayHelper.HexStringToByteArray(rawTx)),
                MerklePath = merklePath
            };

            Proposal(service, registerInput);
            Logger.Info(
                $"Chain {service.ChainId} register Main chain token address {MainServices.TokenService.ContractAddress}");
        }

        [TestMethod]
        public async Task ChangeCrossChainTokenContractRegistrationController()
        {
            var service = SideServices.First();
            var tokenImplStub = service.TokenImplContractStub;
            var controller =
                await tokenImplStub.GetCrossChainTokenContractRegistrationController.CallAsync(new Empty());
            var newController = SideAuthorityManager.CreateAssociationOrganization(Members);
            var changeInput = new AuthorityInfo
            {
                ContractAddress = service.AssociationService.Contract,
                OwnerAddress = newController
            };
            var authorityManager = new AuthorityManager(service.NodeManager);
            var changeProposal = authorityManager.ExecuteTransactionWithAuthority(
                service.TokenService.ContractAddress,
                nameof(TokenContractImplContainer.TokenContractImplStub
                    .ChangeCrossChainTokenContractRegistrationController), changeInput, InitAccount,
                controller.OwnerAddress);
            changeProposal.Status.ShouldBe(TransactionResultStatus.Mined);
            var updateController =
                await tokenImplStub.GetCrossChainTokenContractRegistrationController.CallAsync(new Empty());
            updateController.ContractAddress.ShouldBe(service.AssociationService.Contract);
            updateController.OwnerAddress.ShouldBe(newController);

            //register 
            var rawTx = ValidateTokenAddress(MainServices);
            var txId = ExecuteMethodWithTxId(MainServices, rawTx);
            var txResult = MainServices.NodeManager.CheckTransactionResult(txId);

            if (txResult.Status.ConvertTransactionResultStatus() != TransactionResultStatus.Mined)
                Assert.IsTrue(false,
                    $"Validate chain {MainServices.ChainId} token contract failed");
            Logger.Info($"Validate Transaction block: {txResult.BlockNumber}, rawTx: {rawTx}, txId:{txId}");
            Logger.Info(
                $"Validate chain {MainServices.ChainId} token address {MainServices.TokenService.ContractAddress}");

            while (txResult.BlockNumber > GetIndexParentHeight(service))
            {
                Logger.Info("Block is not recorded ");
                Thread.Sleep(10000);
            }

            var merklePath = GetMerklePath(txResult.BlockNumber, txId, MainServices, out var root);
            if (merklePath == null)
                Assert.IsTrue(false, "Can't get the merkle path.");
            var registerInput = new RegisterCrossChainTokenContractAddressInput
            {
                FromChainId = MainServices.ChainId,
                ParentChainHeight = txResult.BlockNumber,
                TokenContractAddress =
                    service.TokenService.Contract,
                TransactionBytes = ByteString.CopyFrom(ByteArrayHelper.HexStringToByteArray(rawTx)),
                MerklePath = merklePath
            };

            //create proposal
            var proposer = service.AssociationService.GetOrganization(newController).ProposerWhiteList.Proposers
                .First();
            service.TokenService.TransferBalance(InitAccount, proposer.ToBase58(), 5000_000000000);
            var createProposalResult =
                service.AssociationService.CreateProposal(service.TokenService.ContractAddress,
                    nameof(TokenMethod.RegisterCrossChainTokenContractAddress), registerInput, newController,
                    proposer.ToBase58());
            service.AssociationService.ApproveWithAssociation(createProposalResult, newController);
            var release = service.AssociationService.ReleaseProposal(createProposalResult, proposer.ToBase58());
            release.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            Logger.Info(
                $"Main chain register chain {service.ChainId} token address {service.TokenService.ContractAddress}");

            //recover
            var recoverInput = new AuthorityInfo
            {
                ContractAddress = controller.ContractAddress,
                OwnerAddress = controller.OwnerAddress
            };
            var recoverProposalResult =
                service.AssociationService.CreateProposal(service.TokenService.ContractAddress,
                    nameof(TokenContractImplContainer.TokenContractImplStub
                        .ChangeCrossChainTokenContractRegistrationController), recoverInput, newController,
                    proposer.ToBase58());
            service.AssociationService.ApproveWithAssociation(recoverProposalResult, newController);
            var recoverRelease =
                service.AssociationService.ReleaseProposal(recoverProposalResult, proposer.ToBase58());
            recoverRelease.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var recoverController =
                await tokenImplStub.GetCrossChainTokenContractRegistrationController.CallAsync(new Empty());
            recoverController.ContractAddress.ShouldBe(controller.ContractAddress);
            recoverController.OwnerAddress.ShouldBe(controller.OwnerAddress);
        }

        [TestMethod]
        public async Task SideChain1RegisterSideChain2()
        {
            var service = SideServices.First();
            var registerService = SideServices.Last();
            
            var rawTx = ValidateTokenAddress(service);
            var txId = ExecuteMethodWithTxId(service, rawTx);
            var txResult = service.NodeManager.CheckTransactionResult(txId);

            if (txResult.Status.ConvertTransactionResultStatus() != TransactionResultStatus.Mined)
                Assert.IsTrue(false,
                    $"Validate chain {service.ChainId} token contract failed");
            Logger.Info($"Validate Transaction block: {txResult.BlockNumber}, rawTx: {rawTx}, txId:{txId}");
            Logger.Info(
                $"Validate chain {service.ChainId} token address {service.TokenService.ContractAddress}");

            var mainHeight = await MainChainCheckSideChainBlockIndex(service, txResult.BlockNumber);
            var crossChainMerkleProofContextA =
                GetBoundParentChainHeightAndMerklePathByHeight(service, InitAccount, txResult.BlockNumber);
            var sideChainMerklePathA =
                GetMerklePath(txResult.BlockNumber, txId, service, out var root);
            if (sideChainMerklePathA == null)
                Assert.IsTrue(false, "Can't get the merkle path.");
            var sideChainRegisterInputA = new RegisterCrossChainTokenContractAddressInput
            {
                FromChainId = service.ChainId,
                ParentChainHeight = crossChainMerkleProofContextA.BoundParentChainHeight,
                TokenContractAddress =
                    service.TokenService.Contract,
                TransactionBytes = ByteString.CopyFrom(ByteArrayHelper.HexStringToByteArray(rawTx)),
                MerklePath = sideChainMerklePathA
            };
            sideChainRegisterInputA.MerklePath.MerklePathNodes.AddRange(crossChainMerkleProofContextA
                .MerklePathFromParentChain
                .MerklePathNodes);

            while (mainHeight > GetIndexParentHeight(registerService))
            {
                Logger.Info("Block is not recorded ");
                await Task.Delay(10000);
            }

            Proposal(registerService, sideChainRegisterInputA);
            Logger.Info(
                $"Chain {registerService.ChainId} register chain {service.ChainId} token address {service.TokenService.ContractAddress}");
        }

        #endregion

        #region cross transfer

        [TestMethod]
        public void MainChainCrossChainTransferSideChainResourceToken()
        {
            foreach (var service in SideServices)
                MainChainCrossChainTransferSideChain(service);
        }

        public void MainChainCrossChainTransferSideChain(ContractServices sideService)
        {
            var symbols = new[] {"ELF"};
            var account = "28Y8JA1i2cN6oHvdv7EraXJr9a1gY6D1PpJXw9QtRMRwKcBQMK";
            var toAccount = "28Y8JA1i2cN6oHvdv7EraXJr9a1gY6D1PpJXw9QtRMRwKcBQMK";
            var txInfos = new Dictionary<TransactionResultDto, string>();
            foreach (var symbol in symbols)
            {
                var tokenInfo = MainServices.TokenService.GetTokenInfo(symbol);
                var crossChainTransferInput = new CrossChainTransferInput
                {
                    Symbol = symbol,
                    IssueChainId = MainServices.ChainId,
                    Amount = 5000_00000000,
                    Memo = "cross chain transfer",
                    To = toAccount.ConvertAddress(),
                    ToChainId = sideService.ChainId
                };
                // execute cross chain transfer
                var rawTx = MainServices.NodeManager.GenerateRawTransaction(account,
                    MainServices.TokenService.ContractAddress, TokenMethod.CrossChainTransfer.ToString(),
                    crossChainTransferInput);
                Logger.Info($"Transaction rawTx is: {rawTx}");
                var txId = ExecuteMethodWithTxId(MainServices, rawTx);
                var txResult = MainServices.NodeManager.CheckTransactionResult(txId);
                // get transaction info            
                var status = txResult.Status.ConvertTransactionResultStatus();
                status.ShouldBe(TransactionResultStatus.Mined);
                Logger.Info(
                    $"Cross chain Transaction block: {txResult.BlockNumber}, rawTx: {rawTx}, txId:{txId} to chain {sideService.ChainId}");
                txInfos.Add(txResult, rawTx);

                var afterTokenInfo = MainServices.TokenService.GetTokenInfo(symbol);
//                afterTokenInfo.Supply.ShouldBe(tokenInfo.Supply - amount);
                Logger.Info($"issued : {afterTokenInfo.Issued}" +
                            $"supply: {afterTokenInfo.Supply}" +
                            $"{afterTokenInfo}");
            }

            while (txInfos.Last().Key.BlockNumber > GetIndexParentHeight(sideService))
            {
                Logger.Info("Block is not recorded ");
                Thread.Sleep(10000);
            }

            foreach (var symbol in symbols)
            {
                var tokenInfo = MainServices.TokenService.GetTokenInfo(symbol);
                Logger.Info($"before receive : {tokenInfo}");
            }

            foreach (var txInfo in txInfos)
            {
                var merklePath = GetMerklePath(txInfo.Key.BlockNumber, txInfo.Key.TransactionId, MainServices,
                    out var root);
                var crossChainReceiveToken = new CrossChainReceiveTokenInput
                {
                    FromChainId = MainServices.ChainId,
                    ParentChainHeight = txInfo.Key.BlockNumber,
                    MerklePath = merklePath
                };
                crossChainReceiveToken.TransferTransactionBytes =
                    ByteString.CopyFrom(ByteArrayHelper.HexStringToByteArray(txInfo.Value));

                var result = CrossChainReceive(sideService, account, crossChainReceiveToken);
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            }

            foreach (var symbol in symbols)
            {
                //verify
                var balance = GetBalance(sideService, toAccount, symbol);
                Logger.Info($"balance: {balance}");
                var tokenInfo = sideService.TokenService.GetTokenInfo(symbol);
                Logger.Info($"after receive : {tokenInfo}");
            }
        }

        [TestMethod]
        public async Task AllSideChainTransferToMainChain()
        {
            foreach (var service in SideServices) await SideChainCrossChainTransferMainChain(service, 10_00000000);
        }

        public async Task SideChainCrossChainTransferMainChain(ContractServices services, long amount)
        {
//            var symbol = services.TokenService.GetPrimaryTokenSymbol();
            var symbol = "TEST";
            var symbolInfo = services.TokenService.GetTokenInfo(symbol);
            var crossChainTransferInput = new CrossChainTransferInput
            {
                Symbol = symbol,
                IssueChainId = symbolInfo.IssueChainId,
                Amount = amount,
                Memo = "cross chain transfer",
                To = InitAccount.ConvertAddress(),
                ToChainId = MainServices.ChainId
            };

            var sideChainTokenContracts = services.TokenService.ContractAddress;
            Logger.Info($"{sideChainTokenContracts}");

            // execute cross chain transfer
            var rawTx = services.NodeManager.GenerateRawTransaction(InitAccount,
                sideChainTokenContracts, nameof(TokenMethod.CrossChainTransfer),
                crossChainTransferInput);
            Logger.Info($"Transaction rawTx is: {rawTx}");
            var txId = services.NodeManager.SendTransaction(rawTx);
            var txResult = services.NodeManager.CheckTransactionResult(txId);
            // get transaction info            
            var status = txResult.Status.ConvertTransactionResultStatus();
            status.ShouldBe(TransactionResultStatus.Mined);
            var fee = txResult.GetDefaultTransactionFee();
            Logger.Info(
                $"Cross chain Transaction block: {txResult.BlockNumber}, rawTx: {rawTx}, txId:{txId} to chain {MainServices.ChainId}");
            var afterSymbolInfo = services.TokenService.GetTokenInfo(symbol);
            afterSymbolInfo.Supply.ShouldBe(symbolInfo.Supply - amount - fee);

            await MainChainCheckSideChainBlockIndex(services, txResult.BlockNumber);
            var merklePath = GetMerklePath(txResult.BlockNumber, txId, services, out var root);
            var crossChainReceiveToken = new CrossChainReceiveTokenInput
            {
                FromChainId = services.ChainId,
                MerklePath = merklePath
            };
            // verify side chain transaction
            var crossChainMerkleProofContext =
                services.CrossChainService.GetCrossChainMerkleProofContext(txResult.BlockNumber);
            crossChainReceiveToken.MerklePath.MerklePathNodes.AddRange(crossChainMerkleProofContext
                .MerklePathFromParentChain.MerklePathNodes);
            crossChainReceiveToken.ParentChainHeight = crossChainMerkleProofContext.BoundParentChainHeight;
            crossChainReceiveToken.TransferTransactionBytes =
                ByteString.CopyFrom(ByteArrayHelper.HexStringToByteArray(rawTx));

            var mainInfo = MainServices.TokenService.GetTokenInfo(symbol);
            var result = CrossChainReceive(MainServices, InitAccount, crossChainReceiveToken);
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            //verify
            var balance = GetBalance(MainServices, InitAccount, symbol);
            Logger.Info($"account {InitAccount}, {symbol} balance: {balance}");
            var afterMainInfo = MainServices.TokenService.GetTokenInfo(symbol);
            afterMainInfo.Issued.ShouldBe(mainInfo.Issued);
            afterMainInfo.Supply.ShouldBe(mainInfo.Supply + amount);
        }

        [TestMethod]
        public async Task SideChainACrossChainTransferSideChainB()
        {
            var service = SideServices.First();
            var toService = SideServices.Last();
            //Transfer
            var crossChainTransferInput = new CrossChainTransferInput
            {
                Symbol = "TEST",
                IssueChainId = MainServices.ChainId,
                Amount = 100_00000000,
                Memo = "cross chain transfer",
                To = InitAccount.ConvertAddress(),
                ToChainId = toService.ChainId
            };
            // execute cross chain transfer
            var rawTx = service.NodeManager.GenerateRawTransaction(InitAccount,
                service.TokenService.ContractAddress,
                TokenMethod.CrossChainTransfer.ToString(),
                crossChainTransferInput);
            Logger.Info($"Transaction rawTx is: {rawTx}");
            var txId = ExecuteMethodWithTxId(service, rawTx);
            var txResult = service.NodeManager.CheckTransactionResult(txId);
            // get transaction info            
            var status = txResult.Status.ConvertTransactionResultStatus();
            status.ShouldBe(TransactionResultStatus.Mined);

            Logger.Info(
                $"Cross chain Transaction block: {txResult.BlockNumber}, rawTx: {rawTx}, txId:{txId} to chain {service.ChainId}");

            var mainHeight = await MainChainCheckSideChainBlockIndex(service, txResult.BlockNumber);
            var merklePath = GetMerklePath(txResult.BlockNumber, txId, service,
                out var root);

            var crossChainReceiveToken = new CrossChainReceiveTokenInput
            {
                FromChainId = service.ChainId,
                MerklePath = merklePath
            };
            var crossChainMerkleProofContext =
                service.CrossChainService.GetCrossChainMerkleProofContext(txResult.BlockNumber);
            crossChainReceiveToken.MerklePath.MerklePathNodes.AddRange(crossChainMerkleProofContext
                .MerklePathFromParentChain.MerklePathNodes);
            crossChainReceiveToken.ParentChainHeight = crossChainMerkleProofContext.BoundParentChainHeight;
            crossChainReceiveToken.TransferTransactionBytes =
                ByteString.CopyFrom(ByteArrayHelper.HexStringToByteArray(rawTx));

            while (mainHeight > GetIndexParentHeight(toService))
            {
                Logger.Info("Block is not recorded ");
                await Task.Delay(10000);
            }

            var result = CrossChainReceive(toService, InitAccount, crossChainReceiveToken);
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            //verify
            var balance = GetBalance(toService, InitAccount, "ABC");
            Logger.Info($"balance: {balance}");
        }

        #endregion

        #region cross create token

        [TestMethod]
        public async Task ValidaAllToken()
        {
            foreach (var sideServices in SideServices)
            foreach (var validate in SideServices)
            {
                if (validate.ChainId.Equals(sideServices.ChainId)) continue;
                await ValidToken(sideServices, validate);
            }
        }

        public async Task ValidToken(ContractServices services, ContractServices validateServices)
        {
            var stub = services.TokenImplContractStub;
            var tokenInfo = await stub.GetTokenInfo.CallAsync(new GetTokenInfoInput
                {Symbol = services.TokenService.GetPrimaryTokenSymbol()});
            var validateStub = validateServices.TokenContractStub;
            var validateTokenInfo = await validateStub.GetTokenInfo.CallAsync(new GetTokenInfoInput
                {Symbol = services.TokenService.GetPrimaryTokenSymbol()});
            if (!validateTokenInfo.Equals(new TokenInfo())) return;
            var result = await stub.ValidateTokenInfoExists.SendAsync(new ValidateTokenInfoExistsInput
            {
                Decimals = tokenInfo.Decimals,
                Issuer = tokenInfo.Issuer,
                IsBurnable = tokenInfo.IsBurnable,
                IssueChainId = tokenInfo.IssueChainId,
                Symbol = tokenInfo.Symbol,
                TokenName = tokenInfo.TokenName,
                TotalSupply = tokenInfo.TotalSupply
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            Logger.Info($"{result.TransactionResult.BlockNumber},{result.TransactionResult.TransactionId}");

            var mainHeight = await MainChainCheckSideChainBlockIndex(services, result.TransactionResult.BlockNumber);

            while (mainHeight > GetIndexParentHeight(services))
            {
                Logger.Info("Block is not recorded ");
                await Task.Delay(10000);
            }

            var merklePath = GetMerklePath(result.TransactionResult.BlockNumber,
                result.TransactionResult.TransactionId.ToHex(), services, out var root);
            var crossChainCrossToken = new CrossChainCreateTokenInput
            {
                FromChainId = services.ChainId,
                MerklePath = merklePath,
                TransactionBytes = result.Transaction.ToByteString()
            };
            var crossChainMerkleProofContext =
                services.CrossChainService.GetCrossChainMerkleProofContext(result.TransactionResult.BlockNumber);
            crossChainCrossToken.MerklePath.MerklePathNodes.AddRange(crossChainMerkleProofContext
                .MerklePathFromParentChain.MerklePathNodes);
            crossChainCrossToken.ParentChainHeight = crossChainMerkleProofContext.BoundParentChainHeight;

            var createResult =
                await validateStub.CrossChainCreateToken.SendAsync(crossChainCrossToken);
            createResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            validateTokenInfo = await validateStub.GetTokenInfo.CallAsync(new GetTokenInfoInput
                {Symbol = services.TokenService.GetPrimaryTokenSymbol()});
            validateTokenInfo.TokenName.ShouldBe(tokenInfo.TokenName);
        }

        [TestMethod]
        public async Task ValidTokenSymbol()
        {
            var symbol = "SHARE";
            var stub = MainServices.TokenImplContractStub;
            var tokenInfo = await stub.GetTokenInfo.CallAsync(new GetTokenInfoInput
                {Symbol = symbol});
            var validateService = new List<ContractServices>();
            foreach (var side in SideServices)
            {
                var validateStub = side.TokenContractStub;
                var validateTokenInfo = await validateStub.GetTokenInfo.CallAsync(new GetTokenInfoInput
                    {Symbol = symbol});
                if (validateTokenInfo.Equals(new TokenInfo()))
                    validateService.Add(side);
            }

            var result = await stub.ValidateTokenInfoExists.SendAsync(new ValidateTokenInfoExistsInput
            {
                Decimals = tokenInfo.Decimals,
                Issuer = tokenInfo.Issuer,
                IsBurnable = tokenInfo.IsBurnable,
                IssueChainId = tokenInfo.IssueChainId,
                Symbol = tokenInfo.Symbol,
                TokenName = tokenInfo.TokenName,
                TotalSupply = tokenInfo.TotalSupply
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            Logger.Info($"{result.TransactionResult.BlockNumber},{result.TransactionResult.TransactionId}");

            var merklePath = GetMerklePath(result.TransactionResult.BlockNumber,
                result.TransactionResult.TransactionId.ToHex(), MainServices, out var root);
            var crossChainCrossToken = new CrossChainCreateTokenInput
            {
                FromChainId = MainServices.ChainId,
                MerklePath = merklePath,
                TransactionBytes = result.Transaction.ToByteString(),
                ParentChainHeight = result.TransactionResult.BlockNumber
            };

            foreach (var validate in validateService)
            {
                while (result.TransactionResult.BlockNumber > GetIndexParentHeight(validate))
                {
                    Logger.Info("Block is not recorded ");
                    Thread.Sleep(10000);
                }

                var validateStub = validate.TokenContractStub;

                var createResult =
                    await validateStub.CrossChainCreateToken.SendAsync(crossChainCrossToken);
                createResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

                var validateTokenInfo = await validateStub.GetTokenInfo.CallAsync(new GetTokenInfoInput
                    {Symbol = symbol});
                validateTokenInfo.Symbol.ShouldBe(symbol);
            }
        }

        #endregion

        [TestMethod]
        public void SideChainVerifyMainChain()
        {
            var service = SideServices.First();
            var block = AsyncHelper.RunSync(() => MainServices.NodeManager.ApiClient.GetBlockByHeightAsync(10000));
            var transaction = AsyncHelper.RunSync(() =>
                MainServices.NodeManager.ApiClient.GetTransactionResultsAsync(block.BlockHash)).First();
            var verifyInput =
                GetMainChainTransactionVerificationInput(transaction.BlockNumber, transaction.TransactionId);
            var result = service.CrossChainService.CallViewMethod<BoolValue>(
                CrossChainContractMethod.VerifyTransaction, verifyInput).Value;
            result.ShouldBeTrue();
        }

        [TestMethod]
        public void MainChainVerifySideChain()
        {
            var service = SideServices.Last();
            var block = AsyncHelper.RunSync(() => service.NodeManager.ApiClient.GetBlockByHeightAsync(8385));
            var transactions = AsyncHelper.RunSync(() =>
                service.NodeManager.ApiClient.GetTransactionResultsAsync(block.BlockHash));

            foreach (var transaction in transactions)
            {
                var merklePath = GetMerklePath(transaction.BlockNumber, transaction.TransactionId, service,
                    out var root);
                var verifyInput = new VerifyTransactionInput
                {
                    TransactionId = Hash.LoadFromHex(transaction.TransactionId),
                    VerifiedChainId = service.ChainId,
                    Path = merklePath
                };
                // verify side chain transaction
                var crossChainMerkleProofContext =
                    service.CrossChainService.GetCrossChainMerkleProofContext(transaction.BlockNumber);
                verifyInput.Path.MerklePathNodes.AddRange(crossChainMerkleProofContext
                    .MerklePathFromParentChain.MerklePathNodes);
                verifyInput.ParentChainHeight = crossChainMerkleProofContext.BoundParentChainHeight;

                var result = MainServices.CrossChainService.CallViewMethod<BoolValue>(
                    CrossChainContractMethod.VerifyTransaction, verifyInput).Value;
                result.ShouldBeTrue();
            }
        }


        #region private

        private async Task<long> MainChainCheckSideChainBlockIndex(ContractServices servicesFrom,
            long txHeight)
        {
            var mainHeight = long.MaxValue;
            var checkResult = false;

            while (!checkResult)
            {
                var indexSideChainBlock = GetIndexSideHeight(servicesFrom);

                if (indexSideChainBlock < txHeight)
                {
                    Logger.Info("Block is not recorded ");
                    await Task.Delay(10000);
                    continue;
                }

                mainHeight = mainHeight == long.MaxValue
                    ? await MainServices.NodeManager.ApiClient.GetBlockHeightAsync()
                    : mainHeight;
                var indexParentBlock = GetIndexParentHeight(servicesFrom);
                checkResult = indexParentBlock > mainHeight;
            }

            return mainHeight;
        }

        private long GetIndexParentHeight(ContractServices services)
        {
            return services.CrossChainService.CallViewMethod<Int64Value>(
                CrossChainContractMethod.GetParentChainHeight, new Empty()).Value;
        }

        private long GetIndexSideHeight(ContractServices services)
        {
            return MainServices.CrossChainService.CallViewMethod<Int64Value>(
                CrossChainContractMethod.GetSideChainHeight, new Int32Value {Value = services.ChainId}).Value;
        }

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
                ToAddress = services.TokenService.Contract,
                ContractMethodName = TokenMethod.RegisterCrossChainTokenContractAddress.ToString(),
                ExpiredTime = DateTime.UtcNow.AddDays(1).ToTimestamp(),
                Params = input.ToByteString()
            };
            var createProposalResult =
                services.ParliamentService.ExecuteMethodWithResult(ParliamentMethod.CreateProposal,
                    createProposalInput);
            if (createProposalResult.Status.ConvertTransactionResultStatus() != TransactionResultStatus.Mined) return;
            var proposalId =
                Hash.Parser.ParseFrom(ByteArrayHelper.HexStringToByteArray(createProposalResult.ReturnValue));

            //approve
            var miners = GetMiners(services);
            foreach (var miner in miners)
            {
                services.ParliamentService.SetAccount(miner.ToBase58());
                var approveResult =
                    services.ParliamentService.ExecuteMethodWithResult(ParliamentMethod.Approve, proposalId);
                if (approveResult.Status.ConvertTransactionResultStatus() != TransactionResultStatus.Mined) return;
            }

            services.ParliamentService.SetAccount(InitAccount);
            var releaseResult
                = services.ParliamentService.ExecuteMethodWithResult(ParliamentMethod.Release, proposalId);
            if (releaseResult.Status.ConvertTransactionResultStatus() != TransactionResultStatus.Mined)
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

        private VerifyTransactionInput GetMainChainTransactionVerificationInput(long blockHeight, string txId)
        {
            var merklePath = GetMerklePath(blockHeight, txId, MainServices, out var hash);
            if (merklePath == null) return null;

            var verificationInput = new VerifyTransactionInput
            {
                ParentChainHeight = blockHeight,
                TransactionId = Hash.LoadFromHex(txId),
                VerifiedChainId = MainServices.ChainId,
                Path = merklePath
            };
            return verificationInput;
        }

        #endregion
    }
}