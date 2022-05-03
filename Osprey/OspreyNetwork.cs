using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using Osprey.Communication;
using Osprey.Configuration;
using Osprey.Logging;
using Osprey.Serialization;
using Osprey.ServiceDiscovery;
using Osprey.Utilities;
using JsonSerializer = Osprey.Serialization.JsonSerializer;

namespace Osprey
{
    public interface IOsprey : IDisposable
    {
        ISerializer Serializer { get; }

        /// <summary>
        /// Contains information about this node that is broadcasted to other nodes.
        /// </summary>
        NodeInfo Node { get; }

        /// <summary>
        /// Start broadcasting and discovering services.
        /// </summary>
        void Start(bool discover = true, bool broadcast = true);

        /// <summary>
        /// Attempt to locate a node on the network.
        /// </summary>
        /// <param name="environment">Restrict to a particular environment. If null, uses current environment.</param>
        NodeInfo Locate(string node, string environment = null, bool throwError = false);

        /// <summary>
        /// Locate all instances of a node on the network.
        /// </summary>
        /// <param name="environment">Restrict to a particular environment. If null, uses current environment.</param>
        IEnumerable<NodeInfo> LocateNodes(string node, string environment = null);

        /// <summary>
        /// Locate all nodes on the network for a particular environment.
        /// </summary>
        /// <param name="environment">Restrict to a particular environment. If null, uses current environment.</param>
        IEnumerable<NodeInfo> LocateEnvironment(string environment = null);

        /// <summary>
        /// Return all nodes across all environments on the network.
        /// </summary>
        IEnumerable<NodeInfo> LocateAll();

        /// <summary>
        /// Register a new service to be broadcasted on the network.
        /// </summary>
        void Register(string type, string name, string address);

        /// <summary>
        /// Register a new service to be broadcasted on the network.
        /// </summary>
        void Register(ServiceInfo service);
    }

	public class OspreyNetwork : IOsprey
    {
        public ISerializer Serializer { get; set; }
        public NodeInfo Node { get; }
        public Receiver Receiver { get; private set; }
        public Broadcaster Broadcaster { get; private set; }

        private readonly UdpChannel _broadcastChannel;
        private bool _started;

        internal OspreyNetwork(string nodeName, string environment)
        {
            Serializer = new JsonSerializer();
            
            var port = Configuration.Configuration.Global.UdpBroadcastPort;

            IPAddress local;
            if (string.IsNullOrEmpty(Configuration.Configuration.Global.UdpBroadcastLocal))
            {
                local = Address.GetLocalUdpBroadcastAddress();
                OspreyLog.Debug("Using automatic local address: " + local);
            }
            else
            {
                local = Address.ParseIPAddress(Configuration.Configuration.Global.UdpBroadcastLocal);
                OspreyLog.Debug("Using local address from config: " + local);
            }

            var remote = IPAddress.Parse(Configuration.Configuration.Global.UdpBroadcastRemote);

            var uid = Regex.Replace(Convert.ToBase64String(Guid.NewGuid().ToByteArray()), "[/+=]", "");

            Node = new NodeInfo
            {
                Id = uid,
                Name = nodeName,
                Environment = environment,
                Ip = local.ToString(),
            };

            _broadcastChannel = new UdpChannel(remote, local, port);
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

            _started = true;
        }
        
        public NodeInfo Locate(string node, string environment = null, bool throwError = false)
        {
            if (!_started) throw new Exception("Caller has not joined an Osprey network.");

            environment ??= Node.Environment;

            return Receiver.Locate(node, environment, throwError);
        }

        public IEnumerable<NodeInfo> LocateNodes(string node, string environment = null)
        {
            if (!_started) throw new Exception("Caller has not joined an Osprey network.");

            environment ??= Node.Environment;

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
