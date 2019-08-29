using System.Collections.Generic;
using AElf.Contracts.AssociationAuth;

namespace AElf.Automation.ProposalTest
{
    public class ReviewerInfo
    {
        public List<Reviewer> Reviewers;
        public readonly int TotalWeight;
        public readonly int MinWeight;
        public readonly int MaxWeight;

        public ReviewerInfo(List<Reviewer> reviewers)
        {
            Reviewers = reviewers;
            MinWeight = reviewers[0].Weight;
            MaxWeight = reviewers[0].Weight;

            foreach (var reviewer in reviewers)
            {
                TotalWeight += reviewer.Weight;

                if (MinWeight > reviewer.Weight)
                    MinWeight = reviewer.Weight;

                if (MaxWeight < reviewer.Weight)
                    MaxWeight = reviewer.Weight;
            }
        }
    }
}