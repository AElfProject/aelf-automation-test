using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace AElf.Automation.EconomicSystem.Tests
{
    [TestClass]
    public class ElectionNodeTests : ElectionTests
    {
        public ElectionNodeTests() : base()
        {
            Initialize();
        }
        
        [TestMethod]
        public void First_Announcement_Election_Scenario()
        {
            var beforeBalance = QueryBehaviors.GetBalance(FullNodeAddress[0]).Balance; 
            var result = NodeBehaviors.AnnouncementElection(FullNodeAddress[0]);
            result.GetJsonInfo();
            result.JsonInfo["result"]["Status"].ToString().ShouldBe("Mined");
            
            var afterBalance = QueryBehaviors.GetBalance(FullNodeAddress[0]).Balance; 
            beforeBalance.ShouldBe(afterBalance + 100_000L);
        }

        [TestMethod]
        public void Announcement_Election_MultipleTimes_Scenario()
        {
            var beforeBalance = QueryBehaviors.GetBalance(FullNodeAddress[1]).Balance; 
            var result = NodeBehaviors.AnnouncementElection(FullNodeAddress[1]);
            result.GetJsonInfo();
            result.JsonInfo["result"]["Status"].ToString().ShouldBe("Mined");
            
            var result1 = NodeBehaviors.AnnouncementElection(FullNodeAddress[1]);
            result.GetJsonInfo();
            result.JsonInfo["result"]["Status"].ToString().ShouldBe("Failed");
            
            var result2 = NodeBehaviors.AnnouncementElection(FullNodeAddress[1]);
            result.GetJsonInfo();
            result.JsonInfo["result"]["Status"].ToString().ShouldBe("Failed");
            
            var afterBalance = QueryBehaviors.GetBalance(FullNodeAddress[1]).Balance;
            beforeBalance.ShouldBe(afterBalance + 100_000L);
        }

        [TestMethod]
        public void QuiteElection_And_Announcement_Again_Scenario()
        {
            var beforeBalance = QueryBehaviors.GetBalance(FullNodeAddress[0]).Balance; 
            
            var announcement1 = NodeBehaviors.AnnouncementElection(FullNodeAddress[0]);
            announcement1.Result.ShouldBeTrue();
            
            var quitElection = NodeBehaviors.QuitElection(FullNodeAddress[0]);
            quitElection.Result.ShouldBeTrue();
            
            var announcement2 = NodeBehaviors.AnnouncementElection(FullNodeAddress[0]);
            announcement2.Result.ShouldBeTrue();
            
            var afterBalance = QueryBehaviors.GetBalance(FullNodeAddress[0]).Balance;
            beforeBalance.ShouldBe(afterBalance + 100_000L);
        }

        [TestMethod]
        public void Announcement_AllNodes_scenario()
        {
            for (int i = 0; i < FullNodeAddress.Count; i++)
            {
                var result = NodeBehaviors.AnnouncementElection(FullNodeAddress[i]);
                result.GetJsonInfo();
                result.JsonInfo["result"]["Status"].ToString().ShouldBe("Mined");
            }
        }

        [TestMethod]
        public void Get_Miners_Count()
        {
            var miners = QueryBehaviors.GetMinersCount();
            miners.ShouldBe(3);
        }

        [TestMethod]
        [DataRow(1)]
        public void GetElectionResult(long termNumber)
        {
            var electionResult = QueryBehaviors.GetElectionResult(termNumber);
            
        }
    }
}