using System;
using AElf.Automation.Common.Helpers;
using FluentScheduler;
using log4net;

namespace AElf.Automation.ProposalTest
{
    class Program
    {
        #region Private Properties

        private static readonly ILog Logger = Log4NetHelper.GetLogger();

        #endregion

        static void Main(string[] args)
        {
            Log4NetHelper.LogInit("ProposalTest");

            var proposalParliament = new ProposalParliament();
            var proposalAssociation = new ProposalAssociation();
            var proposalReferendum = new ProposalReferendum();

            JobManager.UseUtcTime();
            var registry = new Registry();

            registry.Schedule(() => proposalParliament.ParliamentJob()).WithName("Parliament")
                .ToRunEvery(30).Seconds();
            registry.Schedule(() => proposalAssociation.AssociationJob()).WithName("Association")
                .ToRunEvery(60).Seconds();
//            registry.Schedule(() => proposalReferendum.ReferendumJob()).WithName("Referendum")
//                .ToRunEvery(600).Seconds();


            JobManager.Initialize(registry);
            JobManager.JobException += info =>
                Logger.Error($"Error job: {info.Name}, Error message: {info.Exception.Message}");

            Console.ReadLine();
        }
    }
}