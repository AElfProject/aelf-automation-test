using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Acs7;
using AElfChain.Common.Contracts;
using AElfChain.Common.Helpers;
using AElf.Automation.SideChain.Verification.Verify;
using AElf.Contracts.MultiToken;
using AElf.CSharp.Core.Utils;
using AElf.Types;
using AElfChain.SDK.Models;
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
        protected static Dictionary<int, List<string>> AccountList;
        protected static string NativeToken;
        private readonly EnvironmentInfo _environmentInfo;
        protected readonly int CreateTokenNumber;
        protected readonly int VerifyBlockNumber;
        protected readonly int VerifySideChainNumber;
        protected Dictionary<TransactionResultStatus, List<CrossChainTransactionInfo>> TransactionResultList;

        protected CrossChainBase()
        {
            var testEnvironment = ConfigInfoHelper.Config.TestEnvironment;
            _environmentInfo =
                ConfigInfoHelper.Config.EnvironmentInfos.Find(o => o.Environment.Contains(testEnvironment));
            CreateTokenNumber = ConfigInfoHelper.Config.CreateTokenNumber;
            VerifySideChainNumber = ConfigInfoHelper.Config.VerifySideChainNumber;
            VerifyBlockNumber = ConfigInfoHelper.Config.VerifyBlockNumber;
        }

        private string AccountDir { get; } = CommonHelper.GetCurrentDataDir();
        private static int Timeout { get; set; }
        protected static List<string> TokenSymbols { get; set; }

        protected ContractServices InitMainChainServices()
        {
            if (MainChainService != null) return MainChainService;

            var mainChainUrl = _environmentInfo.MainChainInfos.MainChainUrl;
            var password = _environmentInfo.MainChainInfos.Password;
            InitAccount = _environmentInfo.MainChainInfos.Account;
            MainChainService = new ContractServices(mainChainUrl, InitAccount, AccountDir, password);
            MainChainService.NodeManager.ApiService.SetFailReTryTimes(10);
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
                sideService.NodeManager.ApiService.SetFailReTryTimes(10);
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
                Amount = 1000000,
                Memo = "Issue side chain token",
                To = AddressHelper.Base58StringToAddress(account)
            });
        }


        protected string ExecuteMethodWithTxId(ContractServices services, string rawTx)
        {
            var transactionOutput =
                AsyncHelper.RunSync(() => services.NodeManager.ApiService.SendTransactionAsync(rawTx));

            return transactionOutput.TransactionId;
        }

        protected TransactionResultDto CheckTransactionResult(ContractServices services, string txId, int maxTimes = -1)
        {
            if (maxTimes == -1) maxTimes = Timeout == 0 ? 600 : Timeout;

            TransactionResultDto transactionResult = null;
            var checkTimes = 1;
            while (checkTimes <= maxTimes)
            {
                transactionResult =
                    AsyncHelper.RunSync(() => services.NodeManager.ApiService.GetTransactionResultAsync(txId));
                var status = transactionResult.Status.ConvertTransactionResultStatus();
                switch (status)
                {
                    case TransactionResultStatus.Mined:
                        Logger.Info($"Transaction {txId} status: {transactionResult.Status}");
                        return transactionResult;
                    case TransactionResultStatus.NotExisted:
                        
                        Logger.Error($"Transaction {txId} status: {transactionResult.Status}");
                        return transactionResult;
                    case TransactionResultStatus.Failed:
                    {
                        var message = $"Transaction {txId} status: {transactionResult.Status}";
                        message +=
                            $"\r\nMethodName: {transactionResult.Transaction.MethodName}, Parameter: {transactionResult.Transaction.Params}";
                        message += $"\r\nError Message: {transactionResult.Error}";
                        Logger.Error(message);
                        return transactionResult;
                    }
                }

                checkTimes++;
                Thread.Sleep(500);
            }

            Logger.Error("Transaction execute status cannot be 'Mined' after one minutes.");
            return transactionResult;
        }

        protected MerklePath GetMerklePath(ContractServices services, long blockNumber, string TxId)
        {
            var index = 0;
            var blockInfoResult =
                AsyncHelper.RunSync(() => services.NodeManager.ApiService.GetBlockByHeightAsync(blockNumber, true));
            var transactionIds = blockInfoResult.Body.Transactions;
            var transactionStatus = new List<string>();

            foreach (var transactionId in transactionIds)
            {
                var txResult = AsyncHelper.RunSync(() =>
                    services.NodeManager.ApiService.GetTransactionResultAsync(transactionId));
                var resultStatus = txResult.Status.ConvertTransactionResultStatus();
                if (resultStatus == TransactionResultStatus.NotExisted)
                {
                    Thread.Sleep(500);
                    Logger.Info("Check the transaction again");
                    AsyncHelper.RunSync(() =>
                        services.NodeManager.ApiService.GetTransactionResultAsync(transactionId));
                    resultStatus = txResult.Status.ConvertTransactionResultStatus();
                }

                transactionStatus.Add(resultStatus.ToString());
            }

            var txIdsWithStatus = new List<Hash>();
            for (var num = 0; num < transactionIds.Count; num++)
            {
                var txId = HashHelper.HexStringToHash(transactionIds[num]);
                var txRes = transactionStatus[num];
                var rawBytes = txId.ToByteArray().Concat(EncodingHelper.GetBytesFromUtf8String(txRes))
                    .ToArray();
                var txIdWithStatus = Hash.FromRawBytes(rawBytes);
                txIdsWithStatus.Add(txIdWithStatus);
                if (transactionIds[num] == TxId)
                {
                    index = num;
                    Logger.Info($"The transaction index is {index}");
                }
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
                    CrossChainContractMethod.GetBoundParentChainHeightAndMerklePathByHeight, new SInt64Value
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
                IssueChainId = MainChainService.ChainId,
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
            var txResult = CheckTransactionResult(services, txId);
            if (txResult == null)
                return null;
            // get transaction info            
            var status = txResult.Status.ConvertTransactionResultStatus();
            if (status == TransactionResultStatus.NotExisted || status == TransactionResultStatus.Failed)
            {
                Thread.Sleep(2000);
                Logger.Info("Check the transaction again.");
                txResult = CheckTransactionResult(services, txId);
                status = txResult.Status.ConvertTransactionResultStatus();
                if (status == TransactionResultStatus.NotExisted || status == TransactionResultStatus.Failed)
                    return null;
            }

            var blockNumber = txResult.BlockNumber;
            var receiveAccount = toAccount;
            var rawTxInfo = new CrossChainTransactionInfo(blockNumber, txId, rawTx, fromAccount, receiveAccount);
            return rawTxInfo;
        }

        protected CrossChainTransactionInfo CrossChainTransferWithTxId(ContractServices services, string symbol,
            string fromAccount, string toAccount, int toChainId,
            long amount)
        {
            //Transfer
            var crossChainTransferInput = new CrossChainTransferInput
            {
                Symbol = symbol,
                IssueChainId = MainChainService.ChainId,
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
            var txResult = CheckTransactionResult(services, info.TxId);
            if (txResult == null)
                return null;
            // get transaction info            
            var status = txResult.Status.ConvertTransactionResultStatus();
            if (status == TransactionResultStatus.NotExisted || status == TransactionResultStatus.Failed)
            {
                Thread.Sleep(2000);
                Logger.Info("Check the transaction again.");
                txResult = CheckTransactionResult(services, info.TxId);
                status = txResult.Status.ConvertTransactionResultStatus();
                if (status != TransactionResultStatus.Mined)
                    return null;
            }

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
            var blockHeight = services.NodeManager.ApiService.GetBlockHeightAsync().Result;
            return blockHeight;
        }

        protected long GetIndexParentHeight(ContractServices services)
        {
            return services.CrossChainService.CallViewMethod<SInt64Value>(
                CrossChainContractMethod.GetParentChainHeight, new Empty()).Value;
        }

        protected long GetIndexSideHeight(ContractServices services)
        {
            return MainChainService.CrossChainService.CallViewMethod<SInt64Value>(
                CrossChainContractMethod.GetSideChainHeight, new SInt32Value {Value = services.ChainId}).Value;
        }
        protected void GetVerifyResult(ContractServices services, Dictionary<string,bool> results)
        {
            foreach (var item in results)
            {
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
        }

        protected Dictionary<TransactionResultStatus, List<CrossChainTransactionInfo>> CheckoutTransferResult(
            ContractServices services, List<CrossChainTransactionInfo> lists)
        {
            TransactionResultList = new Dictionary<TransactionResultStatus, List<CrossChainTransactionInfo>>();
            var transactionFailed = new List<CrossChainTransactionInfo>();
            var transactionMined = new List<CrossChainTransactionInfo>();

            foreach (var list in lists)
            {
                var txResult = CheckTransactionResult(services, list.TxId);
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

            TransactionResultList.Add(TransactionResultStatus.Failed, transactionFailed);
            TransactionResultList.Add(TransactionResultStatus.Mined, transactionMined);
            return TransactionResultList;
        }

        private async Task<long> GetParentChainBlockIndexAsync(ContractServices services,
            CrossChainTransactionInfo infos)
        {
            var transactionHeight = infos.BlockHeight;
            long indexSideBlock = 0;

            while (indexSideBlock < transactionHeight)
            {
                Logger.Info("Block is not recorded ");
                Thread.Sleep(10000);

                indexSideBlock =
                    MainChainService.CrossChainService.CallViewMethod<SInt64Value>(
                        CrossChainContractMethod.GetSideChainHeight, new SInt32Value {Value = services.ChainId}).Value;
            }

            return await MainChainService.NodeManager.ApiService.GetBlockHeightAsync();
        }

        protected void CheckSideChainBlockIndexParentChainHeight(ContractServices services,
            CrossChainTransactionInfo infos)
        {
            var mainHeight = GetParentChainBlockIndexAsync(services, infos).Result;
            long indexParentBlock = 0;
            while (indexParentBlock < mainHeight)
            {
                Logger.Info("Block is not recorded ");
                Thread.Sleep(10000);

                indexParentBlock = GetIndexParentHeight(services);
            }
        }

        protected bool CheckSideChainBlockIndex(ContractServices services, CrossChainTransactionInfo infos)
        {
            var indexParentBlock =
                services.CrossChainService.CallViewMethod<SInt64Value>(
                    CrossChainContractMethod.GetParentChainHeight, new Empty());
            var transactionHeight = infos.BlockHeight;
            return indexParentBlock.Value > transactionHeight;
        }

        protected async void SideChainCheckSideChainBlockIndex(ContractServices servicesFrom,
            ContractServices servicesTo, CrossChainTransactionInfo infos)
        {
            var mainHeight = await MainChainService.NodeManager.ApiService.GetBlockHeightAsync();
            var checkResult = false;

            while (!checkResult)
            {
                var indexSideChainBlock = MainChainService.CrossChainService.CallViewMethod<SInt64Value>(
                    CrossChainContractMethod.GetSideChainHeight, new SInt32Value {Value = servicesFrom.ChainId});
                var indexParentBlock =
                    servicesTo.CrossChainService.CallViewMethod<SInt64Value>(
                        CrossChainContractMethod.GetParentChainHeight, new Empty());
                var transactionHeight = infos.BlockHeight;

                if (indexSideChainBlock.Value > transactionHeight && indexParentBlock.Value > mainHeight)
                {
                    checkResult = true;
                }
                else
                {
                    Logger.Info("Block is not recorded ");
                    Thread.Sleep(10000);
                }
            }
        }

        protected long GetBalance(ContractServices services, string address, string symbol)
        {
            var result =
                services.TokenService.CallViewMethod<GetBalanceOutput>(TokenMethod.GetBalance, new GetBalanceInput
                {
                    Owner = AddressHelper.Base58StringToAddress(address),
                    Symbol = symbol
                });
            return result.Balance;
        }

        protected List<string> NewAccount(ContractServices services, int count)
        {
            var accountList = new List<string>();
            for (var i = 0; i < count; i++)
            {
                var account = services.NodeManager.NewAccount();
                accountList.Add(account);
            }

            return accountList;
        }

        protected void UnlockAccounts(ContractServices services, int count, List<string> accountList)
        {
            services.NodeManager.ListAccounts();
            for (var i = 0; i < count; i++)
            {
                var result = services.NodeManager.UnlockAccount(accountList[i]);
                if (!result)
                    throw new Exception("Account unlock failed.");
            }
        }
    }
}