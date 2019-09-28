using System.Threading.Tasks;
using AElf.Automation.Common.Helpers;
using AElf.Types;
using Xunit;

namespace AElf.Automation.ApiTest
{
    public partial class ChainApiTests
    {
        [Fact]
        public async Task GetContractFileDescriptorSet_Test()
        {
            //not exist address
            for (var i = 0; i < 10; i++)
            {
                var bytes = CommonHelper.GenerateRandombytes(32);
                var randomAddress = Address.FromPublicKey(bytes);

                var (_, timeSpan) =
                    await _listener.ExecuteApi(o => _client.GetContractFileDescriptorSetAsync(randomAddress.GetFormatted()));

                _testOutputHelper.WriteLine($"Address: {randomAddress.GetFormatted()}, execute time: {timeSpan}ms");
            }
            
            //exist one 
            var chainStatus = await _client.GetChainStatusAsync();
            var gensisContract = chainStatus.GenesisContractAddress;

            for (var i = 0; i < 10; i++)
            {
                var (_, timeSpan) =
                    await _listener.ExecuteApi(o => _client.GetContractFileDescriptorSetAsync(gensisContract));

                _testOutputHelper.WriteLine($"Address: {gensisContract}, execute time: {timeSpan}ms");
            }
        }
    }
}