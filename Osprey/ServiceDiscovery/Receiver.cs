using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Osprey.Communication;
using Osprey.Configuration;
using Osprey.Logging;
using Osprey.Serialization;
using Osprey.ServiceDiscovery.Data;

namespace Osprey.ServiceDiscovery
{
    public class Receiver
    {
        private readonly IChannel _channel;
        private readonly ISerializer _serializer;
        private readonly ConcurrentDictionary<string, NodeInfoEntry> _discovered;

        public IEnumerable<NodeInfo> Active => _discovered.Values
            .Where(x => x.Active)
            .Select(x => x.Node)
            .ToList();

        public event Action<NodeInfo> OnDiscover;
        //public event Action<NodeInfo> OnLost; // TODO

        internal Receiver(IChannel channel, ISerializer serializer)
        {
            _channel = channel;
            _serializer = serializer;
            _discovered = new ConcurrentDictionary<string, NodeInfoEntry>();
        }

        internal void Start()
        {
            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    try
                    {

                        var message = _channel.Receive();

                        _discovered.AddOrUpdate(message, msg =>
                        {
                            var nodeInfo = _serializer.Deserialize<NodeInfo>(message);
                            var nodeInfoEntry = new NodeInfoEntry(nodeInfo, Config.Global.DiscoveryTimeout);
                            OnDiscover?.Invoke(nodeInfo);
                            return nodeInfoEntry;
                        }, (msg, node) =>
                        {
                            node.Update();
                            return node;
                        });
                        
                    }
                    catch (Exception ex)
                    {
                        OspreyLog.Warn("Failed to receive UDP multicast.");
                        OspreyLog.Error(ex.ToString());
                    }

                }
            }, TaskCreationOptions.LongRunning);
        }

        internal NodeInfo Locate(string node, string environment, bool throwError = false)
        {
            return Active
                       .Where(x => x.Name == node && x.Environment == environment)
                       .OrderBy(x => Guid.NewGuid())
                       .FirstOrDefault()
                   ?? (throwError ? throw new ServiceUnavailableException("Service not found") : (NodeInfo)null);
        }

        internal IEnumerable<NodeInfo> LocateAll(string node, string environment)
        {
            return Active.Where(x => x.Name == node && x.Environment == environment);
        }

        internal IEnumerable<NodeInfo> LocateAll(string environment)
        {
            return Active.Where(x => x.Environment == environment);
        }

        private class NodeInfoEntry
        {
            public NodeInfoEntry(NodeInfo node, int timeout)
            {
                Node = node;
                Discovered = DateTime.UtcNow;
                Timeout = timeout;
            }

            private DateTime Discovered { get; set; }
            private int Timeout { get; }
            public NodeInfo Node { get; }
            public bool Active => DateTime.UtcNow < Discovered.AddMilliseconds(Timeout);

            public void Update()
            {
                Discovered = DateTime.UtcNow;
            }
        }
    }
}