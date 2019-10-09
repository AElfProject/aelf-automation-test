using System;

namespace AElf.Automation.Common.Contracts
{
    public enum NameProvider
    {
        Genesis,
        Election,
        Profit,
        Vote,
        Treasury,
        Token,
        TokenConverter,
        Consensus,
        ParliamentAuth,
        CrossChain,
        AssociationAuth,
        Configuration,
        ReferendumAuth,

        TestBasicFunction,
        TestPerformance
    }

    public static class NameProviderExtension
    {
        public static NameProvider ConvertNameProvider(this string name)
        {
            return (NameProvider) Enum.Parse(typeof(NameProvider), name, true);
        }
    }
}