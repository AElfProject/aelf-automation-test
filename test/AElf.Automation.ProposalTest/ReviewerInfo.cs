using System.Collections.Generic;
using AElf.Contracts.AssociationAuth;

namespace AElf.Automation.ProposalTest
{
    public class ReviewerInfo
    {
        public readonly int MaxWeight;
        public readonly int MinWeight;
        public readonly int TotalWeight;
        public List<Reviewer> Reviewers;

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