using System;
using System.Collections.Generic;
using System.Linq;
using AElfChain.Common.Helpers;

namespace AElf.Automation.NodesConfigGen
{
    public class ConfigCheck
    {
        private readonly List<NodeInfo> _allNodes;
        private readonly ILogHelper _logger = LogHelper.GetLogger();
        private readonly NodesInformation _nodes;

        public ConfigCheck()
        {
            _nodes = ConfigInfoHelper.Config;
            _allNodes = _nodes.BpNodes.Concat(_nodes.FullNodes).ToList();
        }

        public bool CheckNodeName()
        {
            if (_allNodes.Distinct().Count() == _allNodes.Count) return true;

            _logger.Error("Check name: Nodes contains duplicate name.");
            throw new Exception();
        }

        public bool CheckOtherNumbers()
        {
            var ipList = _allNodes.Select(o => o.IpAddress).Distinct().ToList();
            foreach (var ip in ipList)
            {
                var dbNoList = _allNodes.FindAll(o => o.IpAddress == ip).Select(o => o.DbNo).ToList();
                if (dbNoList.Distinct().Count() != dbNoList.Count)
                {
                    _logger.Error($"Check db number: Nodes contain duplicate db number for ip: {ip}.");
                    throw new Exception();
                }

                var apiPortList = _allNodes.FindAll(o => o.IpAddress == ip).Select(o => o.ApiPort).ToList();
                if (apiPortList.Distinct().Count() != apiPortList.Count)
                {
                    _logger.Error($"Check api port: Nodes contain duplicate api port for ip: {ip}.");
                    throw new Exception();
                }

                var netPortList = _allNodes.FindAll(o => o.IpAddress == ip).Select(o => o.NetPort).ToList();
                if (netPortList.Distinct().Count() != netPortList.Count)
                {
                    _logger.Error($"Check net port: Nodes contain duplicate net port for ip: {ip}.");
                    throw new Exception();
                }
            }

            return true;
        }
    }
}