namespace AElfChain.SDK
{
    public class AElfChainClient
    {
        public static IApiService GetClient(string baseUrl, int retryTimes = 1, int timeout = 60)
        {
            var sdkOption = new SdkOption
            {
                ServiceUrl = baseUrl,
                TimeoutSeconds = timeout,
                FailReTryTimes = retryTimes
            };
            
            return new ApiService(sdkOption);
        }
    }
}