using AElf.Contracts.MultiToken;
using AElf.Contracts.TransferWrapperContract;
using AElf.Types;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using log4net;
using Shouldly;

namespace AElf.Automation.BasicTransaction
{
    public class TransactionAction: BasicAction
    {
        public TransactionAction()
        {
            GetService();
        }
        
        public TransferWrapperContract DeployWrapperContract(TokenContract tokenAddress)
        {
            var contractAddress =
                AuthorityManager.DeployContract(InitAccount,
                    "AElf.Contracts.TransferWrapperContract", Password);
            var wrapperContract =
                new TransferWrapperContract(NodeManager, InitAccount, contractAddress.ToBase58());
            var initialize = wrapperContract.Initialize(tokenAddress.Contract, InitAccount, Password);
            initialize.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            return wrapperContract;
        }
        
        public TokenContract DeployTokenContract()
        {
            var tokenAddress = AuthorityManager.DeployContract(InitAccount,
                "AElf.Contracts.MultiToken", Password);
            var token = new TokenContract(NodeManager, InitAccount, tokenAddress.ToBase58());
            return token;
        }

        public string CreateAndIssueTokenForWrapper(Address tokenAddress)
        {
            var token = new TokenContract(NodeManager, InitAccount, tokenAddress.ToBase58());
            var symbol = GenerateNotExistTokenSymbol(token);
            var transaction = token.ExecuteMethodWithResult(TokenMethod.Create, new CreateInput
            {
                Symbol = symbol,
                TokenName = $"elf token {symbol}",
                TotalSupply = 10_0000_0000_00000000L,
                Decimals = 8,
                Issuer = InitAccount.ConvertAddress(),
                IsBurnable = true
            });
            transaction.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var issueToken = token.IssueBalance(InitAccount, InitAccount, 10_0000_0000_00000000, symbol);
            issueToken.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var balance = token.GetUserBalance(InitAccount, symbol);
            balance.ShouldBe(10_0000_0000_00000000);
            return symbol;
        }

        public void TransferFromAccount(TokenContract token, string symbol)
        {
            var balance = token.GetUserBalance(InitAccount,symbol);
            var testBalance = token.GetUserBalance(TestAccount, symbol);
            var result = token.TransferBalance(InitAccount, TestAccount, TransferAmount, symbol);
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            
            var afterBalance = token.GetUserBalance(InitAccount,symbol);
            var afterTestBalance = token.GetUserBalance(TestAccount,symbol);
            
            afterBalance.ShouldBe(balance - TransferAmount);
            afterTestBalance.ShouldBe(testBalance + TransferAmount);
            Logger.Info($"Before transfer from account balance is {balance}, to account balance is {testBalance}");
            Logger.Info($"After transfer from account balance is {afterBalance}, to account balance is {afterTestBalance}");
        }

        public void TransferFromContract(TokenContract token, TransferWrapperContract wrapper, string symbol)
        {
            var result = token.TransferBalance(InitAccount, wrapper.ContractAddress, TransferAmount * 2, symbol);
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var balance = token.GetUserBalance(wrapper.ContractAddress, symbol);
            var testBalance = token.GetUserBalance(TestAccount, symbol);
            var txResult = wrapper.ExecuteMethodWithResult(TransferWrapperMethod.ContractTransfer,
                new ThroughContractTransferInput
                {
                    Symbol = symbol,
                    To = TestAccount.ConvertAddress(),
                    Amount = TransferAmount
                });
            txResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            
            var afterBalance = token.GetUserBalance(wrapper.ContractAddress, symbol);
            var afterTestBalance = token.GetUserBalance(TestAccount, symbol);
            
            afterBalance.ShouldBe(balance - TransferAmount);
            afterTestBalance.ShouldBe(testBalance + TransferAmount);
            
            Logger.Info($"Before transfer from account balance is {balance}, to account balance is {testBalance}");
            Logger.Info($"After transfer from account balance is {afterBalance}, to account balance is {afterTestBalance}");
        }
        
        public void TransferFromVirtual(TokenContract token, TransferWrapperContract wrapper, string symbol)
        {
            var virtualAccount = GetFromVirtualAccounts(wrapper);
            var result = token.TransferBalance(InitAccount, virtualAccount.ToBase58(), TransferAmount * 2, symbol);
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            
            var balance = token.GetUserBalance(virtualAccount.ToBase58(), symbol);
            var testBalance = token.GetUserBalance(TestAccount, symbol);
            var txResult = wrapper.ExecuteMethodWithResult(TransferWrapperMethod.ThroughContractTransfer,
                new ThroughContractTransferInput
                {
                    Symbol = symbol,
                    To = TestAccount.ConvertAddress(),
                    Amount = TransferAmount
                });
            txResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            
            var afterBalance = token.GetUserBalance(virtualAccount.ToBase58(), symbol);
            var afterTestBalance = token.GetUserBalance(TestAccount, symbol);
            
            afterBalance.ShouldBe(balance - TransferAmount);
            afterTestBalance.ShouldBe(testBalance + TransferAmount);
            
            Logger.Info($"Before transfer from account balance is {balance}, to account balance is {testBalance}");
            Logger.Info($"After transfer from account balance is {afterBalance}, to account balance is {afterTestBalance}");
        }

        private static readonly ILog Logger = Log4NetHelper.GetLogger();
    }
}