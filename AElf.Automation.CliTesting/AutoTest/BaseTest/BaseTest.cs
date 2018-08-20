using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AElf.Automation.CliTesting.AutoTest
{
    [TestClass]
    public class BaseTest
    {
        public static AElfCliProgram Instance { get; set; }
        public static string RpcUrl = "http://127.0.0.1:8000";
        public static string ACCOUNT { get; set; }

        [TestInitialize]
        public void InitTestClass()
        {
            Console.WriteLine();
            InitCli.InitCliCommand(RpcUrl);
            Instance = InitCli.CliInstance;
            ACCOUNT = ReadAccount();
        }

        [TestCleanup]
        public void CleanupTestClass()
        {
            InitCli.CleanCliCommand();
            Console.WriteLine();
        }

        public string ReadAccount()
        {
            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AccountInfo.json");
            string fileContent = File.ReadAllText(filePath);
            var accountInfo = JsonConvert.DeserializeObject<JObject>(fileContent);
            return accountInfo["account"].ToString();
        }
    }
}
