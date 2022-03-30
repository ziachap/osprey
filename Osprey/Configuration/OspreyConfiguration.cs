using System;
using System.Collections.Generic;
using System.Text;

namespace Osprey.Configuration
{
    public class OspreyConfiguration
    {
        public NetworkConfiguration Network { get; set; } = new NetworkConfiguration();
    }

    public class NetworkConfiguration
    {
        public bool Broadcast { get; set; } = true;

        public bool Discover { get; set; } = true;

        public string UdpBroadcastLocalFilter { get; set; }

        public string UdpBroadcastLocal { get; set; }

        public string UdpBroadcastRemote { get; set; } = "255.255.255.255";

        public int UdpBroadcastPort { get; set; } = 55555;

        public int BroadcastInterval { get; set; } = 1000;

        public int DiscoveryTimeout { get; set; } = 3000;

        public bool UseDnsAddress { get; set; } = true;
    }
}
