using System;
using System.Collections.Generic;
using AElfChain.Common.Contracts;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using AElfChain.Common.DtoExtension;
using AElf.Contracts.TestContract.BasicFunctionWithParallel;
using log4net;

namespace AElf.Automation.ContractsTesting
{
    public class CodeRemarkTest
    {
        public string ContractAddress = "2F5C128Srw5rHCXoSY2C7uT5sAku48mkgiaTTp1Hiprhbb7ED9";
        public ILog Logger = Log4NetHelper.GetLogger();
        public string Tester = "mPxf7UnKAGqkKRcwHTHv8Y9eTCG4vfbJpAfV1FLgMDS7wJGzt";

        public CodeRemarkTest(INodeManager nodeManager)
        {
            NodeManager = nodeManager;
            ParallelContract = new BasicWithParallelContract(NodeManager, Tester, ContractAddress);
        }

        public INodeManager NodeManager { get; set; }
        public BasicWithParallelContract ParallelContract { get; set; }

        public void ExecuteContractMethodTest()
        {
            //initialize
            ParallelContract.ExecuteMethodWithResult(BasicParallelMethod.InitialBasicFunctionWithParallelContract,
                new InitialBasicFunctionWithParallelContractInput
                {
                    ContractName = "Parallel testing",
                    Manager = Tester.ConvertAddress(),
                    MinValue = 100,
                    MaxValue = 100_000,
                    MortgageValue = 100_000_00000000
                });
            var toInfos = new List<string>();
            //execute some other transactions
            //transfer
            var accounts = new List<string>();
            var genesis = NodeManager.GetGenesisContract();
            var token = genesis.GetTokenContract();
            for (var i = 0; i < 100; i++)
            {
                var acc = NodeManager.GetRandomAccount();
                accounts.Add(acc);
                token.TransferBalance(Tester, acc, 100);
            }

            for (var i = 0; i < 100; i++)
            {
                ParallelContract.SetAccount(accounts[i]);
                var transactionId = ParallelContract.ExecuteMethodWithTxId(BasicParallelMethod.UserPlayBet, new BetInput
                {
                    Int64Value = (i + 1) * 100
                });

                Logger.Info($"TransactionId: {transactionId}");
            }

            ParallelContract.CheckTransactionResultList();
            Console.ReadLine();

//            for (var i = 0; i < 100; i++)
//            {
//                var address = Address.FromPublicKey(CryptoHelper.GenerateKeyPair().PublicKey);
//                var transactionId = ParallelContract.ExecuteMethodWithTxId(BasicParallelMethod.IncreaseWinMoney, new IncreaseWinMoneyInput
//                {
//                    First = Tester.ConvertAddress(), Second = address
//                });
//                toInfos.Add(address.GetFormatted());
//                Logger.Info($"TransactionId: {transactionId}, From:{Tester}, To: {address.GetFormatted()}");
//            }

            ParallelContract.CheckTransactionResultList();

            //query result
            foreach (var to in toInfos)
            {
                var result = ParallelContract.CallViewMethod<TwoUserMoneyOut>(BasicParallelMethod.QueryTwoUserWinMoney,
                    new QueryTwoUserWinMoneyInput
                    {
                        First = Tester.ConvertAddress(),
                        Second = to.ConvertAddress()
                    });
                Logger.Info($"to: {result.SecondInt64Value}");
            }
        }
    }
}