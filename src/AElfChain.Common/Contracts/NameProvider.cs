using System;

namespace AElfChain.Common.Contracts
{
    public enum NameProvider
    {
        Genesis,
        Consensus
    }

    public static class NameProviderExtension
    {
        public static NameProvider ConvertNameProvider(this string name)
        {
            return (NameProvider) Enum.Parse(typeof(NameProvider), name, true);
        }
    }
}