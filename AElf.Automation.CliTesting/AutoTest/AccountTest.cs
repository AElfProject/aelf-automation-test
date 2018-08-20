using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AElf.Automation.CliTesting.AutoTest
{
    [TestClass]
    public class AccountTest : BaseTest
    {
        [TestMethod]
        public void CreateAccount()
        {
            CommandRequest cmdReq = new CommandRequest("account new 123");
            cmdReq.Result = Instance.ExecuteCommand(cmdReq.Command, out cmdReq.InfoMessage, out cmdReq.ErrorMessage);

            Assert.IsTrue(cmdReq.Result);
        }

        [TestMethod]
        public void ListAccount()
        {
            CommandRequest cmdReq = new CommandRequest("account list");
            cmdReq.Result = Instance.ExecuteCommand(cmdReq.Command, out cmdReq.InfoMessage, out cmdReq.ErrorMessage);

            Assert.IsTrue(cmdReq.Result);
        }

        [TestMethod]
        public void UnlockAccount()
        {
            CommandRequest cmdReq = new CommandRequest($"account unlock {ACCOUNT} 123");
            cmdReq.Result = Instance.ExecuteCommand(cmdReq.Command, out cmdReq.InfoMessage, out cmdReq.ErrorMessage);

            Assert.IsTrue(cmdReq.Result);
        }

        [TestMethod]
        public void UnlockAccountAgain()
        {
            CommandRequest cmdReq = new CommandRequest($"account unlock {ACCOUNT} 123 notimeout");
            cmdReq.Result = Instance.ExecuteCommand(cmdReq.Command, out cmdReq.InfoMessage, out cmdReq.ErrorMessage);
            Assert.IsTrue(cmdReq.Result);

            CommandRequest accountReq = new CommandRequest($"account unlock {ACCOUNT} 123 notimeout");
            accountReq.Result = Instance.ExecuteCommand(accountReq.Command, out accountReq.InfoMessage, out accountReq.ErrorMessage);
            Assert.IsTrue(accountReq.Result);
        }

        [TestMethod]
        public void UnlockAccountWithWrongPassword()
        {
            CommandRequest cmdReq = new CommandRequest($"account unlock {ACCOUNT} 12345");
            cmdReq.Result = Instance.ExecuteCommand(cmdReq.Command, out cmdReq.InfoMessage, out cmdReq.ErrorMessage);

            Assert.IsTrue(cmdReq.Result);
        }
    }
}
