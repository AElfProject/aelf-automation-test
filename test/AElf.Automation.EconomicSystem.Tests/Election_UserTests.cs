using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace AElf.Automation.EconomicSystem.Tests
{
    [TestClass]
    public class ElectionUserTests : ElectionTests
    {
        public ElectionUserTests() : base()
        {
            Initialize();
        }
        [TestMethod]
        public void Vote_One_Candidate_Scenario()
        {
            
        }

        [TestMethod]
        public void Vote_One_Candidate_MultipleTimes_Scenario()
        {
            
        }

        [TestMethod]
        public void Vote_Multiple_Candidates_Scenario()
        {
            
        }

        [TestMethod]
        public void Vote_One_NonCandidate_Scenario()
        {
            
        }

        [TestMethod]
        public void Vote_One_Candidate_With_NotEnough_Token_Scenario()
        {
            
        }

        [TestMethod]
        [DataRow(0, 1, 2)]
        public void Vote_Three_Candidates_ForBP(int no1, int no2, int no3)
        {
            var voteResult1 = UserBehaviors.UserVote(UserList[0], FullNodeAddress[no1], 90, 100);
            voteResult1.GetJsonInfo();
            voteResult1.JsonInfo["result"]["Status"].ToString().ShouldBe("Mined");

            var voteResult2 = UserBehaviors.UserVote(UserList[1], FullNodeAddress[no2], 90, 200);
            voteResult2.GetJsonInfo();
            voteResult2.JsonInfo["result"]["Status"].ToString().ShouldBe("Mined");

            var voteResult3 = UserBehaviors.UserVote(UserList[2], FullNodeAddress[no3], 90, 300);
            voteResult3.GetJsonInfo();            
            voteResult3.JsonInfo["result"]["Status"].ToString().ShouldBe("Mined");

            for (var i = 0; i < FullNodeAddress.Count; i++)
            {
                var voteResult = UserBehaviors.UserVote(UserList[i+3], FullNodeAddress[i], 90, 50);
                voteResult.GetJsonInfo();
                voteResult.JsonInfo["result"]["Status"].ToString().ShouldBe("Mined");
            }
            
            //verify victories
            Query_Candidate_Victories(no1, no2, no3);
        }

        [TestMethod]
        [DataRow(0, 1, 2)]
        public void Query_Candidate_Victories(int no1, int no2, int no3)
        {
            var victories = QueryBehaviors.GetVictories();
            victories.Value.Count.ShouldBe(3);
            
            var publicKeys = victories.Value.Select(o => o.ToHex()).ToList();
            
            publicKeys.Contains(
                QueryBehaviors.ApiHelper.GetPublicKeyFromAddress(FullNodeAddress[no1])).ShouldBeTrue();
            publicKeys.Contains(
                QueryBehaviors.ApiHelper.GetPublicKeyFromAddress(FullNodeAddress[no2])).ShouldBeTrue();
            publicKeys.Contains(
                QueryBehaviors.ApiHelper.GetPublicKeyFromAddress(FullNodeAddress[no3])).ShouldBeTrue();
        }

        [TestMethod]
        public void Get_Candidate_List()
        {
        }
        
        [TestMethod]
        public void Query_Vote_Records()
        {
            
        }
    }
}