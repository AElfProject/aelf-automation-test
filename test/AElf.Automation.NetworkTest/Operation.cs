using System.Collections.Generic;
using System.Linq;
using AElfChain.Common;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using log4net;

namespace AElf.Automation.NetworkTest
{
    public class Operation
    {
        private static readonly ILog Logger = Log4NetHelper.GetLogger();
        public readonly List<string> AllEndpoint;
        public readonly List<string> AllNodes;

        public Operation()
        {
            AllEndpoint = NodeOption.AllNodes.Select(o => o.Endpoint).ToList();
            AllNodes = GetAllNodes();
        }

        private List<string> GetAllNodes()
        {
            var nodes = NodeOption.AllNodes;
            return nodes.Select(node => node.Endpoint.Replace("8000", "6800")).ToList();
        }

        public void RemovePeer(ICollection<string> operatedNode)
        {
            var operatedNodeEndpoint = operatedNode.Select(node => node.Replace("6800", "8000")).ToList();

            foreach (var endpoint in operatedNodeEndpoint)
            {
                var nodeManager = new NodeManager(endpoint);
                var changed = nodeManager.UpdateApiUrl(endpoint);
                if (changed == false) continue;
                
                foreach (var n in AllNodes.Where(n => !operatedNode.Contains(n)))
                {
                    var peers = nodeManager.NetGetPeers().Select(o => o.IpAddress).ToList();
                    if (!peers.Contains(n)) continue;
                    var result = nodeManager.NetRemovePeer(n);
                    Logger.Info($"{endpoint} remove peer {n} {result}");
                }

                foreach (var e in AllEndpoint.Where(e => !operatedNodeEndpoint.Contains(e)))
                {
                    var isChanged = nodeManager.UpdateApiUrl(e);
                    if (isChanged == false) continue;
                    
                    var removeNode = endpoint.Replace("8000", "6800");
                    var result = nodeManager.NetRemovePeer(removeNode);
                    Logger.Info($"{e} remove peer {removeNode} {result}");
                }
            }
        }

        public void AddPeer(ICollection<string> operatedNode)
        {
            var operatedNodeEndpoint = operatedNode.Select(node => node.Replace("6800", "8000")).ToList();
            var nodeManager = new NodeManager(operatedNodeEndpoint.First());

            foreach (var node in operatedNodeEndpoint)
            {
                var changed = nodeManager.UpdateApiUrl(node);
                if (changed == false) continue;
                
                foreach (var n in AllNodes.Where(n => !operatedNode.Contains(n)))
                {
                    var peers = nodeManager.NetGetPeers().Select(o => o.IpAddress).ToList();
                    if (peers.Contains(n)) continue;
                    var result = nodeManager.NetAddPeer(n);
                    Logger.Info($"{node} add peer {n} {result}");
                }
            }
            
            foreach (var e in AllEndpoint.Where(e => !operatedNodeEndpoint.Contains(e)))
            {
                var changed = nodeManager.UpdateApiUrl(e);
                if (changed == false) continue;
                
                foreach (var node in operatedNodeEndpoint)
                {
                    var n = node.Replace("8000", "6800");
                    var result = nodeManager.NetAddPeer(n);
                    Logger.Info($"{e} add peer {n} {result}");
                }
            }
        }

        public void GetPeer()
        {
            var nodeManager = new NodeManager(AllEndpoint.First());
            foreach (var node in AllEndpoint)
            {
                var changed = nodeManager.UpdateApiUrl(node);
                if (changed == false) continue;
                var peers = nodeManager.NetGetPeers();
                Logger.Info($"Node {node} peer: {peers.Count}");
                foreach (var res in peers) Logger.Info(res.IpAddress);
            }
        }
    }
}