using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace AElf.Automation.EconomicSystem.Tests
{
    [TestClass]
    public class OtherTests
    {
        [TestMethod]
        public void DictionaryTest()
        {
            var dic = new Dictionary<string, long>();

            dic.ContainsKey("test").ShouldBeFalse();
            
            dic.Add("test", 123);
            dic.ContainsKey("test").ShouldBeTrue();
        }
    }
}