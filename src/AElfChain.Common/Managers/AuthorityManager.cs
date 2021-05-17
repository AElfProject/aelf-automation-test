using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AElf;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Types;
using AElfChain.Common.Contracts;
using AElfChain.Common.Helpers;
using Google.Protobuf.WellKnownTypes;
using log4net;

namespace AElfChain.Common.Managers
{
    public class AuthorityManager
    {
        private static readonly ILog Logger = Log4NetHelper.GetLogger();
        private readonly ConsensusContract _consensus;
        private readonly GenesisContract _genesis;
        private NodesInfo _info;

        public AuthorityManager(INodeManager nodeManager, string caller = "", bool getNodeInfo = true)
        {
            if (getNodeInfo)
                GetConfigNodeInfo();

            if (caller == "")
                caller = _info.Nodes.First().Account;
            NodeManager = nodeManager;
            _genesis = GenesisContract.GetGenesisContract(nodeManager, caller);
            _consensus = _genesis.GetConsensusContract();
        }

        public INodeManager NodeManager { get; set; }

        public Address DeployContract(string caller, string contractName, string password = "")
        {
            Logger.Info($"Deploy contract: {contractName}");
            var contractPath = GetContractFilePath(contractName);
            var code = File.ReadAllBytes(contractPath);
            code = GenerateUniqContractCode(code);

            return _genesis.DeployContract(caller, code, password);
        }

        public List<string> GetCurrentMiners()
        {
            var minerList = new List<string>();
            var miners =
                _consensus.CallViewMethod<MinerList>(ConsensusMethod.GetCurrentMinerList, new Empty());
            foreach (var minersPubkey in miners.Pubkeys)
            {
                var miner = Address.FromPublicKey(minersPubkey.ToByteArray());
                minerList.Add(miner.ToBase58());
            }

            return minerList;
        }

        public long GetPeriod()
        {
            return _consensus.GetCurrentTermInformation().TermNumber;
        }

        private void GetConfigNodeInfo()
        {
            var nodes = NodeInfoHelper.Config;
            nodes.CheckNodesAccount();

            _info = nodes;
        }

        private string GetContractFilePath(string contractName)
        {
            var localPath = CommonHelper.MapPath("aelf/contracts");
            var defaultPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "aelf", "contracts");
            var contractPaths = new List<string>();
            if (contractName.Contains("\\") || contractName.Contains("/"))
            {
                contractPaths.Add(contractName);
            }
            else if (contractName.Contains(".dll"))
            {
                contractPaths.Add(Path.Combine(localPath, contractName));
                contractPaths.Add(Path.Combine(defaultPath, contractName));
            }
            else
            {
                contractPaths.Add(Path.Combine(localPath, $"{contractName}.dll.patched"));
                contractPaths.Add(Path.Combine(defaultPath, $"{contractName}.dll.patched"));
                contractPaths.Add(Path.Combine(localPath, $"{contractName}.dll"));
                contractPaths.Add(Path.Combine(defaultPath, $"{contractName}.dll"));
            }

            foreach (var path in contractPaths)
            {
                var exist = File.Exists(path);
                if (exist)
                {
                    Logger.Info($"Deploy contract file: {path}");
                    return path;
                }
            }

            throw new FileNotFoundException($"contract file {contractName} not found.");
        }

        private byte[] GenerateUniqContractCode(byte[] code)
        {
            while (true)
            {
                var hash = HashHelper.ComputeFrom(code);
                var registration =
                    _genesis.CallViewMethod<SmartContractRegistration>(
                        GenesisMethod.GetSmartContractRegistrationByCodeHash,
                        hash);
                if (registration.Equals(new SmartContractRegistration())) return code;
                Logger.Info($"Change code:");
                code = CodeInjectHelper.ChangeContractCodeHash(code);
            }
        }

        private byte[] CheckCode(byte[] code)
        {
            while (true)
            {
                Logger.Info($"Change code:");
                code = CodeInjectHelper.ChangeContractCodeHash(code);
                var hash = HashHelper.ComputeFrom(code);

                var registration =
                    _genesis.CallViewMethod<SmartContractRegistration>(
                        GenesisMethod.GetSmartContractRegistrationByCodeHash,
                        hash);
                if (registration.Equals(new SmartContractRegistration())) return code;
            }
        }
    }
}