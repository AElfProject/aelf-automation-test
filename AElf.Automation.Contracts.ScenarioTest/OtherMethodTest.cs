using System;
using System.Collections.Generic;
using System.Text;
using AElf.Automation.Common.Contracts;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AElf.Automation.Contracts.ScenarioTest
{
    [TestClass]
    public class OtherMethodTest
    {
        [TestMethod]
        public void ConvertFromHex()
        {
            var message = BaseContract.ConvertHexToString("454c465f32476b44317137344877427246734875666d6e434b484a7661475642596b6d59636447337565624573415753737058");
            Assert.IsTrue(message == "ELF_2GkD1q74HwBrFsHufmnCKHJvaGVBYkmYcdG3uebEsAWSspX");
        }
    }
}
