using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using Osprey.Communication;
using Osprey.Configuration;
using Osprey.Logging;
using Osprey.Serialization;
using Osprey.ServiceDiscovery;
using Osprey.ServiceDiscovery.Data;
using Osprey.Utilities;
using JsonSerializer = Osprey.Serialization.JsonSerializer;

namespace Osprey
{
    public class OspreyNetwork : IOsprey
    {
        internal readonly IChannel _broadcastChannel;

        public ISerializer Serializer { get; set; }
        public NodeInfo Node { get; }
        public Receiver Receiver { get; private set; }
        public Broadcaster Broadcaster { get; private set; }

        private bool _started;

        internal OspreyNetwork(string nodeName, string environment)
        {
            Serializer = new JsonSerializer();
            
            var port = Config.Global.UdpBroadcastPort;

            var local = LocalAddress();

            var uid = Regex.Replace(Convert.ToBase64String(Guid.NewGuid().ToByteArray()), "[/+=]", "");

            Node = new NodeInfo
            {
                Id = uid,
                Name = nodeName,
                Environment = environment,
                Ip = local.ToString(),
            };
            
            if (Config.Global.DisableUdpNetworking)
            {
                OspreyLog.Warn("Networking disabled, using in-process channel.");
                _broadcastChannel = new InProcessChannel();
            }
            else
            {
                var remote = IPAddress.Parse(Config.Global.UdpBroadcastRemote);
                _broadcastChannel = new UdpChannel(remote, local, port);
            }
        }

        private static IPAddress LocalAddress()
        {
            if (Config.Global.DisableUdpNetworking) return IPAddress.Loopback;

            if (string.IsNullOrEmpty(Config.Global.UdpBroadcastLocal))
            {
                var local = Address.GetLocalUdpBroadcastAddress();
                OspreyLog.Debug("Using automatic local address: " + local);
                return local;
            }
            else
            {
                var local = Address.ParseIPAddress(Config.Global.UdpBroadcastLocal);
                OspreyLog.Debug("Using local address from config: " + local);
                return local;
            }
        }

        public void Start(bool discover = true, bool broadcast = true)
        {
            Receiver = new Receiver(_broadcastChannel, Serializer);
            Broadcaster = new Broadcaster(_broadcastChannel, this);

            if (discover) Receiver.Start();
            if (broadcast) Broadcaster.Start();

            OspreyLog.Info($"Node started:");
            OspreyLog.Info($"  Id:".PadRight(16) + Node.Id);
            OspreyLog.Info($"  Service:".PadRight(16) + Node.Name);
            OspreyLog.Info($"  Environment:".PadRight(16) + Node.Environment);
            OspreyLog.Info($"  Local:".PadRight(16) + Node.Ip);
            OspreyLog.Info($"  Discover:".PadRight(16) + discover);
            OspreyLog.Info($"  Broadcast:".PadRight(16) + broadcast);
            if (Config.Global.EnableServiceOverrides)
            {
                OspreyLog.Info($"  Service Overrides:");
                foreach (var nodeOverride in Config.Global.ServiceOverrides.Where(x => x.Enabled))
                {
                    OspreyLog.Info($"  - {nodeOverride.Name}:");
                    foreach (var serviceOverride in nodeOverride.Services)
                    {
                        OspreyLog.Info($"      {serviceOverride.Name}:".PadRight(16) + serviceOverride.Address);
                    }
                }
            }

            _started = true;
        }
        
        public NodeInfo Locate(string node, string environment = null, bool throwError = false)
        {
            if (!_started) throw new Exception("Caller has not joined an Osprey network.");

            environment ??= Node.Environment;

            if (TryOverride(node, environment, out var nodeInfo)) return nodeInfo;

            return Receiver.Locate(node, environment, throwError);
        }

        public IEnumerable<NodeInfo> LocateNodes(string node, string environment = null)
        {
            if (!_started) throw new Exception("Caller has not joined an Osprey network.");

            environment ??= Node.Environment;

            if (TryOverride(node, environment, out var nodeInfo)) return new [] {nodeInfo};

            return Receiver.LocateAll(node, environment);
        }

        public IEnumerable<NodeInfo> LocateEnvironment(string environment = null)
        {
            if (!_started) throw new Exception("Caller has not joined an Osprey network.");

            environment ??= Node.Environment;
            
            return Receiver.LocateAll(environment);
        }

        public IEnumerable<NodeInfo> LocateAll()
        {
            if (!_started) throw new Exception("Caller has not joined an Osprey network.");
            return Receiver.Active;
        }

        private bool TryOverride(string node, string environment, out NodeInfo nodeInfo)
        {
            nodeInfo = (NodeInfo)null;

            if (Config.Global.EnableServiceOverrides)
            {
                var nodeOverride = Config.Global.ServiceOverrides
                    .Where(x => x.Enabled)
                    .FirstOrDefault(x => x.Name == node);

                if (nodeOverride != null)
                {
                    nodeInfo = new NodeInfo()
                    {
                        Id = nodeOverride.Name + "_override",
                        Environment = environment,
                        Ip = null,
                        Name = nodeOverride.Name,
                        Services = nodeOverride.Services.Select(x => new ServiceInfo
                        {
                            Name = x.Name,
                            Type = x.Type,
                            Address = x.Address
                        }).ToList()
                    };

                    OspreyLog.Trace($"Using override for '{node}'");

                    return true;
                }
            }

            return false;
        }

        public void Register(string type, string name, string address)
        {
            var service = new ServiceInfo()
            {
                Type = type,
                Name = name,
                Address = address
            };

            if (Node.Services.Any(x => x.Name == name))
                throw new Exception("Cannot register the same service name multiple times.");

            Node.Services.Add(service);
        }

        public void Register(ServiceInfo service)
        {
            if (Node.Services.Any(x => x.Name == service.Name))
                throw new Exception("Cannot register the same service name multiple times.");

            Node.Services.Add(service);
        }

        public void Dispose()
        {
            _broadcastChannel?.Dispose();
        }
    }
}
