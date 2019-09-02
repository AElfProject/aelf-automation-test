using System;
using System.IO;

namespace AElf.Automation.Common.Helpers
{
    public class SmartContractReader
    {
        private const string ContractExtension = ".dll";
        private const string ContractFolderName = "contracts";

        private readonly string _dataDirectory;

        public SmartContractReader()
        {
            _dataDirectory = CommonHelper.GetDefaultDataDir();
        }

        public byte[] Read(string name)
        {
            try
            {
                byte[] code;
                using (var file = File.OpenRead(GetKeyFileFullPath(name)))
                {
                    code = file.GetAllBytes();
                }

                return code;
            }
            catch (Exception e)
            {
                Console.WriteLine("SmartContractReader: Invalid transaction data.");
                Console.WriteLine($"Exception: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// Return the full path of the files 
        /// </summary>
        private string GetKeyFileFullPath(string address)
        {
            var dirPath = GetKeystoreDirectoryPath();
            var filePath = Path.Combine(dirPath, address);

            var filePathWithExtension = filePath + ContractExtension;

            return filePathWithExtension;
        }

        private string GetKeystoreDirectoryPath()
        {
            return Path.Combine(_dataDirectory, ContractFolderName);
        }
    }
}