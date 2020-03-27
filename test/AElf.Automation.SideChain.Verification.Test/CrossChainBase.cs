using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Acs7;
using AElf.Contracts.MultiToken;
using AElf.Types;
using AElfChain.Common;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using log4net;
using Volo.Abp.Threading;

namespace AElf.Automation.SideChain.Verification
{
    public class CrossChainBase
    {
        protected static readonly ILog Logger = Log4NetHelper.GetLogger();
        protected static string InitAccount;
        protected static ContractServices MainChainService;
        protected static List<ContractServices> SideChainServices;
        protected static List<string> AccountList;
        protected static string NativeToken;
        protected static List<string> PrimaryTokens;
        private readonly EnvironmentInfo _environmentInfo;
        protected readonly int Count;
        protected readonly int CreateTokenNumber;
        protected readonly int VerifyBlockNumber;
        protected readonly int VerifySideChainNumber;
        private Dictionary<TransactionResultStatus, List<CrossChainTransactionInfo>> _transactionResultList;

        protected CrossChainBase()
        {
            var testEnvironment = ConfigInfoHelper.Config.TestEnvironment;
            _environmentInfo =
                ConfigInfoHelper.Config.EnvironmentInfos.Find(o => o.Environment.Contains(testEnvironment));
            CreateTokenNumber = ConfigInfoHelper.Config.CreateTokenNumber;
            Count = ConfigInfoHelper.Config.TransferAccount;
            VerifySideChainNumber = ConfigInfoHelper.Config.VerifySideChainNumber;
            VerifyBlockNumber = ConfigInfoHelper.Config.VerifyBlockNumber;
            EnvCheck = EnvCheck.GetDefaultEnvCheck();
        }

        protected EnvCheck EnvCheck { get; set; }

        private string AccountDir { get; } = CommonHelper.GetCurrentDataDir();
        protected static List<string> TokenSymbols { get; set; }

        protected ContractServices InitMainChainServices()
        {
            if (MainChainService != null) return MainChainService;

            var mainChainUrl = _environmentInfo.MainChainInfos.MainChainUrl;
            var password = _environmentInfo.MainChainInfos.Password;
            InitAccount = _environmentInfo.MainChainInfos.Account;
            MainChainService = new ContractServices(mainChainUrl, InitAccount, AccountDir, password);
            NativeToken = MainChainService.PrimaryTokenSymbol;

            return MainChainService;
        }

        protected List<ContractServices> InitSideChainServices()
        {
            if (SideChainServices != null) return SideChainServices;
            SideChainServices = new List<ContractServices>();
            var sideChainInfos = _environmentInfo.SideChainInfos;
            var password = _environmentInfo.MainChainInfos.Password;
            foreach (var info in sideChainInfos)
            {
                var url = info.SideChainUrl;
                var sideService = new ContractServices(url, InitAccount, AccountDir, password);
                SideChainServices.Add(sideService);
            }

            return SideChainServices;
        }

        protected void IssueSideChainToken(ContractServices services, string account)
        {
            Logger.Info($"Issue side chain {services.ChainId} token {services.PrimaryTokenSymbol} to {account}");
            services.TokenService.SetAccount(services.CallAddress);
            services.TokenService.ExecuteMethodWithResult(TokenMethod.Issue, new IssueInput
            {
                Symbol = services.PrimaryTokenSymbol,
                Amount = 10000_00000000,
                Memo = "Issue side chain token",
                To = AddressHelper.Base58StringToAddress(account)
            });
        }

        protected void TransferToken(ContractServices services, string account)
        {
            Logger.Info($"Transfer token {services.PrimaryTokenSymbol} to {account}");
            services.TokenService.SetAccount(services.CallAddress);
            services.TokenService.ExecuteMethodWithResult(TokenMethod.Transfer, new TransferInput
            {
                Symbol = services.PrimaryTokenSymbol,
                Amount = 10000_00000000,
                Memo = "transfer side chain token",
                To = AddressHelper.Base58StringToAddress(account)
            });
        }

        protected bool IsSupplyAllToken(ContractServices services)
        {
            var symbol = services.PrimaryTokenSymbol;
            var tokenInfo = services.TokenService.GetTokenInfo(symbol);
            return tokenInfo.TotalSupply == tokenInfo.Supply + tokenInfo.Burned;
        }

        protected string ExecuteMethodWithTxId(ContractServices services, string rawTx)
        {
            var transactionId =
                services.NodeManager.SendTransaction(rawTx);

            return transactionId;
        }

        protected MerklePath GetMerklePath(ContractServices services, long blockNumber, string txId)
        {
            var index = 0;
            var blockInfoResult =
                AsyncHelper.RunSync(() => services.NodeManager.ApiClient.GetBlockByHeightAsync(blockNumber, true));
            var transactionIds = blockInfoResult.Body.Transactions;
            var transactionStatus = new List<string>();

            foreach (var transactionId in transactionIds)
            {
                var txResult = AsyncHelper.RunSync(() =>
                    services.NodeManager.ApiClient.GetTransactionResultAsync(transactionId));
                var resultStatus = txResult.Status.ConvertTransactionResultStatus();
                if (resultStatus == TransactionResultStatus.NotExisted)
                {
                    Thread.Sleep(500);
                    Logger.Info("Check the transaction again");
                    AsyncHelper.RunSync(() =>
                        services.NodeManager.ApiClient.GetTransactionResultAsync(transactionId));
                    resultStatus = txResult.Status.ConvertTransactionResultStatus();
                }

                transactionStatus.Add(resultStatus.ToString());
            }

            var txIdsWithStatus = new List<Hash>();
            for (var num = 0; num < transactionIds.Count; num++)
            {
                var transactionId = HashHelper.HexStringToHash(transactionIds[num]);
                var txRes = transactionStatus[num];
                var rawBytes = transactionId.ToByteArray().Concat(Encoding.UTF8.GetBytes(txRes))
                    .ToArray();
                var txIdWithStatus = Hash.FromRawBytes(rawBytes);
                txIdsWithStatus.Add(txIdWithStatus);
                if (transactionIds[num] != txId) continue;
                index = num;
                Logger.Info($"The transaction index is {index}");
            }

            var bmt = BinaryMerkleTree.FromLeafNodes(txIdsWithStatus);
            var root = bmt.Root;
            var merklePath = new MerklePath();
            merklePath.MerklePathNodes.AddRange(bmt.GenerateMerklePath(index).MerklePathNodes);
            return merklePath;
        }

        protected CrossChainMerkleProofContext GetCrossChainMerkleProofContext(ContractServices services,
            long blockHeight)
        {
            var crossChainMerkleProofContext =
                services.CrossChainService.CallViewMethod<CrossChainMerkleProofContext>(
                    CrossChainContractMethod.GetBoundParentChainHeightAndMerklePathByHeight, new Int64Value
                    {
                        Value = blockHeight
                    });
            Logger.Info("Get CrossChain Merkle Proof");
            return crossChainMerkleProofContext;
        }

        protected CrossChainTransactionInfo CrossChainTransferWithResult(ContractServices services, string symbol,
            string fromAccount, string toAccount, int toChainId,
            long amount)
        {
            //Transfer
            var crossChainTransferInput = new CrossChainTransferInput
            {
                Symbol = symbol,
                IssueChainId = services.ChainId,
                Amount = amount,
                Memo = "cross chain transfer",
                To = AddressHelper.Base58StringToAddress(toAccount),
                ToChainId = toChainId
            };
            // execute cross chain transfer
            var rawTx = services.NodeManager.GenerateRawTransaction(fromAccount,
                services.TokenService.ContractAddress, TokenMethod.CrossChainTransfer.ToString(),
                crossChainTransferInput);
            Logger.Info($"Transaction rawTx is: {rawTx}");
            var txId = ExecuteMethodWithTxId(services, rawTx);
            var txResult = services.NodeManager.CheckTransactionResult(txId);
            if (txResult == null)
                return null;
            // get transaction info            
            var blockNumber = txResult.BlockNumber;
            var receiveAccount = toAccount;
            var rawTxInfo = new CrossChainTransactionInfo(blockNumber, txId, rawTx, fromAccount, receiveAccount);
            return rawTxInfo;
        }

        protected CrossChainTransactionInfo CrossChainTransferWithTxId(ContractServices services, string symbol,
            string fromAccount, string toAccount, int toChainId, int issueChainId,
            long amount)
        {
            //Transfer
            var crossChainTransferInput = new CrossChainTransferInput
            {
                Symbol = symbol,
                IssueChainId = issueChainId,
                Amount = amount,
                Memo = "cross chain transfer",
                To = AddressHelper.Base58StringToAddress(toAccount),
                ToChainId = toChainId
            };
            // execute cross chain transfer
            var rawTx = services.NodeManager.GenerateRawTransaction(fromAccount,
                services.TokenService.ContractAddress, TokenMethod.CrossChainTransfer.ToString(),
                crossChainTransferInput);
            var txId = ExecuteMethodWithTxId(services, rawTx);
            Logger.Info($"Transaction rawTx is: {rawTx}, txId is {txId}");
            var info = new CrossChainTransactionInfo(txId, rawTx, fromAccount, toAccount);
            return info;
        }

        protected CrossChainTransactionInfo GetCrossChainTransferResult(ContractServices services,
            CrossChainTransactionInfo info)
        {
            var txResult = services.NodeManager.CheckTransactionResult(info.TxId);
            if (txResult == null)
                return null;
            // get transaction info            
            var status = txResult.Status.ConvertTransactionResultStatus();
            if (status == TransactionResultStatus.NotExisted || status == TransactionResultStatus.Failed)
                return null;

            var blockNumber = txResult.BlockNumber;
            var rawTxInfo = new CrossChainTransactionInfo(blockNumber, info.TxId, info.RawTx, info.FromAccount,
                info.ReceiveAccount);
            return rawTxInfo;
        }

        protected CrossChainReceiveTokenInput ReceiveFromMainChainInput(CrossChainTransactionInfo rawTxInfo)
        {
            var merklePath = GetMerklePath(MainChainService, rawTxInfo.BlockHeight, rawTxInfo.TxId);
            if (merklePath == null)
                return null;
            var crossChainReceiveToken = new CrossChainReceiveTokenInput
            {
                FromChainId = MainChainService.ChainId,
                ParentChainHeight = rawTxInfo.BlockHeight,
                MerklePath = merklePath
            };
            crossChainReceiveToken.TransferTransactionBytes =
                ByteString.CopyFrom(ByteArrayHelper.HexStringToByteArray(rawTxInfo.RawTx));

            return crossChainReceiveToken;
        }

        protected CrossChainReceiveTokenInput ReceiveFromSideChainInput(ContractServices fromServices,
            CrossChainTransactionInfo rawTxInfo)
        {
            var merklePath = GetMerklePath(fromServices, rawTxInfo.BlockHeight, rawTxInfo.TxId);
            if (merklePath == null) return null;

            var crossChainReceiveToken = new CrossChainReceiveTokenInput
            {
                FromChainId = fromServices.ChainId,
                MerklePath = merklePath
            };
            // verify side chain transaction
            var crossChainMerkleProofContext = GetCrossChainMerkleProofContext(fromServices, rawTxInfo.BlockHeight);
            crossChainReceiveToken.MerklePath.MerklePathNodes.AddRange(crossChainMerkleProofContext
                .MerklePathFromParentChain.MerklePathNodes);
            crossChainReceiveToken.ParentChainHeight = crossChainMerkleProofContext.BoundParentChainHeight;
            crossChainReceiveToken.TransferTransactionBytes =
                ByteString.CopyFrom(ByteArrayHelper.HexStringToByteArray(rawTxInfo.RawTx));

            return crossChainReceiveToken;
        }

        protected long GetBlockHeight(ContractServices services)
        {
            var blockHeight = services.NodeManager.ApiClient.GetBlockHeightAsync().Result;
            return blockHeight;
        }

        protected long GetIndexParentHeight(ContractServices services)
        {
            return services.CrossChainService.CallViewMethod<Int64Value>(
                CrossChainContractMethod.GetParentChainHeight, new Empty()).Value;
        }

        protected long GetIndexSideHeight(ContractServices services)
        {
            return MainChainService.CrossChainService.CallViewMethod<Int64Value>(
                CrossChainContractMethod.GetSideChainHeight, new Int32Value {Value = services.ChainId}).Value;
        }

        protected long MainChainCheckSideChainBlockIndex(ContractServices servicesFrom,
            long txHeight)
        {
            var mainHeight = long.MaxValue;
            var checkResult = false;

            while (!checkResult)
            {
                var indexSideChainBlock = GetIndexSideHeight(servicesFrom);

                if (indexSideChainBlock < txHeight)
                {
                    Console.WriteLine("Block is not recorded ");
                    Thread.Sleep(10000);
                    continue;
                }

                mainHeight = mainHeight == long.MaxValue
                    ? MainChainService.NodeManager.ApiClient.GetBlockHeightAsync().Result
                    : mainHeight;
                var indexParentBlock = GetIndexParentHeight(servicesFrom);
                checkResult = indexParentBlock > mainHeight;
            }

            return mainHeight;
        }

        protected void GetVerifyResult(ContractServices services, Dictionary<string, bool> results)
        {
            foreach (var item in results)
                switch (item.Value)
                {
                    case true:
                        Logger.Info($"Transaction {item.Key} on chain {services.ChainId} verify successfully.");
                        break;
                    case false:
                        Logger.Error($"Transaction {item.Key} on chain {services.ChainId} verify failed.");
                        break;
                }
        }

        protected Dictionary<TransactionResultStatus, List<CrossChainTransactionInfo>> CheckoutTransferResult(
            ContractServices services, List<CrossChainTransactionInfo> lists)
        {
            _transactionResultList = new Dictionary<TransactionResultStatus, List<CrossChainTransactionInfo>>();
            var transactionFailed = new List<CrossChainTransactionInfo>();
            var transactionMined = new List<CrossChainTransactionInfo>();

            foreach (var list in lists)
            {
                var txResult = services.NodeManager.CheckTransactionResult(list.TxId);
                var status = txResult.Status.ConvertTransactionResultStatus();
                switch (status)
                {
                    case TransactionResultStatus.Failed:
                        transactionFailed.Add(list);
                        break;
                    case TransactionResultStatus.Mined:
                        transactionMined.Add(list);
                        break;
                }
            }

            _transactionResultList.Add(TransactionResultStatus.Failed, transactionFailed);
            _transactionResultList.Add(TransactionResultStatus.Mined, transactionMined);
            return _transactionResultList;
        }

        protected void CheckAccountBalance(string symbol)
        {
            Logger.Info("Show the main chain account balance: ");

            foreach (var account in AccountList)
            {
                var accountBalance = MainChainService.TokenService.GetUserBalance(account, symbol);
                Logger.Info(
                    $"On main chain {MainChainService.ChainId} account:{account}, {symbol} balance is:{accountBalance}");
            }

            Logger.Info("Show the side chain account balance: ");
            foreach (var sideChain in SideChainServices)
            foreach (var account in AccountList)
            {
                var accountBalance = sideChain.TokenService.GetUserBalance(account, symbol);
                Logger.Info(
                    $"On side chain {sideChain.ChainId} account:{account},\n {symbol} balance is: {accountBalance}\n");
            }
        }

        protected bool CheckSideChainBlockIndex(ContractServices services, CrossChainTransactionInfo infos)
        {
            var indexParentBlock = GetIndexParentHeight(services);
            var transactionHeight = infos.BlockHeight;
            return indexParentBlock > transactionHeight;
        }

        protected void UnlockAccounts(ContractServices services, List<string> accountList)
        {
            services.NodeManager.ListAccounts();
            foreach (var account in accountList)
            {
                var result = services.NodeManager.UnlockAccount(account);
                if (!result)
                    throw new Exception("Account unlock failed.");
            }
        }
    }
}