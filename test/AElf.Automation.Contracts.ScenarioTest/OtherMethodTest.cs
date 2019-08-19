using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using AElf.Automation.Common.Contracts;
using AElf.Automation.Common.Helpers;
using AElf.Automation.Common.OptionManagers;
using AElf.Automation.Common.Utils;
using AElf.Types;
using Google.Protobuf;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ProtoBuf;

namespace AElf.Automation.Contracts.ScenarioTest
{
    [TestClass]
    public class OtherMethodTest
    {
        [TestInitialize]
        public void TestInitialize()
        {
            Log4NetHelper.LogInit();
        }
        
        [TestMethod]
        public void ConvertFromHex()
        {
            var message = DataHelper.ConvertHexToString(
                "454c465f32476b44317137344877427246734875666d6e434b484a7661475642596b6d59636447337565624573415753737058");
            Assert.IsTrue(message == "ELF_2GkD1q74HwBrFsHufmnCKHJvaGVBYkmYcdG3uebEsAWSspX");
        }

        [TestMethod]
        public void ConvertTest()
        {
            string rpcMessage =
                "{\"result\":\"Mined\", \"message\":\"Test successful.\", \"return_code\":\"90000\", \"detail\":{\"info\":\"successful\"}}";
            var result1 = DataHelper.TryGetValueFromJson(out var message1, rpcMessage, "return_code");
            var result2 = DataHelper.TryGetValueFromJson(out var message2, rpcMessage, "detail", "info");
        }

        [TestMethod]
        public void ProtoMessageRead_Test()
        {
            var address = AddressUtils.Generate();
            var stream = new MemoryStream();
            address.WriteTo(stream);
            
            var info = new Address();
            info.MergeFrom(stream);
            var value = info.GetFormatted();
        }

        [TestMethod]
        public void AccountCreate()
        {
            var dataDir = CommonHelper.GetCurrentDataDir();
            var keyStore = new AElfKeyStore(dataDir);
            var accountManager = new AccountManager(keyStore);
            for (var i = 0; i < 10; i++)
            {
                var accountInfo = accountManager.NewAccount("123");
                var account = accountInfo.InfoMsg.ToString();
                Console.WriteLine($"Account: {account}");

                var publicKey = accountManager.GetPublicKey(account, "123");
                Console.WriteLine($"Public Key: {publicKey}");

                Console.WriteLine();
            }
        }
    }
}