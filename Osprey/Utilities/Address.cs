using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Osprey.Utilities
{
    /// <summary>
    /// Helper functions for generating addresses and ports.
    /// </summary>
    public static class Address
    {
        public static IPAddress ParseIPAddress(this string endpoint)
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
            var config = OSPREY.Network.Config;

            var addresses = GetLocalAddressesIPV4();

            var ipFilter = config.Network.UdpBroadcastLocalFilter;

            if (!string.IsNullOrEmpty(ipFilter))
            {
                OSPREY.Network.Logger.Debug($"Using preferred local IP filter: {ipFilter}*");
            }

            var ordered = addresses.OrderByDescending(ip => ip.ToString().StartsWith(ipFilter ?? ""));

            return ordered.First();
        }

        public static IPAddress GetLocalIpAddress()
        {
            return GetLocalAddressesIPV4().First();
        }

        private static IEnumerable<IPAddress> GetLocalAddressesIPV4()
        {
            var config = OSPREY.Network.Config;

            if (config.Network.UseDnsAddress)
            {
                OSPREY.Network.Logger.Debug("Resolving local IP from DNS.");

                var hostName = Dns.GetHostName();
                var host = Dns.GetHostEntry(hostName);

                OSPREY.Network.Logger.Debug("DNS host name: " + hostName);
                OSPREY.Network.Logger.Debug("DNS hosts: " + string.Join(", ", host.AddressList.Select(x => (object)x)));

                var filtered = host.AddressList
                    .Where(x => !x.ToString().StartsWith("127"))
                    .Where(x => x.AddressFamily == AddressFamily.InterNetwork)
                    .ToList();

                if (!filtered.Any()) throw new Exception("No network adapters with an IPv4 address in the system!");

                return filtered;
            }

            OSPREY.Network.Logger.Debug("Resolving local IP from a transient socket.");

            var transientAddress = LocalAddressFromSocket();
            OSPREY.Network.Logger.Debug("Socket address: " + transientAddress);

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
