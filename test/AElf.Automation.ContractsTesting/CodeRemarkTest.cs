using System.Collections.Generic;
using AElf.Automation.Common.Contracts;
using AElf.Automation.Common.Helpers;
using AElf.Automation.Common.Managers;
using AElf.Automation.Common.Utils;
using AElf.Contracts.TestContract.BasicFunctionWithParallel;
using AElf.Cryptography;
using AElf.Types;
using log4net;

namespace AElf.Automation.ContractsTesting
{
    public class CodeRemarkTest
    {
        public INodeManager NodeManager { get; set; }
        public ILog Logger = Log4NetHelper.GetLogger();
        public BasicWithParallelContract ParallelContract { get; set; }
        public string Tester = "2WFS2hJqtY8jhshuAGLWuwNKxqJ67qvmXygqPA27SMPWiY6GnB";
        public string ContractAddress = "2F5C128Srw5rHCXoSY2C7uT5sAku48mkgiaTTp1Hiprhbb7ED9";
        
        public CodeRemarkTest(INodeManager nodeManager)
        {
            NodeManager = nodeManager;
            ParallelContract = new BasicWithParallelContract(NodeManager, Tester, ContractAddress);
        }

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
            for (var i = 0; i < 100; i++)
            {
                var address = Address.FromPublicKey(CryptoHelper.GenerateKeyPair().PublicKey);
                var transactionId = ParallelContract.ExecuteMethodWithTxId(BasicParallelMethod.IncreaseWinMoney, new IncreaseWinMoneyInput
                {
                    First = Tester.ConvertAddress(), Second = address
                });
                toInfos.Add(address.GetFormatted());
                Logger.Info($"TransactionId: {transactionId}, From:{Tester}, To: {address.GetFormatted()}");
            }
            
            ParallelContract.CheckTransactionResultList();

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
            
            //query result
            
        }
    }
}