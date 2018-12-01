using System;
using System.IO;
using ServiceStack;

namespace AElf.Automation.Common.Helpers
{
    public class SmartContractReader
    {
        private const string ContractExtension = ".dll";
        private const string ContractFolderName = "contracts";
        
        private readonly string _dataDirectory;

        public SmartContractReader()
        {
            _dataDirectory = ApplicationHelper.GetDefaultDataDir();
        }

        public byte[] Read(string name)
        {
            try
            {
                byte[] code = null;
                using (FileStream file = File.OpenRead(GetKeyFileFullPath(name)))
                {
                    code = file.ReadFully();
                }

                return code;
            }
            catch (Exception e)
            {
                Console.WriteLine("SmartContractReader: Invalid transaction data.");
                Console.WriteLine("Exception: " + e.Message);
                return null;
            }
        }
        
        /// <summary>
        /// Return the full path of the files 
        /// </summary>
        internal string GetKeyFileFullPath(string address)
        {
            string dirPath = GetKeystoreDirectoryPath();
            string filePath = Path.Combine(dirPath, address);

            string filePathWithExtension = filePath + ContractExtension;

            return filePathWithExtension;
        }

        internal DirectoryInfo GetOrCreateContractDir()
        {
            try
            {
                string dirPath = GetKeystoreDirectoryPath();
                return Directory.CreateDirectory(dirPath);
            }
            catch (Exception e)
            {
                return null;
            }
        }

        internal string GetKeystoreDirectoryPath()
        {
            return Path.Combine(_dataDirectory, ContractFolderName);
        }
    }
}