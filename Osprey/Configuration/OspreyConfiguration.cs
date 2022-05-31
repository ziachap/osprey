using System;
using System.Collections.Generic;
using System.Text;

namespace Osprey.Configuration
{
    /// <summary>
    /// Holds Osprey configuration data loaded from a JSON file.
    /// </summary>
    public class OspreyConfiguration
    {
        /// <summary>
        /// When populated, addresses that begin with this filter will be preferred
        /// when retrieving the local IP Address via DNS. This ensures that Osprey
        /// broadcasts on the correct network where there are multiple NICs.
        /// </summary>
        public string UdpBroadcastLocalFilter { get; set; }

        /// <summary>
        /// Manually set the local IP to be used when broadcasting/receiving services.
        /// </summary>
        public string UdpBroadcastLocal { get; set; }

        /// <summary>
        /// Manually set the remote IP to be used when broadcasting/receiving services.
        /// </summary>
        public string UdpBroadcastRemote { get; set; } = "255.255.255.255";

        /// <summary>
        /// Manually set the port to be used when broadcasting/receiving services.
        /// </summary>
        public int UdpBroadcastPort { get; set; } = 55555;

        /// <summary>
        /// Interval in milliseconds between broadcasting messages containing this
        /// node's service information. This should be in line with discovery timeouts
        /// on other services on the network.
        /// </summary>
        public int BroadcastInterval { get; set; } = 1000;

        /// <summary>
        /// Time in milliseconds to wait for a broadcast from a service before declaring
        /// it dead.
        /// </summary>
        public int DiscoveryTimeout { get; set; } = 3000;

        /// <summary>
        /// If true, uses DNS lookup to find local IP addresses. If false, the address is
        /// obtained through a transient socket.
        /// </summary>
        public bool UseDnsAddress { get; set; } = true;

        /// <summary>
        /// Uses an in-process channel for communication, disabling the UDP multi-cast connection.
        /// </summary>
        public bool DisableUdpNetworking { get; set; } = false;

        /// <summary>
        /// Use the configured service overrides.
        /// </summary>
        public bool EnableServiceOverrides { get; set; } = false;

        public IEnumerable<NodeOverride> ServiceOverrides { get; set; } = Array.Empty<NodeOverride>();
    }

    public class NodeOverride
    {
        public bool Enabled { get; set; }

        public string Name { get; set; }

        public IEnumerable<ServiceOverride> Services { get; set; }
    }

    public class ServiceOverride
    {
        public string Name { get; set; }

        public string Type { get; set; }

        public string Address { get; set; }
    }
}
