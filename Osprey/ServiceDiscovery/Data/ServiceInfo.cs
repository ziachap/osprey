using System;
using Newtonsoft.Json;

namespace Osprey.ServiceDiscovery.Data
{
    /// <summary>
    /// Describes a service on a node.
    /// </summary>
    [Serializable]
    public class ServiceInfo
    {
        [JsonProperty("t")]
        public string Type { get; set; }

        [JsonProperty("n")]
        public string Name { get; set; }

        [JsonProperty("a")]
        public string Address { get; set; }
    }

    public static class ServiceType
    {
        public const string HTTP = "http";
        public const string ZeroMQ = "zmq";
    }
}
