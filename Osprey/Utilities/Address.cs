using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Osprey.Communication;
using Osprey.Configuration;
using Osprey.Logging;
using Osprey.Serialization;
using Osprey.ServiceDiscovery;

namespace Osprey.Utilities
{
    /// <summary>
    /// Helper functions for generating addresses and ports.
    /// </summary>
    public static class Address
    {
        public static IPAddress ParseIPAddress(string endpoint)
        {
            return IPAddress.Parse(endpoint);
        }

        public static IPEndPoint GenerateUdpEndpoint()
        {
            return new IPEndPoint(GetLocalIpAddress(), GetUdpPort());
        }

        public static IPEndPoint GenerateTcpEndpoint()
        {
            return new IPEndPoint(GetLocalIpAddress(), GetTcpPort());
        }

        public static int GetUdpPort()
        {
            var l = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0));
            var p = ((IPEndPoint)l.Client.LocalEndPoint).Port;
            l.Close();
            return p;
        }

        public static int GetTcpPort()
        {
            var listener = new TcpListener(IPAddress.Any, 0);
            listener.Start();
            var p = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return p;
        }

        /// <summary>
        /// Get an IP address to be used as a local address for service discovery on an Osprey network.
        /// </summary>
        public static IPAddress GetLocalUdpBroadcastAddress()
        {
            var config = Config.Global;
            var addresses = GetLocalAddressesIPV4();
            
            var ipFilter = "";
            
            // 1) Try to get the system's environment variable filter
            var globalIpFilter = Environment.GetEnvironmentVariable("OSPREY_LOCAL_IP_FILTER");
            if (!string.IsNullOrEmpty(globalIpFilter))
            {
                OspreyLog.Debug($"Using environment variable local IP filter from OSPREY_LOCAL_IP_FILTER: {globalIpFilter}*");
                ipFilter = globalIpFilter;
            }
            
            // 2) Try to match based on the configured UdpBroadcastLocalFilter
            else if (!string.IsNullOrEmpty(config.UdpBroadcastLocalFilter))
            {
                OspreyLog.Debug($"Using preferred local IP filter: {config.UdpBroadcastLocalFilter}*");
                ipFilter = config.UdpBroadcastLocalFilter;
            }

            var ordered = addresses
                .OrderByDescending(ip => ip.ToString().StartsWith(ipFilter))
                .ToList();

            // 3) Use an address that actually contain services if we don't match anything using the filter.
            if (string.IsNullOrEmpty(ipFilter) || !ordered.Any(x => x.ToString().StartsWith(ipFilter)))
            {
                OspreyLog.Warn($"No services matching filter, scanning for networks with services..");
                var activeAddress = ScanForServices(ordered);
                if (activeAddress != null) return activeAddress;
            }

            return ordered.First();
        }

        public static IPAddress? ScanForServices(IEnumerable<IPAddress> addresses)
        {
            var port = Config.Global.UdpBroadcastPort;
            var remote = IPAddress.Parse(Config.Global.UdpBroadcastRemote);

            foreach (var ipAddress in addresses)
            {
                OspreyLog.Debug($"Detecting services on {ipAddress}");

                try
                {
                    using var broadcastChannel = new UdpChannel(remote, ipAddress, port);
                    using var receiver = new Receiver(broadcastChannel, new JsonSerializer());

                    receiver.Start();

                    Thread.Sleep(Config.Global.BroadcastInterval + 200);

                    if (receiver.Active.Any())
                    {
                        OspreyLog.Debug($"SUCCESS: Found {receiver.Active.Count()} services on {ipAddress}!");
                        return ipAddress;
                    }

                    OspreyLog.Debug($"No services found on {ipAddress}");
                }
                catch (Exception ex)
                {
                    OspreyLog.Error($"Error scanning for services on {ipAddress}: {ex.Message}");
                }
            }

            OspreyLog.Warn($"Failed to locate any services on any networks.");
            return null;
        }

        public static IPAddress GetLocalIpAddress()
        {
            return GetLocalAddressesIPV4().First();
        }

        private static IEnumerable<IPAddress> GetLocalAddressesIPV4()
        {
            var config = Config.Global;

            if (config.UseDnsAddress)
            {
                OspreyLog.Debug("Resolving local IP from DNS.");

                var hostName = Dns.GetHostName();
                var host = Dns.GetHostEntry(hostName);

                OspreyLog.Debug("DNS host name: " + hostName);
                OspreyLog.Debug("DNS hosts: " + string.Join(", ", host.AddressList.Select(x => (object)x)));

                var filtered = host.AddressList
                    .Where(x => !x.ToString().StartsWith("127"))
                    .Where(x => x.AddressFamily == AddressFamily.InterNetwork)
                    .ToList();

                if (!filtered.Any()) throw new Exception("No network adapters with an IPv4 address in the system!");

                return filtered;
            }

            OspreyLog.Debug("Resolving local IP from a transient socket.");

            var transientAddress = LocalAddressFromSocket();
            OspreyLog.Debug("Socket address: " + transientAddress);

            if (transientAddress == null) throw new Exception("Unable to resolve address from a transient socket.");

            return new []{ transientAddress };
        }
        
        private static IPAddress LocalAddressFromSocket()
        {
            using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
            {
                socket.Connect("8.8.8.8", 65530);
                IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
                return endPoint?.Address;
            }
        }
    }
}
