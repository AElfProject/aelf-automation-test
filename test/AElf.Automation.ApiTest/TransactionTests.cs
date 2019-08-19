using System.Threading.Tasks;
using AElf.Automation.Common.Contracts;
using AElf.Automation.Common.Helpers;
using AElf.Contracts.MultiToken;
using AElf.Types;
using AElfChain.SDK.Models;
using Newtonsoft.Json;
using Xunit;

namespace AElf.Automation.ApiTest
{
    public partial class ChainApiTests
    {
        private WebApiHelper _apiHelper;
        
        [Fact]
        public async Task SendTransactions_Test()
        {
            Log4NetHelper.LogInit();
            var token = DeployTokenContract();
            var symbol = $"ELF{CommonHelper.RandomString(4, false)}";
            var chainStatus = await _client.GetChainStatusAsync();
            var rawTransactionOutput = await  _client.CreateRawTransactionAsync(new CreateRawTransactionInput
            {
                From = token.CallAddress,
                To = token.ContractAddress,
                MethodName = "Create",
                Params = JsonConvert.SerializeObject(new CreateInput
                {
                    Symbol = symbol,
                    TokenName = $"elf token {symbol}",
                    TotalSupply = long.MaxValue,
                    Decimals = 2,
                    Issuer = AddressHelper.Base58StringToAddress(token.CallAddress),
                    IsBurnable = true
                }),
                RefBlockHash = chainStatus.BestChainHash,
                RefBlockNumber = chainStatus.BestChainHeight
            });
            
            var transactionId =
                Hash.FromRawBytes(ByteArrayHelper.HexStringToByteArray(rawTransactionOutput.RawTransaction));
            var signature = _apiHelper.TransactionManager.Sign(token.CallAddress, transactionId.ToByteArray());
            var result = await _client.ExecuteRawTransactionAsync(new ExecuteRawTransactionDto
            {
                RawTransaction = rawTransactionOutput.RawTransaction,
                Signature = signature.ToHex()
            });
        }

        private TokenContract DeployTokenContract()
        {
            _apiHelper = new WebApiHelper(ServiceUrl);
            var cmdResult = _apiHelper.AccountManager.NewAccount("123");
            var account = cmdResult.InfoMsg.ToString();
            
            return new TokenContract(_apiHelper,account);
        }
    }
}