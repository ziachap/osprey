using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Osprey.Serialization;
using Osprey.ServiceDiscovery;
using Osprey.ServiceDiscovery.Data;

[assembly: InternalsVisibleTo("Osprey.Tests")]
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
        /// Locate a node on the network. Will include service overrides.
        /// </summary>
        /// <param name="environment">Restrict to a particular environment. If null, uses current environment.</param>
        NodeInfo Locate(string node, string environment = null, bool throwError = false);

        /// <summary>
        /// Locate all instances of a node on the network. Will include service overrides.
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
}