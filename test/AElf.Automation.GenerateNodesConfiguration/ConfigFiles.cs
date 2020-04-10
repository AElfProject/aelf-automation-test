using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using AElfChain.Common;
using AElfChain.Common.Helpers;

namespace AElf.Automation.NodesConfigGen
{
    public class GenerateInformation
    {
        private readonly List<string> _bootNodes;
        private string _minerInfo;

        public GenerateInformation()
        {
            _bootNodes = GetAllBootNodes();
        }

        public string GenerateBootNodeInfo(NodeInfo node)
        {
            var array = _bootNodes.FindAll(o => o != $"\"{node.IpAddress}:{node.NetPort}\"");
            return string.Join(",", array);
        }

        public string GenerateMinerInfo()
        {
            if (_minerInfo != null) return _minerInfo;
            var bps = ConfigInfoHelper.Config.BpNodes;
            var pubKeys = bps.Select(o => $"\"{o.PublicKey}\"").ToList();
            _minerInfo = string.Join(",", pubKeys);

            return _minerInfo;
        }

        private List<string> GetAllBootNodes()
        {
            var bpNodes = ConfigInfoHelper.Config.BpNodes;
            var fullNodes = ConfigInfoHelper.Config.FullNodes;
            var nodes = bpNodes.Concat(fullNodes);

            return nodes.Select(node => $"\"{node.IpAddress}:{node.NetPort}\"").ToList();
        }
    }

    public class ConfigFiles
    {
        private const string LogFile = "log4net.config";
        private const string MainNetFile = "appsettings.MainChain.MainNet.json";
        private const string SettingFile = "appsettings.json";

        private readonly NodeInfo _node;
        private readonly string _templateFolder = Path.Combine(CommonHelper.AppRoot, "templates");
        private readonly ILogHelper Logger = LogHelper.GetLogger();

        public ConfigFiles(NodeInfo node)
        {
            _node = node;
        }

        public void GenerateBasicConfigFile()
        {
            var desPath = Path.Combine(CommonHelper.AppRoot, "results", _node.Name);

            //copy log setting
            var logFile = Path.Combine(_templateFolder, LogFile);
            CommonHelper.CopyFiles(logFile, desPath);
            Logger.Info($"{LogFile} generate success.");

            //copy main chain net file
            var mainFile = Path.Combine(_templateFolder, MainNetFile);
            CommonHelper.CopyFiles(mainFile, desPath);
            Logger.Info($"{MainNetFile} generate success.");
        }

        public void GenerateSettingFile(GenerateInformation info)
        {
            var content = ReadFiles(SettingFile);

            //update db number
            content = content.Replace("[DB_NO]", _node.DbNo.ToString());

            //update account
            content = content.Replace("[ACCOUNT]", _node.Account);
            content = content.Replace("[PASSWORD]", NodeOption.DefaultPassword);

            //update api port
            content = content.Replace("[API_PORT]", _node.ApiPort.ToString());

            //update net port
            content = content.Replace("[NET_PORT]", _node.NetPort.ToString());

            //update miner and boot nodes
            var minerInfo = info.GenerateMinerInfo();
            content = content.Replace("[MINERLIST]", minerInfo);

            var bootInfo = info.GenerateBootNodeInfo(_node);
            content = content.Replace("[BOOT_NODE]", bootInfo);

            //save setting file
            SaveSettingFiles(content);
        }

        private void SaveSettingFiles(string content)
        {
            var settingPath = Path.Combine(CommonHelper.AppRoot, "results", _node.Name, SettingFile);
            File.WriteAllText(settingPath, content, Encoding.UTF8);
            Logger.Info($"{SettingFile} generate success.");
        }

        private string ReadFiles(string fileName)
        {
            var path = Path.Combine(_templateFolder, fileName);
            return File.ReadAllText(path);
        }
    }
}