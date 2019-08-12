using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Acs7;
using AElf.Automation.Common.Contracts;
using AElf.Automation.Common.Helpers;
using AElf.Automation.Common.WebApi.Dto;
using AElf.Automation.SideChain.Verification.Verify;
using AElf.Contracts.MultiToken.Messages;
using AElf.CSharp.Core.Utils;
using AElf.Kernel;
using AElf.Types;
using Google.Protobuf;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AElf.Automation.SideChain.Verification
{
    public class CrossChainBase
    {
        private string AccountDir { get; } = CommonHelper.GetCurrentDataDir();
        protected static readonly ILogHelper Logger = LogHelper.GetLogHelper();
        private readonly EnvironmentInfo _environmentInfo;
        private static int Timeout { get; set; }
        protected readonly string NativeToken = "ELF";
        protected static string InitAccount;
        protected readonly int CreateTokenNumber;
        protected readonly int VerifySideChainNumber;
        protected readonly int VerifyBlockNumber;
        protected static ContractServices MainChainService;
        protected static List<ContractServices> SideChainServices;
        protected static Dictionary<int, List<string>> AccountList;
        protected Dictionary<TransactionResultStatus, List<CrossChainTransactionInfo>> TransactionResultList;
        protected static List<string> TokenSymbol { get; set; }

        protected CrossChainBase()
        {
            var testEnvironment = ConfigInfoHelper.Config.TestEnvironment;
            _environmentInfo =
                ConfigInfoHelper.Config.EnvironmentInfos.Find(o => o.Environment.Contains(testEnvironment));
            CreateTokenNumber = ConfigInfoHelper.Config.CreateTokenNumber;
            VerifySideChainNumber = ConfigInfoHelper.Config.VerifySideChainNumber;
            VerifyBlockNumber = ConfigInfoHelper.Config.VerifyBlockNumber;
        }

        protected ContractServices InitMainChainServices()
        {
            if (MainChainService != null) return MainChainService;
            
            var mainChainUrl = _environmentInfo.MainChainInfos.MainChainUrl;
            var password = _environmentInfo.MainChainInfos.Password;
            InitAccount = _environmentInfo.MainChainInfos.Account;
            var mainChainId = ChainHelper.ConvertBase58ToChainId(_environmentInfo.MainChainInfos.MainChainId);
            MainChainService = new ContractServices(mainChainUrl, InitAccount, AccountDir, password, mainChainId);

            return MainChainService;
        }

        protected List<ContractServices> InitSideChainServices()
        {
            if (SideChainServices != null) return SideChainServices;
            var sideChainIds = new List<int>();
            SideChainServices = new List<ContractServices>();
            var sideChainInfos = _environmentInfo.SideChainInfos;
            var password = _environmentInfo.MainChainInfos.Password;
            foreach (var info in sideChainInfos)
            {
                var url = info.SideChainUrl;
                var chainId = ChainHelper.ConvertBase58ToChainId(info.SideChainId);
                sideChainIds.Add(chainId);
                var sideService = new ContractServices(url, InitAccount, AccountDir, password, chainId);
                SideChainServices.Add(sideService);
            }

            return SideChainServices;
        }

        protected void ExecuteContinuousTasks(IEnumerable<Action> actions, bool interrupted = true,
            int sleepSeconds = 0)
        {
            while (true)
            {
                try
                {
                    if (actions == null)
                        throw new ArgumentException("Action methods is null.");
                    ExecuteStandaloneTask(actions, sleepSeconds);
                }
                catch (Exception e)
                {
                    Logger.Error($"ExecuteContinuousTasks got exception: {e.Message}");
                    if (interrupted)
                        break;
                }
            }
        }

        protected void ExecuteStandaloneTask(IEnumerable<Action> actions, int sleepSeconds = 0)
        {
            try
            {
                foreach (var action in actions)
                {
                    action.Invoke();
                }

                if (sleepSeconds != 0)
                    Thread.Sleep(1000 * sleepSeconds);
            }
            catch (Exception e)
            {
                Logger.Error($"ExecuteStandaloneTask got exception: {e.Message}");
            }
        }

        protected string ExecuteMethodWithTxId(ContractServices services, string rawTx)
        {
            var ci = new CommandInfo(ApiMethods.SendTransaction)
            {
                Parameter = rawTx
            };
            services.ApiHelper.BroadcastWithRawTx(ci);
            if (ci.Result)
            {
                var transactionOutput = ci.InfoMsg as SendTransactionOutput;

                return transactionOutput?.TransactionId;
            }

            Assert.IsTrue(ci.Result, $"Execute contract failed. Reason: {ci.GetErrorMessage()}");

            return string.Empty;
        }

        protected CommandInfo CheckTransactionResult(ContractServices services, string txId, int maxTimes = -1)
        {
            if (maxTimes == -1)
            {
                maxTimes = Timeout == 0 ? 600 : Timeout;
            }

            CommandInfo ci = null;
            var checkTimes = 1;
            while (checkTimes <= maxTimes)
            {
                ci = new CommandInfo(ApiMethods.GetTransactionResult) {Parameter = txId};
                services.ApiHelper.GetTransactionResult(ci);
                if (ci.Result)
                {
                    if (ci.InfoMsg is TransactionResultDto transactionResult)
                    {
                        var status = transactionResult.Status.ConvertTransactionResultStatus();
                        switch (status)
                        {
                            case TransactionResultStatus.Mined:
                                Logger.Info($"Transaction {txId} status: {transactionResult.Status}");
                                return ci;
                            case TransactionResultStatus.NotExisted:
                                Logger.Error($"Transaction {txId} status: {transactionResult.Status}");
                                return ci;
                            case TransactionResultStatus.Failed:
                            {
                                var message = $"Transaction {txId} status: {transactionResult.Status}";
                                message +=
                                    $"\r\nMethodName: {transactionResult.Transaction.MethodName}, Parameter: {transactionResult.Transaction.Params}";
                                message += $"\r\nError Message: {transactionResult.Error}";
                                Logger.Error(message);
                                return ci;
                            }
                        }
                    }
                }

                checkTimes++;
                Thread.Sleep(500);
            }

            if (ci != null)
            {
                var result = ci.InfoMsg as TransactionResultDto;
                Logger.Error(result?.Error);
            }

            Logger.Error("Transaction execute status cannot be 'Mined' after one minutes.");
            return ci;
        }

        protected MerklePath GetMerklePath(ContractServices services, long blockNumber, string TxId)
        {
            var index = 0;
            var ci = new CommandInfo(ApiMethods.GetBlockByHeight) {Parameter = $"{blockNumber} true"};
            ci = services.ApiHelper.ExecuteCommand(ci);
            var blockInfoResult = ci.InfoMsg as BlockDto;
            var transactionIds = blockInfoResult.Body.Transactions;
            var transactionStatus = new List<string>();

            foreach (var transactionId in transactionIds)
            {
                ci = new CommandInfo(ApiMethods.GetTransactionResult) {Parameter = transactionId};
                var result = services.ApiHelper.ExecuteCommand(ci);
                if (!(result.InfoMsg is TransactionResultDto txResult)) return null;
                var resultStatus = txResult.Status.ConvertTransactionResultStatus();
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
                }
            }

            var bmt = new BinaryMerkleTree();
            bmt.AddNodes(txIdsWithStatus);
            var root = bmt.ComputeRootHash();
            var merklePath = new MerklePath();
            merklePath.Path.AddRange(bmt.GenerateMerklePath(index));
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
            var crossChainTransferInput = new CrossChainTransferInput()
            {
                Symbol = symbol,
                IssueChainId = MainChainService.ChainId,
                Amount = amount,
                Memo = "cross chain transfer",
                To = AddressHelper.Base58StringToAddress(toAccount),
                ToChainId = toChainId,
            };
            // execute cross chain transfer
            var rawTx = services.ApiHelper.GenerateTransactionRawTx(fromAccount,
                services.TokenService.ContractAddress, TokenMethod.CrossChainTransfer.ToString(),
                crossChainTransferInput);
            Logger.Info($"Transaction rawTx is: {rawTx}");
            var txId = ExecuteMethodWithTxId(services, rawTx);
            var result = CheckTransactionResult(services, txId);
            if (result == null)
                return null;
            // get transaction info            
            var txResult = result.InfoMsg as TransactionResultDto;
            var status = txResult.Status.ConvertTransactionResultStatus();
            if (status == TransactionResultStatus.NotExisted || status == TransactionResultStatus.Failed)
            {
                Thread.Sleep(2000);
                Logger.Info("Check the transaction again.");
                result = CheckTransactionResult(services, txId);
                txResult = result.InfoMsg as TransactionResultDto;
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
            var crossChainTransferInput = new CrossChainTransferInput()
            {
                Symbol = symbol,
                IssueChainId = MainChainService.ChainId,
                Amount = amount,
                Memo = "cross chain transfer",
                To = AddressHelper.Base58StringToAddress(toAccount),
                ToChainId = toChainId,
            };
            // execute cross chain transfer
            var rawTx = services.ApiHelper.GenerateTransactionRawTx(fromAccount,
                services.TokenService.ContractAddress, TokenMethod.CrossChainTransfer.ToString(),
                crossChainTransferInput);
            var txId = ExecuteMethodWithTxId(services, rawTx);
            Logger.Info($"Transaction rawTx is: {rawTx}, txId is {txId}");
            var info = new CrossChainTransactionInfo(txId, rawTx,fromAccount,toAccount);
            return info;
        }

        protected CrossChainTransactionInfo GetCrossChainTransferResult(ContractServices services,CrossChainTransactionInfo info)
        {
            var result = CheckTransactionResult(services, info.TxId);
            if (result == null)
                return null;
            // get transaction info            
            var txResult = result.InfoMsg as TransactionResultDto;
            var status = txResult.Status.ConvertTransactionResultStatus();
            if (status == TransactionResultStatus.NotExisted || status == TransactionResultStatus.Failed)
            {
                Thread.Sleep(2000);
                Logger.Info("Check the transaction again.");
                result = CheckTransactionResult(services,info.TxId);
                txResult = result.InfoMsg as TransactionResultDto;
                status = txResult.Status.ConvertTransactionResultStatus();
                if (status == TransactionResultStatus.NotExisted || status == TransactionResultStatus.Failed)
                    return null;
            }

            var blockNumber = txResult.BlockNumber;
            var rawTxInfo = new CrossChainTransactionInfo(blockNumber, info.TxId, info.RawTx, info.FromAccount, info.ReceiveAccount);
            return rawTxInfo;
        }

        protected CrossChainReceiveTokenInput ReceiveFromMainChainInput(CrossChainTransactionInfo rawTxInfo)
        {
            var merklePath = GetMerklePath(MainChainService, rawTxInfo.BlockHeight, rawTxInfo.TxId);
            if (merklePath == null)
                return null;
            var crossChainReceiveToken = new CrossChainReceiveTokenInput
            {
                FromChainId = 9992731,
                ParentChainHeight = rawTxInfo.BlockHeight
            };
            crossChainReceiveToken.MerklePath.AddRange(merklePath.Path);
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
            };
            crossChainReceiveToken.MerklePath.AddRange(merklePath.Path);

            // verify side chain transaction
            var crossChainMerkleProofContext = GetCrossChainMerkleProofContext(fromServices, rawTxInfo.BlockHeight);
            crossChainReceiveToken.MerklePath.AddRange(crossChainMerkleProofContext.MerklePathForParentChainRoot.Path);
            crossChainReceiveToken.ParentChainHeight = crossChainMerkleProofContext.BoundParentChainHeight;
            crossChainReceiveToken.TransferTransactionBytes =
                ByteString.CopyFrom(ByteArrayHelper.HexStringToByteArray(rawTxInfo.RawTx));

            return crossChainReceiveToken;
        }

        protected long GetBlockHeight(ContractServices services)
        {
            var blockHeight = services.ApiHelper.ApiService.GetBlockHeight().Result;
            return blockHeight;
        }

        protected void CheckoutVerifyResult(ContractServices services, IEnumerable<string> txIds)
        {
            var verifyResultList = new List<CrossChainTransactionVerifyResult>();
            foreach (var txId in txIds)
            {
                var result = CheckTransactionResult(services, txId);
                if (result == null) return;
                var txResult = result.InfoMsg as TransactionResultDto;
                var status = txResult.Status.ConvertTransactionResultStatus();
                if (status != TransactionResultStatus.Mined) continue;
                var verifyResult =
                    new CrossChainTransactionVerifyResult(txResult.ReadableReturnValue, services.ChainId, txId);
                verifyResultList.Add(verifyResult);
            }

            foreach (var item in verifyResultList)
            {
                switch (item.Result)
                {
                    case "true":
                        Logger.Info($"On chain {item.ChainId}, transaction {item.TxId} Verify successfully.");
                        break;
                    case "false":
                        Logger.Error($"On chain {item.ChainId}, transaction {item.TxId} Verify failed.");
                        break;
                    default:
                        continue;
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
                var result = CheckTransactionResult(services, list.TxId);
                var txResult = result.InfoMsg as TransactionResultDto;
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

        protected List<string> GenerateTestUsers(IApiHelper helper, int count)
        {
            var accounts = new List<string>();
            Parallel.For(0, count, i =>
            {
                var command = new CommandInfo(ApiMethods.AccountNew, "123");
                command = helper.NewAccount(command);
                var account = command.InfoMsg.ToString();
                accounts.Add(account);
            });

            return accounts;
        }

        protected List<string> NewAccount(ContractServices services, int count)
        {
            var accountList = new List<string>();
            for (var i = 0; i < count; i++)
            {
                var ci = new CommandInfo(ApiMethods.AccountNew) {Parameter = "123"};
                ci = services.ApiHelper.ExecuteCommand(ci);
                Assert.IsTrue(ci.Result);
                accountList.Add(ci.InfoMsg.ToString());
            }

            return accountList;
        }

        protected void UnlockAccounts(ContractServices services, int count, List<string> accountList)
        {
            services.ApiHelper.ListAccounts();

            for (var i = 0; i < count; i++)
            {
                var ci = new CommandInfo(ApiMethods.AccountUnlock)
                {
                    Parameter = $"{accountList[i]} 123 notimeout"
                };
                ci = services.ApiHelper.ExecuteCommand(ci);
                Assert.IsTrue(ci.Result);
            }
        }
    }
}