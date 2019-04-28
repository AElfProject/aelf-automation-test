using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace AElf.Automation.EconomicSystem.Tests
{
    [TestClass]
    public class NodeTests : ElectionTests
    {
        [TestInitialize]
        public void InitializeNodeTests()
        {
            base.Initialize();
        }

        [TestCleanup]
        public void CleanUpNodeTests()
        {
            base.TestCleanUp();
        }
        
        [TestMethod]
        public void First_Announcement_Election_Scenario()
        {
            var beforeBalance = Behaviors.GetBalance(FullNodeAddress[0]).Balance; 
            var result = Behaviors.AnnouncementElection(FullNodeAddress[0]);
            result.GetJsonInfo();
            result.JsonInfo["result"]["Status"].ToString().ShouldBe("Mined");
            
            var afterBalance = Behaviors.GetBalance(FullNodeAddress[0]).Balance; 
            beforeBalance.ShouldBe(afterBalance + 100_000L);
        }

        [TestMethod]
        public void Announcement_Election_MultipleTimes_Scenario()
        {
            var beforeBalance = Behaviors.GetBalance(FullNodeAddress[1]).Balance; 
            var result = Behaviors.AnnouncementElection(FullNodeAddress[1]);
            result.GetJsonInfo();
            result.JsonInfo["result"]["Status"].ToString().ShouldBe("Mined");
            
            var result1 = Behaviors.AnnouncementElection(FullNodeAddress[1]);
            result.GetJsonInfo();
            result.JsonInfo["result"]["Status"].ToString().ShouldBe("Failed");
            
            var result2 = Behaviors.AnnouncementElection(FullNodeAddress[1]);
            result.GetJsonInfo();
            result.JsonInfo["result"]["Status"].ToString().ShouldBe("Failed");
            
            var afterBalance = Behaviors.GetBalance(FullNodeAddress[1]).Balance;
            beforeBalance.ShouldBe(afterBalance + 100_000L);
        }

        [TestMethod]
        public void Announcement_Election_With_NotEnough_Token_Scenario()
        {
            var beforeBalance = Behaviors.GetBalance(UserList[0]).Balance; 
            var result = Behaviors.AnnouncementElection(UserList[0]);
            result.GetJsonInfo();
            result.JsonInfo["result"]["Status"].ToString().ShouldBe("Failed");
            
            var afterBalance = Behaviors.GetBalance(UserList[0]).Balance; 
            beforeBalance.ShouldBe(afterBalance);
        }

        [TestMethod]
        public void QuiteElection_And_Announcement_Again_Scenario()
        {
            var beforeBalance = Behaviors.GetBalance(FullNodeAddress[5]).Balance; 
            
            var announcement1 = Behaviors.AnnouncementElection(FullNodeAddress[5]);
            announcement1.GetJsonInfo();
            announcement1.JsonInfo["result"]["Status"].ToString().ShouldBe("Mined");
            
            var quitElection = Behaviors.QuitElection(FullNodeAddress[5]);
            quitElection.GetJsonInfo();
            quitElection.JsonInfo["result"]["Status"].ToString().ShouldBe("Mined");
            
            var announcement2 = Behaviors.AnnouncementElection(FullNodeAddress[5]);
            announcement2.GetJsonInfo();
            announcement2.JsonInfo["result"]["Status"].ToString().ShouldBe("Mined");
            
            var afterBalance = Behaviors.GetBalance(FullNodeAddress[5]).Balance;
            beforeBalance.ShouldBe(afterBalance + 100_000L);
            
        }

        [TestMethod]
        public void Announcement_AllNodes_scenario()
        {
            for (int i = 0; i < FullNodeAddress.Count; i++)
            {
                var result = Behaviors.AnnouncementElection(FullNodeAddress[i]);
                result.GetJsonInfo();
                result.JsonInfo["result"]["Status"].ToString().ShouldBe("Mined");
            }
        }

        [TestMethod]
        public void Get_Miners_Count()
        {
            var miners = Behaviors.GetMinersCount();
            miners.ShouldBe(3);
        }

        [TestMethod]
        [DataRow(0)]
        public void GetVotesInformationResult(int nodeId)
        {
            var records = Behaviors.GetVotesInformationWithAllRecords(FullNodeAddress[nodeId]);

            var tickets = records.AllObtainedVotesAmount;
            tickets.ShouldBe(100);
        }

        [TestMethod]
        public void GetVictories()
        {
            var victories = Behaviors.GetVictories();

            var publicKeys = victories.Value.Select(o => o.ToHex()).ToList();
            
            publicKeys.Contains(Behaviors.ApiHelper.GetPublicKeyFromAddress(FullNodeAddress[0])).ShouldBeTrue();
            publicKeys.Contains(Behaviors.ApiHelper.GetPublicKeyFromAddress(FullNodeAddress[1])).ShouldBeTrue();
            publicKeys.Contains(Behaviors.ApiHelper.GetPublicKeyFromAddress(FullNodeAddress[2])).ShouldBeTrue();
        }
        
        [TestMethod]
        [DataRow(5)]
        public void QuitElection(int nodeId)
        {
            var beforeBalance = Behaviors.GetBalance(FullNodeAddress[nodeId]).Balance; 
            var result = Behaviors.QuitElection(FullNodeAddress[nodeId]);
            result.GetJsonInfo();
            result.JsonInfo["result"]["Status"].ToString().ShouldBe("Mined");
            
            var afterBalance = Behaviors.GetBalance(FullNodeAddress[nodeId]).Balance; 
            beforeBalance.ShouldBe(afterBalance - 100_000L);
        }

        [TestMethod]
        public void GetCandidates()
        {
            var candidates = Behaviors.GetCandidates();
            candidates.Value.Count.ShouldBe(6);
        }
    }
}