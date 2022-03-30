using System;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using Osprey.Communication;
using Osprey.ServiceDiscovery;
using Osprey.Utilities;

[assembly: InternalsVisibleTo("Osprey.Monitor")]
namespace Osprey
{
    public class Node : IDisposable
    {
        public NodeInfo Info { get; private set; }
        public Receiver Receiver { get; private set; }
        public Broadcaster Broadcaster { get; private set; }

        private readonly UdpChannel _broadcastChannel;

        internal Node(string id, string name, string environment)
        {
            var config = OSPREY.Network.Config.Network;
            var port = config.UdpBroadcastPort;
            
            IPAddress local;
            if (string.IsNullOrEmpty(config.UdpBroadcastLocal))
            {
                local = Address.GetLocalUdpBroadcastAddress();
                OSPREY.Network.Logger.Debug("Using automatic local address: " + local);
            }
            else
            {
                local = config.UdpBroadcastLocal.ParseIPAddress();
                OSPREY.Network.Logger.Debug("Using local address from config: " + local);
            }

            var remote = IPAddress.Parse(config.UdpBroadcastRemote);

            Info = new NodeInfo
            {
                Id = id,
                Name = name,
                Environment = environment,
                Ip = local.ToString(),
            };

            _broadcastChannel = new UdpChannel(remote, local, port);
        }

        internal void Start()
        {
            var config = OSPREY.Network.Config.Network;

            if (config.Discover)
            {
                Receiver = new Receiver(_broadcastChannel);
                Receiver.Start();
            }
            if (config.Broadcast)
            {
                Broadcaster = new Broadcaster(_broadcastChannel, Info, config.BroadcastInterval);
                Broadcaster.Start();
            }

            OSPREY.Network.Logger.Info($"Node started:");
            OSPREY.Network.Logger.Info($"  Id:".PadRight(14) + Info.Id);
            OSPREY.Network.Logger.Info($"  Service:".PadRight(14) + Info.Name);
            OSPREY.Network.Logger.Info($"  Environment:".PadRight(14) + Info.Environment);
            OSPREY.Network.Logger.Info($"  Local:".PadRight(14) + Info.Ip);
            OSPREY.Network.Logger.Info($"  Broadcast:".PadRight(14) + config.Broadcast);
            OSPREY.Network.Logger.Info($"  Discover:".PadRight(14) + config.Discover);
        }

        public void Register(ServiceInfo service)
        {
            if (Info.Services.Any(x => x.Name == service.Name))
                throw new Exception("Already registered: " + service.Name);
            Info.Services.Add(service);
            OSPREY.Network.Logger.Info($"Registered new service: [{service.Type}] {service.Name} | {service.Address}");
        }

        public void Dispose()
        {
            _broadcastChannel?.Dispose();
        }
    }
}