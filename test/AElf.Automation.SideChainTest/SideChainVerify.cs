using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Acs3;
using Acs7;
using AElf.Client.Dto;
using AElfChain.Common.Contracts;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.MultiToken;
using AElf.Kernel;
using AElf.Types;
using AElfChain.Common.DtoExtension;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

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

        #region register

        [TestMethod]
        public async Task MainChainRegisterSideChain1()
        {
            var rawTx = ValidateTokenAddress(SideAServices);
            var txId = ExecuteMethodWithTxId(SideAServices, rawTx);
            var txResult = SideAServices.NodeManager.CheckTransactionResult(txId);

            if (txResult.Status.ConvertTransactionResultStatus() != TransactionResultStatus.Mined)
                Assert.IsTrue(false,
                    $"Validate chain {SideAServices.ChainId} token contract failed");
            _logger.Info($"Validate Transaction block: {txResult.BlockNumber}, rawTx: {rawTx}, txId:{txId}");
            _logger.Info(
                $"Validate chain {SideAServices.ChainId} token address {SideAServices.TokenService.ContractAddress}");

            await MainChainCheckSideChainBlockIndex(SideAServices, txResult.BlockNumber);
            var merklePath = GetMerklePath(txResult.BlockNumber, txId, SideAServices, out var root);
            if (merklePath == null)
                Assert.IsTrue(false, "Can't get the merkle path.");
            var crossChainMerkleProofContext =
                GetBoundParentChainHeightAndMerklePathByHeight(SideAServices, InitAccount, txResult.BlockNumber);
            var registerInput = new RegisterCrossChainTokenContractAddressInput
            {
                FromChainId = SideAServices.ChainId,
                ParentChainHeight = crossChainMerkleProofContext.BoundParentChainHeight,
                TokenContractAddress =
                    AddressHelper.Base58StringToAddress(SideAServices.TokenService.ContractAddress),
                TransactionBytes = ByteString.CopyFrom(ByteArrayHelper.HexStringToByteArray(rawTx)),
                MerklePath = merklePath
            };
            registerInput.MerklePath.MerklePathNodes.AddRange(crossChainMerkleProofContext.MerklePathFromParentChain
                .MerklePathNodes);
            Proposal(MainServices, registerInput);
            _logger.Info(
                $"Main chain register chain {SideAServices.ChainId} token address {SideAServices.TokenService.ContractAddress}");
        }

        [TestMethod]
        public void SideChain1RegisterMainChain()
        {
            var rawTx = ValidateTokenAddress(MainServices);
            var txId = ExecuteMethodWithTxId(MainServices, rawTx);
            var txResult = MainServices.NodeManager.CheckTransactionResult(txId);

            if (txResult.Status.ConvertTransactionResultStatus() != TransactionResultStatus.Mined)
                Assert.IsTrue(false,
                    $"Validate chain {MainServices.ChainId} token contract failed");
            _logger.Info($"Validate Transaction block: {txResult.BlockNumber}, rawTx: {rawTx}, txId:{txId}");
            _logger.Info(
                $"Validate chain {MainServices.ChainId} token address {MainServices.TokenService.ContractAddress}");

            while (txResult.BlockNumber > GetIndexParentHeight(SideAServices))
            {
                _logger.Info("Block is not recorded ");
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
                    AddressHelper.Base58StringToAddress(MainServices.TokenService.ContractAddress),
                TransactionBytes = ByteString.CopyFrom(ByteArrayHelper.HexStringToByteArray(rawTx)),
                MerklePath = merklePath
            };

            Proposal(SideAServices, registerInput);
            _logger.Info(
                $"Chain {SideAServices.ChainId} register Main chain token address {MainServices.TokenService.ContractAddress}");
        }

        [TestMethod]
        public async Task SideChain1RegisterSideChain2()
        {
            var rawTx = ValidateTokenAddress(SideAServices);
            var txId = ExecuteMethodWithTxId(SideAServices, rawTx);
            var txResult = SideAServices.NodeManager.CheckTransactionResult(txId);

            if (txResult.Status.ConvertTransactionResultStatus() != TransactionResultStatus.Mined)
                Assert.IsTrue(false,
                    $"Validate chain {SideAServices.ChainId} token contract failed");
            _logger.Info($"Validate Transaction block: {txResult.BlockNumber}, rawTx: {rawTx}, txId:{txId}");
            _logger.Info(
                $"Validate chain {SideAServices.ChainId} token address {SideAServices.TokenService.ContractAddress}");

            var mainHeight = await MainChainCheckSideChainBlockIndex(SideAServices, txResult.BlockNumber);
            var crossChainMerkleProofContextA =
                GetBoundParentChainHeightAndMerklePathByHeight(SideAServices, InitAccount, txResult.BlockNumber);
            var sideChainMerklePathA =
                GetMerklePath(txResult.BlockNumber, txId, SideAServices, out var root);
            if (sideChainMerklePathA == null)
                Assert.IsTrue(false, "Can't get the merkle path.");
            var sideChainRegisterInputA = new RegisterCrossChainTokenContractAddressInput
            {
                FromChainId = SideAServices.ChainId,
                ParentChainHeight = crossChainMerkleProofContextA.BoundParentChainHeight,
                TokenContractAddress =
                    AddressHelper.Base58StringToAddress(SideAServices.TokenService.ContractAddress),
                TransactionBytes = ByteString.CopyFrom(ByteArrayHelper.HexStringToByteArray(rawTx)),
                MerklePath = sideChainMerklePathA
            };
            sideChainRegisterInputA.MerklePath.MerklePathNodes.AddRange(crossChainMerkleProofContextA
                .MerklePathFromParentChain
                .MerklePathNodes);

            while (mainHeight > GetIndexParentHeight(SideBServices))
            {
                _logger.Info("Block is not recorded ");
                await Task.Delay(10000);
            }

            Proposal(SideBServices, sideChainRegisterInputA);
            _logger.Info(
                $"Chain {SideBServices.ChainId} register chain {SideAServices.ChainId} token address {SideAServices.TokenService.ContractAddress}");
        }

        #endregion

        #region cross transfer

        [TestMethod]
        public void MainChainCrossChainTransferSideChain()
        {
            var crossChainTransferInput = new CrossChainTransferInput
            {
                Symbol = GetPrimaryTokenSymbol(MainServices),
                IssueChainId = MainServices.ChainId,
                Amount = 100000_00000000,
                Memo = "cross chain transfer",
                To = AddressHelper.Base58StringToAddress(InitAccount),
                ToChainId = SideAServices.ChainId,
            };
            // execute cross chain transfer
            var rawTx = MainServices.NodeManager.GenerateRawTransaction(InitAccount,
                MainServices.TokenService.ContractAddress, TokenMethod.CrossChainTransfer.ToString(),
                crossChainTransferInput);
            _logger.Info($"Transaction rawTx is: {rawTx}");
            var txId = ExecuteMethodWithTxId(MainServices, rawTx);
            var txResult = MainServices.NodeManager.CheckTransactionResult(txId);
            // get transaction info            
            var status = txResult.Status.ConvertTransactionResultStatus();
            status.ShouldBe(TransactionResultStatus.Mined);
            _logger.Info(
                $"Cross chain Transaction block: {txResult.BlockNumber}, rawTx: {rawTx}, txId:{txId} to chain {SideAServices.ChainId}");

            while (txResult.BlockNumber > GetIndexParentHeight(SideAServices))
            {
                _logger.Info("Block is not recorded ");
                Thread.Sleep(10000);
            }

            var merklePath = GetMerklePath(txResult.BlockNumber, txId, MainServices, out var root);
            var crossChainReceiveToken = new CrossChainReceiveTokenInput
            {
                FromChainId = MainServices.ChainId,
                ParentChainHeight = txResult.BlockNumber,
                MerklePath = merklePath
            };
            crossChainReceiveToken.TransferTransactionBytes =
                ByteString.CopyFrom(ByteArrayHelper.HexStringToByteArray(rawTx));

            var result = CrossChainReceive(SideAServices, InitAccount, crossChainReceiveToken);
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            //verify
            var balance = GetBalance(SideAServices, InitAccount, "EPC");
            _logger.Info($"balance: {balance}");
        }

        [TestMethod]
        public async Task SideChain1CrossChainTransferMainChain()
        {
            var issue = SideAServices.TokenService.ExecuteMethodWithResult(TokenMethod.Issue, new IssueInput
            {
                Symbol = "STA",
                Amount = 100_0000,
                Memo = "issue side chain token on main chain",
                To = AddressHelper.Base58StringToAddress(InitAccount)
            });

            var crossChainTransferInput = new CrossChainTransferInput
            {
                Symbol = "STA",
                IssueChainId = SideAServices.ChainId,
                Amount = 2_00000000,
                Memo = "cross chain transfer",
                To = AddressHelper.Base58StringToAddress(InitAccount),
                ToChainId = MainServices.ChainId
            };

            var sideChainTokenContracts = SideAServices.TokenService.ContractAddress;
            _logger.Info($"{sideChainTokenContracts}");

            // execute cross chain transfer
            var rawTx = SideAServices.NodeManager.GenerateRawTransaction(InitAccount,
                sideChainTokenContracts, nameof(TokenMethod.CrossChainTransfer),
                crossChainTransferInput);
            _logger.Info($"Transaction rawTx is: {rawTx}");
            var txId = SideAServices.NodeManager.SendTransaction(rawTx);
            var txResult = SideAServices.NodeManager.CheckTransactionResult(txId);
            // get transaction info            
            var status = txResult.Status.ConvertTransactionResultStatus();

            _logger.Info(
                $"Cross chain Transaction block: {txResult.BlockNumber}, rawTx: {rawTx}, txId:{txId} to chain {MainServices.ChainId}");

            await MainChainCheckSideChainBlockIndex(SideAServices, txResult.BlockNumber);
            var merklePath = GetMerklePath(txResult.BlockNumber, txId, SideAServices, out var root);
            var crossChainReceiveToken = new CrossChainReceiveTokenInput
            {
                FromChainId = SideAServices.ChainId,
                MerklePath = merklePath
            };
            // verify side chain transaction
            var crossChainMerkleProofContext = GetCrossChainMerkleProofContext(SideAServices, txResult.BlockNumber);
            crossChainReceiveToken.MerklePath.MerklePathNodes.AddRange(crossChainMerkleProofContext
                .MerklePathFromParentChain.MerklePathNodes);
            crossChainReceiveToken.ParentChainHeight = crossChainMerkleProofContext.BoundParentChainHeight;
            crossChainReceiveToken.TransferTransactionBytes =
                ByteString.CopyFrom(ByteArrayHelper.HexStringToByteArray(rawTx));


            var result = CrossChainReceive(MainServices, InitAccount, crossChainReceiveToken);
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            //verify
            var balance = GetBalance(MainServices, InitAccount, "EPC");
            _logger.Info($"balance: {balance}");
        }

        [TestMethod]
        public async Task SideChainACrossChainTransferSideChainB()
        {
            //Transfer
            var crossChainTransferInput = new CrossChainTransferInput
            {
                Symbol = "STA",
                IssueChainId = SideAServices.ChainId,
                Amount = 1000,
                Memo = "cross chain transfer",
                To = AddressHelper.Base58StringToAddress(InitAccount),
                ToChainId = SideBServices.ChainId
            };
            // execute cross chain transfer
            var rawTx = SideAServices.NodeManager.GenerateRawTransaction(InitAccount,
                SideAServices.TokenService.ContractAddress,
                TokenMethod.CrossChainTransfer.ToString(),
                crossChainTransferInput);
            _logger.Info($"Transaction rawTx is: {rawTx}");
            var txId = ExecuteMethodWithTxId(SideAServices, rawTx);
            var txResult = SideAServices.NodeManager.CheckTransactionResult(txId);
            // get transaction info            
            var status = txResult.Status.ConvertTransactionResultStatus();
            status.ShouldBe(TransactionResultStatus.Mined);

            _logger.Info(
                $"Cross chain Transaction block: {txResult.BlockNumber}, rawTx: {rawTx}, txId:{txId} to chain {SideAServices.ChainId}");

            var mainHeight = await MainChainCheckSideChainBlockIndex(SideAServices, txResult.BlockNumber);
            var merklePath = GetMerklePath(txResult.BlockNumber, txId, SideAServices,
                out var root);

            var crossChainReceiveToken = new CrossChainReceiveTokenInput
            {
                FromChainId = SideAServices.ChainId,
                MerklePath = merklePath
            };
            var crossChainMerkleProofContext = GetCrossChainMerkleProofContext(SideAServices, txResult.BlockNumber);
            crossChainReceiveToken.MerklePath.MerklePathNodes.AddRange(crossChainMerkleProofContext
                .MerklePathFromParentChain.MerklePathNodes);
            crossChainReceiveToken.ParentChainHeight = crossChainMerkleProofContext.BoundParentChainHeight;
            crossChainReceiveToken.TransferTransactionBytes =
                ByteString.CopyFrom(ByteArrayHelper.HexStringToByteArray(rawTx));

            while (mainHeight > GetIndexParentHeight(SideBServices))
            {
                _logger.Info("Block is not recorded ");
                await Task.Delay(10000);
            }

            var result = CrossChainReceive(SideBServices, InitAccount, crossChainReceiveToken);
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            //verify
            var balance = GetBalance(SideBServices, InitAccount, "STA");
            _logger.Info($"balance: {balance}");
        }

        #endregion

        #region cross create token

        [TestMethod]
        public async Task ValidToken()
        {
            var tokenInfo = await side1TokenContractStub.GetTokenInfo.CallAsync(new GetTokenInfoInput
                {Symbol = SideAServices.TokenService.GetPrimaryTokenSymbol()});
            var result = await side1TokenContractStub.ValidateTokenInfoExists.SendAsync(new ValidateTokenInfoExistsInput
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
            _logger.Info($"{result.TransactionResult.BlockNumber},{result.TransactionResult.TransactionId}");

            var mainHeight = await MainChainCheckSideChainBlockIndex(SideAServices, result.TransactionResult);
            var merklePath = GetMerklePath(result.TransactionResult.BlockNumber,
                result.TransactionResult.TransactionId.ToString().Replace("\"", ""), SideAServices, out var root);
            var crossChainCrossToken = new CrossChainCreateTokenInput
            {
                FromChainId = SideAServices.ChainId,
                MerklePath = merklePath,
                TransactionBytes = result.Transaction.ToByteString()
            };

            var crossChainMerkleProofContext =
                GetCrossChainMerkleProofContext(SideAServices, result.TransactionResult.BlockNumber);
            crossChainCrossToken.MerklePath.MerklePathNodes.AddRange(crossChainMerkleProofContext
                .MerklePathFromParentChain.MerklePathNodes);
            crossChainCrossToken.ParentChainHeight = crossChainMerkleProofContext.BoundParentChainHeight;

            while (mainHeight > GetIndexParentHeight(SideBServices))
            {
                _logger.Info("Block is not recorded ");
                await Task.Delay(10000);
            }

            var createResult =
                await side2TokenContractStub.CrossChainCreateToken.SendAsync(crossChainCrossToken);
            createResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var sideBTokenInfo = await side2TokenContractStub.GetTokenInfo.CallAsync(new GetTokenInfoInput
                {Symbol = SideAServices.TokenService.GetPrimaryTokenSymbol()});
            sideBTokenInfo.TokenName.ShouldBe(tokenInfo.TokenName);
        }

        [TestMethod]
        public async Task ValidateTokenSymbol()
        {
            var rawTx = await ValidateTokenSymbol(SideAServices, SideAServices.TokenService
                .GetPrimaryTokenSymbol());
            var txId = ExecuteMethodWithTxId(SideAServices, rawTx);
            var txResult = SideAServices.NodeManager.CheckTransactionResult(txId);
            if (txResult.Status.ConvertTransactionResultStatus() != TransactionResultStatus.Mined)
                Assert.IsTrue(false,
                    $"Validate chain {SideAServices.ChainId} token symbol failed");
            var mainHeight = await MainChainCheckSideChainBlockIndex(SideAServices, txResult.BlockNumber);

            var merklePath = GetMerklePath(txResult.BlockNumber, txResult.TransactionId, SideAServices, out var root);
            if (merklePath == null)
                Assert.IsTrue(false, "Can't get the merkle path.");
            var createInput = new CrossChainCreateTokenInput
            {
                FromChainId = SideAServices.ChainId,
                MerklePath = merklePath,
                TransactionBytes = ByteString.CopyFrom(ByteArrayHelper.HexStringToByteArray(rawTx))
            };

            var crossChainMerkleProofContext = GetCrossChainMerkleProofContext(SideAServices, txResult.BlockNumber);
            createInput.MerklePath.MerklePathNodes.AddRange(crossChainMerkleProofContext
                .MerklePathFromParentChain.MerklePathNodes);
            createInput.ParentChainHeight = crossChainMerkleProofContext.BoundParentChainHeight;

            while (mainHeight > GetIndexParentHeight(SideBServices))
            {
                _logger.Info("Block is not recorded ");
                await Task.Delay(10000);
            }

            var createResult =
                SideBServices.TokenService.ExecuteMethodWithResult(TokenMethod.CrossChainCreateToken,
                    createInput);
            createResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var sideBTokenInfo = SideBServices.TokenService.CallViewMethod<TokenInfo>(TokenMethod.GetTokenInfo,
                new GetTokenInfoInput {Symbol = SideAServices.TokenService.GetPrimaryTokenSymbol()});
            sideBTokenInfo.Symbol.ShouldBe(SideAServices.TokenService.GetPrimaryTokenSymbol());
        }

        #endregion

        [TestMethod]
        public async Task CrossChainData()
        {
            var balance = MainServices.TokenService.GetUserBalance(InitAccount);
            _logger.Info($"{balance}");
            var blocks = new List<BlockDto>();
            for (int i = 511; i < 521; i++)
            {
                var block = await SideAServices.NodeManager.ApiClient.GetBlockByHeightAsync(i, true);
                blocks.Add(block);
            }

            var crossChainData = new CrossChainBlockData();

            for (int i = 1; i < blocks.Count; i++)
            {
                var blockHeader = new BlockHeader(HashHelper.HexStringToHash(blocks[i - 1].BlockHash));
                var height = blocks[i].Header.Height;
                var txId = blocks[i].Body.Transactions.First();
                GetMerklePath(height, txId,
                    SideAServices, out var root);
                var sideChainBlockDate = new SideChainBlockData
                {
                    ChainId = ChainHelper.ConvertBase58ToChainId("tDVV"),
                    BlockHeaderHash = Hash.FromMessage(blockHeader),
                    Height = height,
                    TransactionStatusMerkleTreeRoot = root
                };
                crossChainData.SideChainBlockDataList.Add(sideChainBlockDate);
            }

            crossChainData.PreviousBlockHeight = 3900;

            var result =
                MainServices.CrossChainService.ExecuteMethodWithResult(CrossChainContractMethod.RecordCrossChainData,
                    crossChainData);

            var afterBalance = MainServices.TokenService.GetUserBalance(InitAccount);
            _logger.Info($"{afterBalance}");
        }

        [TestMethod]
        public void GetIndexParentHeight()
        {
            var height1 = SideAServices.CrossChainService.CallViewMethod<SInt64Value>(
                CrossChainContractMethod.GetParentChainHeight, new Empty()).Value;
            _logger.Info($"{height1}");

            var height2 = SideBServices.CrossChainService.CallViewMethod<SInt64Value>(
                CrossChainContractMethod.GetParentChainHeight, new Empty()).Value;
            _logger.Info($"{height2}");
        }

        [TestMethod]
        public void GetIndexSideHeight()
        {
            var height1 = MainServices.CrossChainService.CallViewMethod<SInt64Value>(
                CrossChainContractMethod.GetSideChainHeight, new SInt32Value {Value = SideAServices.ChainId}).Value;
            _logger.Info($"{height1}");

            var height2 = MainServices.CrossChainService.CallViewMethod<SInt64Value>(
                CrossChainContractMethod.GetSideChainHeight, new SInt32Value {Value = SideBServices.ChainId}).Value;
            _logger.Info($"{height2}");
        }

        #region private

        private async Task<long> MainChainCheckSideChainBlockIndex(ContractServices servicesFrom,
            TransactionResult transaction)
        {
            var mainHeight = long.MaxValue;
            var checkResult = false;

            while (!checkResult)
            {
                var indexSideChainBlock = GetIndexSideHeight(servicesFrom);

                if (indexSideChainBlock < transaction.BlockNumber)
                {
                    _logger.Info("Block is not recorded ");
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
                    _logger.Info("Block is not recorded ");
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
            return services.CrossChainService.CallViewMethod<SInt64Value>(
                CrossChainContractMethod.GetParentChainHeight, new Empty()).Value;
        }

        private long GetIndexSideHeight(ContractServices services)
        {
            return MainServices.CrossChainService.CallViewMethod<SInt64Value>(
                CrossChainContractMethod.GetSideChainHeight, new SInt32Value {Value = services.ChainId}).Value;
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
                ToAddress = AddressHelper.Base58StringToAddress(services.TokenService.ContractAddress),
                ContractMethodName = TokenMethod.RegisterCrossChainTokenContractAddress.ToString(),
                ExpiredTime = DateTime.UtcNow.AddDays(1).ToTimestamp(),
                Params = input.ToByteString()
            };
            var createProposalResult =
                services.ParliamentService.ExecuteMethodWithResult(ParliamentMethod.CreateProposal,
                    createProposalInput);
            if (createProposalResult.Status.ConvertTransactionResultStatus() != TransactionResultStatus.Mined) return;
            var proposalId = createProposalResult.ReadableReturnValue.Replace("\"", "");

            //approve
            var miners = GetMiners(services);
            foreach (var miner in miners)
            {
                services.ParliamentService.SetAccount(miner.GetFormatted());
                var approveResult = services.ParliamentService.ExecuteMethodWithResult(ParliamentMethod.Approve, HashHelper.HexStringToHash(proposalId)
                    );
                if (approveResult.Status.ConvertTransactionResultStatus() != TransactionResultStatus.Mined) return;
            }

            services.ParliamentService.SetAccount(InitAccount);
            var releaseResult
                = services.ParliamentService.ExecuteMethodWithResult(ParliamentMethod.Release,
                    HashHelper.HexStringToHash(proposalId));
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

        private CrossChainMerkleProofContext GetCrossChainMerkleProofContext(ContractServices services,
            long blockHeight)
        {
            var crossChainMerkleProofContext =
                services.CrossChainService.CallViewMethod<CrossChainMerkleProofContext>(
                    CrossChainContractMethod.GetBoundParentChainHeightAndMerklePathByHeight, new SInt64Value
                    {
                        Value = blockHeight
                    });
            _logger.Info("Get CrossChain Merkle Proof");
            return crossChainMerkleProofContext;
        }

        #endregion
    }
}