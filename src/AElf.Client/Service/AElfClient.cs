using System;
using System.Linq;
using System.Threading.Tasks;
using AElf.Client.Dto;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Address = AElf.Types.Address;
using Hash = AElf.Types.Hash;

namespace AElf.Client.Service
{
    public interface IClientService
    {
        Task<bool> IsConnected();
        Task<string> GetFormattedAddress(Address address);
        Task<string> GetGenesisContractAddressAsync();
        Task<Address> GetContractAddressByName(Hash contractNameHash);
        string GetAddressFromPubKey(string pubKey);
    }

    public partial class AElfClient : IClientService
    {
        private readonly IHttpService _httpService;
        public string BaseUrl { get; set; }

        private const string ExamplePrivateKey = "09da44778f8db2e602fb484334f37df19e221c84c4582ce5b7770ccfbc3ddbef";

        public AElfClient(string baseUrl, int timeOut = 60)
        {
            _httpService = new HttpService(timeOut);
            BaseUrl = baseUrl;
            
        }

        /// <summary>
        /// Verify whether this sdk successfully connects the chain.
        /// </summary>
        /// <returns>IsConnected or not</returns>
        public async Task<bool> IsConnected()
        {
            try
            {
                var chainStatus = await GetChainStatusAsync();
                return chainStatus != null;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Get the address of genesis contract.
        /// </summary>
        /// <returns>Address</returns>
        public async Task<string> GetGenesisContractAddressAsync()
        {
            var statusDto = await GetChainStatusAsync();
            var genesisAddress = statusDto.GenesisContractAddress;

            return genesisAddress;
        }

        /// <summary>
        /// Get address of a contract by given contractNameHash.
        /// </summary>
        /// <param name="contractNameHash"></param>
        /// <param name="privateKeyHex"></param>
        /// <returns>Address</returns>
        public async Task<Address> GetContractAddressByName(Hash contractNameHash)
        {
            var from = GetAddressFromPrivateKey(ExamplePrivateKey);
            var to = await GetGenesisContractAddressAsync();
            var transaction = await GenerateTransaction(from, to, "GetContractAddressByName", contractNameHash);
            var txWithSig = SignTransaction(ExamplePrivateKey, transaction);

            var response = await ExecuteTransactionAsync(new ExecuteTransactionDto
            {
                RawTransaction = txWithSig.ToByteArray().ToHex()
            });
            var byteArray = ByteArrayHelper.HexStringToByteArray(response);
            var address = Address.Parser.ParseFrom(byteArray);

            return address;
        }

        /// <summary>
        /// Build a transaction from the input parameters.
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="methodName"></param>
        /// <param name="input"></param>
        /// <returns>Transaction unsigned</returns>
        public async Task<Transaction> GenerateTransaction(string from, string to,
            string methodName, IMessage input)
        {
            try
            {
                AssertValidAddress(from, to);
                var chainStatus = await GetChainStatusAsync();
                var transaction = new Transaction
                {
                    From = AddressHelper.Base58StringToAddress(from),
                    To = AddressHelper.Base58StringToAddress(to),
                    MethodName = methodName,
                    Params = input.ToByteString(),
                    RefBlockNumber = chainStatus.BestChainHeight,
                    RefBlockPrefix = ByteString.CopyFrom(HashHelper.HexStringToHash(chainStatus.BestChainHash).Value
                        .Take(4).ToArray())
                };

                return transaction;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Convert the Address to the displayed string：symbol_base58-string_base58-string-chain-id
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public async Task<string> GetFormattedAddress(Address address)
        {
            var tokenContractAddress = await GetContractAddressByName(Hash.FromString("AElf.ContractNames.Token"));
            var fromAddress = GetAddressFromPrivateKey(ExamplePrivateKey);
            var toAddress = tokenContractAddress.GetFormatted();
            var methodName = "GetPrimaryTokenSymbol";
            var param = new Empty();

            var transaction = await GenerateTransaction(fromAddress, toAddress, methodName, param);
            var txWithSign = SignTransaction(ExamplePrivateKey, transaction);

            var result = await ExecuteTransactionAsync(new ExecuteTransactionDto
            {
                RawTransaction = txWithSign.ToByteArray().ToHex()
            });

            var symbol = StringValue.Parser.ParseFrom(ByteArrayHelper.HexStringToByteArray(result));
            var chainIdString = (await GetChainStatusAsync()).ChainId;

            return $"{symbol.Value}_{fromAddress}_{chainIdString}";
        }

        /// <summary>
        /// Sign a transaction using private key.
        /// </summary>
        /// <param name="privateKeyHex"></param>
        /// <param name="transaction"></param>
        /// <returns>Transaction signed</returns>
        public Transaction SignTransaction(string privateKeyHex, Transaction transaction)
        {
            var transactionData = transaction.GetHash().ToByteArray();

            // Sign the hash
            var privateKey = ByteArrayHelper.HexStringToByteArray(privateKeyHex);
            var signature = CryptoHelper.SignWithPrivateKey(privateKey, transactionData);
            transaction.Signature = ByteString.CopyFrom(signature);

            return transaction;
        }

        /// <summary>
        /// Get the account address through the public key.
        /// </summary>
        /// <param name="pubKey"></param>
        /// <returns>Account</returns>
        public string GetAddressFromPubKey(string pubKey)
        {
            var publicKey = ByteArrayHelper.HexStringToByteArray(pubKey);
            var address = Address.FromPublicKey(publicKey);
            return address.GetFormatted();
        }
        
        /// <summary>
        /// Get the account address through the private key.
        /// </summary>
        /// <param name="privateKeyHex"></param>
        /// <returns></returns>
        public string GetAddressFromPrivateKey(string privateKeyHex)
        {
            var address = Address.FromPublicKey(GetAElfKeyPair(privateKeyHex).PublicKey);
            return address.GetFormatted();
        }

        public Address GetBase58String(string base58String)
        {
            return AddressHelper.Base58StringToAddress(base58String);
        }

        #region private methods

        private ECKeyPair GetAElfKeyPair(string privateKeyHex)
        {
            var privateKey = ByteArrayHelper.HexStringToByteArray(privateKeyHex);
            var keyPair = CryptoHelper.FromPrivateKey(privateKey);

            return keyPair;
        }
        
        private string GetRequestUrl(string baseUrl, string relativeUrl)
        {
            return new Uri(new Uri(baseUrl + (baseUrl.EndsWith("/") ? "" : "/")), relativeUrl).ToString();
        }

        private void AssertValidAddress(params string[] addresses)
        {
            try
            {
                foreach (var address in addresses)
                {
                    AddressHelper.Base58StringToAddress(address);
                }
            }
            catch (Exception)
            {
                throw new AElfClientException(Error.Message[Error.InvalidAddress]);
            }
        }

        private void AssertValidHash(params string[] hashes)
        {
            try
            {
                foreach (var hash in hashes)
                {
                    HashHelper.HexStringToHash(hash);
                }
            }
            catch (Exception)
            {
                throw new AElfClientException(Error.Message[Error.InvalidBlockHash]);
            }
        }
        
        private void AssertValidTransactionId(params string[] transactionIds)
        {
            try
            {
                foreach (var transactionId in transactionIds)
                {
                    HashHelper.HexStringToHash(transactionId);
                }
            }
            catch (Exception)
            {
                throw new AElfClientException(Error.Message[Error.InvalidTransactionId]);
            }
        }

        #endregion
    }
}