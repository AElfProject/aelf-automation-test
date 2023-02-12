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
        protected static string RpcUrl { get; } = "http://127.0.0.1:8001";
        protected INodeManager NodeManager { get; set; }
        protected AuthorityManager AuthorityManager { get; set; }
        protected string InitAccount { get; } = "zptx91dhHVJjJRxf5Wg5KAoMrDrWX6i1H2FAyKAiv2q8VZfbg";
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
            FullNodeAddress.Add("2X9u7M3YWNUNXqbsTvCsbHkS2ncrTVsiCKsUtf8YRr3DZCQLb6");
            FullNodeAddress.Add("tb4qsxbzi4HLwSS4PM19yF89ww4nA1ELJXHP1mXB4ZPnNjCYc");
            
            ReplaceAddress = new List<string>();
            ReplaceAddress.Add("2NKnGrarMPTXFNMRDiYH4hqfSoZw72NLxZHzgHD1Q3xmNoqdmR");
            Voter = new List<string>();
            Voter.Add("2gfVsyYbLPehmVjZxKHZfxp9AMRUEV6KFHkZDgdU7VZf64teew");
            Voter.Add("2a6MGBRVLPsy6pu4SVMWdQqHS5wvmkZv8oas9srGWHJk7GSJPV");
            Voter.Add("ZczfBVksn1RhvfDXWVR7w5oagutgACiNzhwMdvVapEzcimWYz");
            Voter.Add("W4xEKTZcvPKXRAmdu9xEpM69ArF7gUxDh9MDgtsKnu7JfePXo");
            Voter.Add("NUddzDNy8PBMUgPCAcFW7jkaGmofDTEmr5DUoddXDpdR6E85X");
            Voter.Add("aFm1FWZRLt7V6wCBUGVmqxaDcJGv9HvYPDUVxF95C9L7sTwXp");
            Voter.Add("UZd2HWnZKkECcxh9fJYVKHowVtaE4xMi84UZdZYns9zchvKgR");
            
            Logger.Info($"{NodeManager.ApiClient.BaseUrl}");
            #endregion
        }
    }
}