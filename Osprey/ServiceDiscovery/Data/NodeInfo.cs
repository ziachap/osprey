using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Osprey.ServiceDiscovery.Data
{
    /// <summary>
    /// Contains information about a node.
    /// </summary>
    [Serializable]
    public class NodeInfo
    {
        public NodeInfo()
        {
            Services = new List<ServiceInfo>();
        }

        /// <summary>
        /// Unique ID for the node.
        /// </summary>
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("n")]
        public string Name { get; set; }

        [JsonProperty("e")]
        public string Environment { get; set; }

        [JsonProperty("ip")]
        public string Ip { get; set; }

        [JsonProperty("s")]
        public List<ServiceInfo> Services { get; set; }
    }
}