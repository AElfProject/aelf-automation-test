using System;

namespace AElfChain.Common.Contracts
{
    public enum NameProvider
    {
        Genesis,
        Election,
        Profit,
        Vote,
        Treasury,
        Token,
        TokenHolder,
        TokenConverter,
        Consensus,
        ParliamentAuth,
        CrossChain,
        AssociationAuth,
        Configuration,
        ReferendumAuth,

        TestBasicFunction,
        TestUpdateFunction,
        TestBasicSecurity,
        TestPerformance,
        TestEvents,
        TestTransactionFees,
        TestA,
        TestB,
        TestC,
        ExecutionAcs5,
        ExecutionAcs8
    }

    public static class NameProviderExtension
    {
        public static NameProvider ConvertNameProvider(this string name)
        {
            return (NameProvider) Enum.Parse(typeof(NameProvider), name, true);
        }
    }
}