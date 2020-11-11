using System;
using System.Threading.Tasks;
using AElf.Client.Service;
using AElfChain.Common;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using log4net;
using McMaster.Extensions.CommandLineUtils;

namespace AElf.Automation.AssociationTest
{
    class Program
    {
        private static ILog Logger { get; set; }

        [Option("-e|--endpoint", Description = "Node service endpoint info")]
        public string Endpoint { get; set; } = "http://192.168.197.22:8000";

        [Option("-a|--account", Description = "User")]
        private static string Account { get; set; } = "28Y8JA1i2cN6oHvdv7EraXJr9a1gY6D1PpJXw9QtRMRwKcBQMK";
        
        [Option("-o|--organization", Description = "Organization")]
        private static string Organization { get; set; } = "";
        
        [Option("-t|--token", Description = "Organization create token")]
        private static string Token { get; set; } = "";

        static int Main(string[] args)
        {
            try
            {
                return CommandLineApplication.Execute<Program>(args);
            }
            catch (Exception ex)
            {
                Logger.Error($"Execute failed: {ex.Message}");
            }

            return 0;
        }
        
        private void OnExecute()
        {
            //Init Logger
            Log4NetHelper.LogInit("AssociationTest");
            Logger = Log4NetHelper.GetLogger();

            var nm = new NodeManager(Endpoint);
            var addMembers = new AddMembers(nm,Account);
            //before
            if (Organization.Equals(""))
                Organization = addMembers.CreateOrganization(Token).ToBase58();
            addMembers.TransferToMember(Organization);
            addMembers.CheckOrganization(Organization);
            addMembers.AddMemberTest(Organization);
        }
    }
}