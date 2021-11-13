using System;
using System.Collections.Generic;
using AElf.Contracts.MultiToken;
using AElf.Contracts.Profit;
using AElf.Types;
using AElfChain.Common;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using log4net;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AElf.Automation.EconomicSystemTest
{
    public class ElectionTests
    {
        protected static readonly ILog Logger = Log4NetHelper.GetLogger();

        protected Behaviors Behaviors;
        protected static string RpcUrl { get; } = "http://192.168.66.9:8000";
        protected INodeManager NodeManager { get; set; }
        protected AuthorityManager AuthorityManager { get; set; }
        protected string InitAccount { get; } = "28Y8JA1i2cN6oHvdv7EraXJr9a1gY6D1PpJXw9QtRMRwKcBQMK";
        protected List<string> BpNodeAddress { get; set; }
        protected List<string> FullNodeAddress { get; set; }
        protected List<string> ReplaceAddress { get; set; }
        protected List<string> Voter { get; set; }
        public Dictionary<SchemeType, Scheme> Schemes { get; set; }
        
        protected void Initialize()
        {
            //Init Logger
            Log4NetHelper.LogInit("ElectionTest");

            #region Get services

            NodeManager = new NodeManager(RpcUrl);
            var contractServices = new ContractManager(NodeManager, InitAccount);
            Behaviors = new Behaviors(contractServices,InitAccount);
            AuthorityManager = Behaviors.AuthorityManager;
            Schemes = ProfitContract.Schemes;
            #endregion

            #region Basic Preparation
            
            BpNodeAddress = new List<string>();
             BpNodeAddress.Add("J6zgLjGwd1bxTBpULLXrGVeV74tnS2n74FFJJz7KNdjTYkDF6");
             BpNodeAddress.Add("2oKcAgFCi2FxwyQFzCVnmNYdKZzJLyA983gEwUmyuuaVUX2d1P");
             BpNodeAddress.Add("zptx91dhHVJjJRxf5Wg5KAoMrDrWX6i1H2FAyKAiv2q8VZfbg");
             BpNodeAddress.Add("2wHUPreWWSTrXy6LW9L4o6psJ37MVzJu7CvnJmZbWdDY6E7VYg");
             BpNodeAddress.Add("2bs2uYMECtHWjB57RqgqQ3X2LrxgptWHtzCqGEU11y45aWimh4");

            FullNodeAddress = new List<string>();
            FullNodeAddress.Add("2frDVeV6VxUozNqcFbgoxruyqCRAuSyXyfCaov6bYWc7Gkxkh2");

            ReplaceAddress = new List<string>();
            ReplaceAddress.Add("2NKnGrarMPTXFNMRDiYH4hqfSoZw72NLxZHzgHD1Q3xmNoqdmR");
            Voter = new List<string>();
            Voter.Add("2gfVsyYbLPehmVjZxKHZfxp9AMRUEV6KFHkZDgdU7VZf64teew");
            Voter.Add("2a6MGBRVLPsy6pu4SVMWdQqHS5wvmkZv8oas9srGWHJk7GSJPV");

            Logger.Info($"{NodeManager.ApiClient.BaseUrl}");

            #endregion
        }
    }
}