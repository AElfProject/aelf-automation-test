using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AElf.Automation.EconomicSystemTest
{
    [TestClass]
    public class Token_Tests : TokenTests
    {
        [TestInitialize]
        public void InitializeTests()
        {
            Initialize();
        }

        [TestCleanup]
        public void CleanUpTests()
        {
            TestCleanUp();
        }

        [TestMethod]
        public void TransferAndApprove()
        {
            TokenInfos = new List<TokenInfo>();
            // create token
            for (var i = 0; i < 10; i++)
                foreach (var user in IssuerList)
                {
                    var symbol = $"ELF{RandomString(4, false)}";
                    var tokenName = $"token {symbol}";
                    Behaviors.CreateToken(user, symbol, tokenName);
                    var tokenInfo = new TokenInfo(symbol, tokenName, user);
                    TokenInfos.Add(tokenInfo);
                }

            // issue token
            foreach (var tokenInfo in TokenInfos)
            foreach (var user in UserList)
                Behaviors.IssueToken(tokenInfo.Issuer, tokenInfo.Symbol, user);

            Thread.Sleep(1000);

            for (var i = 0; i < 100; i++)
            {
                foreach (var user in UserList) Behaviors.TransferToken(InitAccount, user, 100);

                Thread.Sleep(1000);
                foreach (var user in UserList) Behaviors.ApproveToken(InitAccount, user, 100);

                Thread.Sleep(1000);
                foreach (var user in UserList) Behaviors.UnApproveToken(InitAccount, user, 50);

                Thread.Sleep(1000);
                foreach (var user in UserList) Behaviors.TransfterFromToken(InitAccount, user, 10);

                Thread.Sleep(1000);
                foreach (var user in UserList) Behaviors.BurnToken(10, user);

                Thread.Sleep(1000);
            }
        }


        private static string RandomString(int size, bool lowerCase)
        {
            var random = new Random(DateTime.Now.Millisecond);
            var builder = new StringBuilder(size);
            var startChar = lowerCase ? 97 : 65; //65 = A / 97 = a
            for (var i = 0; i < size; i++)
                builder.Append((char) (26 * random.NextDouble() + startChar));
            return builder.ToString();
        }
    }
}