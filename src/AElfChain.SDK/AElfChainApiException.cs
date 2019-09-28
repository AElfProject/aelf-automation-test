using System;

namespace AElfChain.SDK
{
    public class AElfChainApiException : Exception
    {
        public AElfChainApiException()
        {
        }

        public AElfChainApiException(string message) :
            base(message)
        {
        }

        public AElfChainApiException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}