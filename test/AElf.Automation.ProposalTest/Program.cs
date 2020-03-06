using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElfChain.Common.Helpers;
using log4net;

namespace AElf.Automation.ProposalTest
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Log4NetHelper.LogInit("ProposalTest");
            TaskCollection = new List<Task>();

            var proposalParliament = new ProposalParliament();
            var proposalAssociation = new ProposalAssociation();
            var proposalReferendum = new ProposalReferendum();

            TaskCollection.Add(RunContinueJobWithInterval(proposalParliament.ParliamentJob, 100));
            TaskCollection.Add(RunContinueJobWithInterval(proposalAssociation.AssociationJob, 120));
            TaskCollection.Add(RunContinueJobWithInterval(proposalReferendum.ReferendumJob, 240));

            Task.WaitAll(TaskCollection.ToArray());
        }

        private static Task RunContinueJobWithInterval(Action action, int seconds)
        {
            void NewAction()
            {
                while (true)
                    try
                    {
                        action.Invoke();
                        Task.Delay(1000 * seconds);
                    }
                    catch (Exception e)
                    {
                        Logger.Error(e);
                        break;
                    }
            }

            return Task.Run(NewAction);
        }

        #region Private Properties

        private static readonly ILog Logger = Log4NetHelper.GetLogger();
        private static List<Task> TaskCollection { get; set; }

        #endregion
    }
}