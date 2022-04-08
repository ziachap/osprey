using System.Collections.Generic;
using System.Linq;
using System.Text;
using Osprey.ServiceDiscovery;

namespace Osprey.Utilities
{
    public static class NodeExtensions
    {
        public static ServiceInfo FindService(this NodeInfo node, string name, bool throwError = false)
        {
            if (node == null)
            {
                return throwError
                    ? throw new ServiceUnavailableException($"Could not locate node '{node.Name}'")
                    : (ServiceInfo)null;
            }

            var service = node.Services.FirstOrDefault(x => x.Name == name);

            if (service == null && throwError)
            {
                throw new ServiceUnavailableException($"Could not locate service '{name}' on node '{node.Name}'");
            }

            return service;
        }
    }
}
